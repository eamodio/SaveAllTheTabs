using System;
using System.Collections;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Text;
using System.Windows.Forms;
using Microsoft;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;
using SaveAllTheTabs.Polyfills;

namespace SaveAllTheTabs
{
    internal interface IDocumentManager
    {
        event EventHandler GroupsReset;

        ObservableCollection<DocumentGroup> Groups { get; }

        int GroupCount { get; }
        bool HasSlotGroups { get; }

        DocumentGroup GetGroup(int slot);
        DocumentGroup GetGroup(string name);
        DocumentGroup GetSelectedGroup();

        void SaveGroup(string name, int? slot = null);

        void RestoreGroup(int slot);
        void RestoreGroup(DocumentGroup group);

        void OpenGroup(int slot);
        void OpenGroup(DocumentGroup group);

        void CloseGroup(DocumentGroup group);

        void MoveGroup(DocumentGroup group, int delta);

        void SetGroupSlot(DocumentGroup group, int slot);

        void RemoveGroup(DocumentGroup group, bool confirm = true);

        void SaveStashGroup();
        void RestoreStashGroup();
        void OpenStashGroup();

        bool HasStashGroup { get; }

        int? FindFreeSlot();
    }

    internal class DocumentManager : IDocumentManager
    {
        public event EventHandler GroupsReset;

        private const string UndoGroupName = "<undo>";
        private const string StashGroupName = "<stash>";

        private const int SlotMin = 1;
        private const int SlotMax = 9;

        private const string StorageCollectionPath = "SaveAllTheTabs";
        private const string SavedTabsStoragePropertyFormat = "SavedTabs.{0}";
        private const string ProjectGroupsKeyPlaceholder = StorageCollectionPath + "\\{0}\\groups";

        private SaveAllTheTabsPackage Package { get; }
        private IServiceProvider ServiceProvider => Package;
        private IVsUIShellDocumentWindowMgr DocumentWindowMgr { get; }
        private string SolutionName
        {
            get
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                return Package.Environment.Solution?.FullName;
            }
        }

        public ObservableCollection<DocumentGroup> Groups { get; private set; }

        public DocumentManager(SaveAllTheTabsPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Package = package;

            package.SolutionChanged += (sender, args) => LoadGroups();
            LoadGroups();

            DocumentWindowMgr = ServiceProvider.GetService(typeof(IVsUIShellDocumentWindowMgr)) as IVsUIShellDocumentWindowMgr;
            Assumes.Present(DocumentWindowMgr);
        }

        private IDisposable _changeSubscription;

        private void LoadGroups()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            _changeSubscription?.Dispose();

            // Load presets for the current solution
            Groups = new TrulyObservableCollection<DocumentGroup>(LoadGroupsForSolution());

            _changeSubscription = new CompositeDisposable
                                  {
                                      Observable.FromEventPattern<NotifyCollectionChangedEventArgs>(Groups, nameof(TrulyObservableCollection<DocumentGroup>.CollectionChanged))
                                                .Subscribe(re => SaveGroupsForSolution()),

                                      Observable.FromEventPattern<PropertyChangedEventArgs>(Groups, nameof(TrulyObservableCollection<DocumentGroup>.CollectionItemChanged))
                                                .Where(re => re.EventArgs.PropertyName == nameof(DocumentGroup.Name) ||
                                                             re.EventArgs.PropertyName == nameof(DocumentGroup.Files) ||
                                                             re.EventArgs.PropertyName == nameof(DocumentGroup.Positions) ||
                                                             re.EventArgs.PropertyName == nameof(DocumentGroup.Slot))
                                                .Throttle(TimeSpan.FromSeconds(1))
                                                .ObserveOnDispatcher()
                                                .Subscribe(re => SaveGroupsForSolution())
                                  };

            GroupsReset?.Invoke(this, EventArgs.Empty);
        }

        public int GroupCount => Groups?.Count ?? 0;
        public bool HasSlotGroups => Groups?.Any(g => g.Slot.HasValue) == true;

        public DocumentGroup GetGroup(int slot) => Groups.FindBySlot(slot);
        public DocumentGroup GetGroup(string name) => Groups.FindByName(name);
        public DocumentGroup GetSelectedGroup() => Groups.SingleOrDefault(g => g.IsSelected);

        public void SaveGroup(string name, int? slot = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (DocumentWindowMgr == null)
            {
                Debug.Assert(false, "IVsUIShellDocumentWindowMgr", String.Empty, 0);
                return;
            }

            if (!Package.Environment.GetDocumentWindows().Any())
            {
                return;
            }

            var isBuiltIn = IsBuiltInGroup(name);
            if (isBuiltIn)
            {
                slot = null;
            }

            var group = Groups.FindByName(name);

            var files = new DocumentFilesHashSet(Package.Environment.GetDocumentFiles().OrderBy(Path.GetFileName));
            //var bps = Package.Environment.GetMatchingBreakpoints(files, StringComparer.OrdinalIgnoreCase));

            using (var stream = new VsOleStream())
            {
                var hr = DocumentWindowMgr.SaveDocumentWindowPositions(0, stream);
                if (hr != VSConstants.S_OK)
                {
                    Debug.Assert(false, "SaveDocumentWindowPositions", String.Empty, hr);

                    if (group != null)
                    {
                        Groups.Remove(group);
                    }
                    return;
                }
                stream.Seek(0, SeekOrigin.Begin);

                var documents = String.Join(", ", files.Select(Path.GetFileName));

                if (group == null)
                {
                    group = new DocumentGroup
                    {
                        Name = name,
                        Description = documents,
                        Files = files,
                        Positions = stream.ToArray()
                    };

                    TrySetSlot(group, slot);
                    if (isBuiltIn)
                    {
                        Groups.Insert(0, group);
                    }
                    else
                    {
                        Groups.Add(group);
                    }
                }
                else
                {
                    SaveUndoGroup(group);

                    group.Description = documents;
                    group.Files = files;
                    group.Positions = stream.ToArray();

                    TrySetSlot(group, slot);
                }
            }
        }

        private void TrySetSlot(DocumentGroup group, int? slot)
        {
            if (!slot.HasValue || !(slot <= SlotMax) || group.Slot == slot)
            {
                return;
            }

            // Find out if there is an existing item in the desired slot
            var resident = Groups.FindBySlot((int)slot);
            if (resident != null)
            {
                resident.Slot = null;
            }

            group.Slot = slot;
        }

        public void RestoreGroup(int slot)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RestoreGroup(Groups.FindBySlot(slot));
        }

        public void RestoreGroup(DocumentGroup group)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (group == null)
            {
                return;
            }

            var windows = Package.Environment.GetDocumentWindows();
            if (!IsUndoGroup(group.Name) && windows.Any())
            {
                SaveUndoGroup();
            }

            if (windows.Any())
                windows.CloseAll();

            OpenGroup(group);
        }

        public void OpenGroup(int slot)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OpenGroup(Groups.FindBySlot(slot));
        }

        public void OpenGroup(DocumentGroup group)
        {
            if (group == null)
            {
                return;
            }

            using (var stream = new VsOleStream())
            {
                ThreadHelper.ThrowIfNotOnUIThread();
                stream.Write(group.Positions, 0, group.Positions.Length);
                stream.Seek(0, SeekOrigin.Begin);

                var hr = DocumentWindowMgr.ReopenDocumentWindows(stream);
                if (hr != VSConstants.S_OK)
                {
                    Debug.Assert(false, "ReopenDocumentWindows", String.Empty, hr);
                }
            }
        }

        public void CloseGroup(DocumentGroup group)
        {
            if (group?.Files == null)
            {
                return;
            }

            ThreadHelper.ThrowIfNotOnUIThread();
            var documents = from d in Package.Environment.GetDocuments()
                            where @group.Files.Contains(d.FullName)
                            select d;
            documents.CloseAll();
        }

        public void MoveGroup(DocumentGroup group, int delta)
        {
            if (group == null || group.IsBuiltIn)
            {
                return;
            }

            var index = Groups.IndexOf(group);
            var newIndex = index + delta;
            if (newIndex < 0 || newIndex >= Groups.Count)
            {
                return;
            }

            var resident = Groups[newIndex];
            if (resident?.IsBuiltIn == true)
            {
                return;
            }

            Groups.Move(index, newIndex);
        }

        public void SetGroupSlot(DocumentGroup group, int slot)
        {
            if (group == null || group.Slot == slot)
            {
                return;
            }

            var resident = Groups.FindBySlot(slot);

            group.Slot = slot;

            if (resident != null)
            {
                resident.Slot = null;
            }
        }

        public void RemoveGroup(DocumentGroup group, bool confirm = true)
        {
            if (group == null)
            {
                return;
            }

            if (confirm)
            {
                var window = new ConfirmDeleteTabsWindow(group.Name);
                if (window.ShowDialog() == false)
                {
                    return;
                }
            }

            SaveUndoGroup(group);
            Groups.Remove(group);
        }

        private void SaveUndoGroup()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SaveGroup(UndoGroupName);
        }

        private void SaveUndoGroup(DocumentGroup group)
        {
            var undo = Groups.FindByName(UndoGroupName);

            if (undo == null)
            {
                undo = new DocumentGroup
                {
                    Name = UndoGroupName,
                    Description = group.Description,
                    Files = group.Files,
                    Positions = group.Positions
                };

                Groups.Insert(0, undo);
            }
            else
            {
                undo.Description = group.Description;
                undo.Files = group.Files;
                undo.Positions = group.Positions;
            }
        }

        public void SaveStashGroup()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            SaveGroup(StashGroupName);
        }

        public void OpenStashGroup()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            OpenGroup(Groups.FindByName(StashGroupName));
        }

        public void RestoreStashGroup()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            RestoreGroup(Groups.FindByName(StashGroupName));
        }

        public bool HasStashGroup => Groups.FindByName(StashGroupName) != null;

        public int? FindFreeSlot()
        {
            var slotted = Groups.Where(g => g.Slot.HasValue)
                                .OrderBy(g => g.Slot)
                                .ToList();

            if (!slotted.Any())
            {
                return SlotMin;
            }

            for (var i = SlotMin; i <= SlotMax; i++)
            {
                if (slotted.All(g => g.Slot != i))
                {
                    return i;
                }
            }

            return null;
        }

        private List<DocumentGroup> LoadGroupsForSolution()
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var solution = SolutionName;
            List<DocumentGroup> groups = new List<DocumentGroup>();
            if (string.IsNullOrWhiteSpace(solution)) {
                return groups;
            }

            try {
                var settingsMgr = new ShellSettingsManager(ServiceProvider);
                var store = settingsMgr.GetReadOnlySettingsStore(SettingsScope.UserSettings);

                string projectKeyHash = GetHashString(solution);
                string projectGroupsKey = string.Format(ProjectGroupsKeyPlaceholder, projectKeyHash);

                if (!store.CollectionExists(projectGroupsKey))
                {
                    // try load tabs from older versions
                    var propertyName = String.Format(SavedTabsStoragePropertyFormat, solution);
                    if (store.PropertyExists(StorageCollectionPath, propertyName))
                    {
                        var tabs = store.GetString(StorageCollectionPath, propertyName);
                        groups = JsonConvert.DeserializeObject<List<DocumentGroup>>(tabs);
                    }

                    return groups;
                }

                var groupProperties = store.GetPropertyNamesAndValues(projectGroupsKey);
                foreach (var groupProperty in groupProperties)
                {
                    DocumentGroup group = JsonConvert.DeserializeObject<DocumentGroup>(groupProperty.Value.ToString());
                    groups.Add(group);
                }
            } catch (Exception ex) {
                Debug.Assert(false, nameof(LoadGroupsForSolution), ex.ToString());
            }

            return groups;
            }

        private void SaveGroupsForSolution(IList<DocumentGroup> groups = null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var solution = SolutionName;
            if (string.IsNullOrWhiteSpace(solution))
            {
                return;
            }

            if (groups == null)
            {
                groups = Groups;
            }

            var settingsMgr = new ShellSettingsManager(ServiceProvider);
            var store = settingsMgr.GetWritableSettingsStore(SettingsScope.UserSettings);

            string projectKeyHash = GetHashString(solution);
            string projectGroupsKey = string.Format(ProjectGroupsKeyPlaceholder, projectKeyHash);

            if (store.CollectionExists(projectGroupsKey))
            {
                store.DeleteProperty(StorageCollectionPath, projectGroupsKey);
            }
            store.CreateCollection(projectGroupsKey);

            foreach (DocumentGroup group in groups)
            {
                var serializedGroup = JsonConvert.SerializeObject(group);
                store.SetString(projectGroupsKey, group.Name, serializedGroup);
            }
        }

        private static string GetHashString(string source)
        {
            using (System.Security.Cryptography.MD5 md5 = System.Security.Cryptography.MD5.Create())
            {
                byte[] inputBytes = System.Text.Encoding.ASCII.GetBytes(source);
                byte[] hashBytes = md5.ComputeHash(inputBytes);
                // Convert the byte array to hexadecimal string
                StringBuilder sb = new StringBuilder();
                for (int i = 0; i < hashBytes.Length; i++)
                {
                    sb.Append(hashBytes [i].ToString("X2"));
                }
                return sb.ToString();
            }
        }

        public static bool IsStashGroup(string name)
        {
            return (name?.Equals(StashGroupName, StringComparison.InvariantCultureIgnoreCase)).GetValueOrDefault();
        }

        public static bool IsUndoGroup(string name)
        {
            return (name?.Equals(UndoGroupName, StringComparison.InvariantCultureIgnoreCase)).GetValueOrDefault();
        }

        public static bool IsBuiltInGroup(string name)
        {
            return IsUndoGroup(name) || IsStashGroup(name);
        }
    }
}
