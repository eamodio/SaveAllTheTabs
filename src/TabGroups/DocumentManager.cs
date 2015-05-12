using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Linq;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Settings;
using Microsoft.VisualStudio.Shell.Interop;
using Microsoft.VisualStudio.Shell.Settings;
using Newtonsoft.Json;
using TabGroups.Interop;

namespace TabGroups
{
    internal class DocumentGroup
    {
        public string Name { get; set; }
        public byte[] Positions { get; set; }
        public int? Slot { get; set; }
    }

    internal interface IDocumentManager
    {
        int GroupCount { get; }
        int SlottedGroupCount { get; }

        DocumentGroup GetGroup(int index);
        DocumentGroup GetGroup(string name);

        void SaveGroup(string name, int? index = null);
        void ApplyGroup(int index);
        void ApplyGroup(string name);
        void RemoveGroup(int index);
        void RemoveGroup(string name);
        void ClearGroups();

        int? FindFreeSlot();
    }

    internal class DocumentManager : IDocumentManager
    {
        public const string StashGroupName = "<stash>";

        private const int SlotMin = 1;
        private const int SlotMax = 9;

        private const string StorageCollectionPath = "TabGroups";

        private TabGroupsPackage Package { get; }
        private IServiceProvider ServiceProvider => Package;
        private IVsUIShellDocumentWindowMgr DocumentWindowMgr { get; }
        private string SolutionName => Package.Environment.Solution?.FullName;

        private readonly List<DocumentGroup> _groups;

        public DocumentManager(TabGroupsPackage package)
        {
            Package = package;

            // Load presets for the current solution
            _groups = LoadGroupsForSolution();

            DocumentWindowMgr = ServiceProvider.GetService(typeof(IVsUIShellDocumentWindowMgr)) as IVsUIShellDocumentWindowMgr;
        }

        public int GroupCount => _groups?.Count ?? 0;
        public int SlottedGroupCount => _groups?.Count(g => g.Slot.HasValue) ?? 0;

        public DocumentGroup GetGroup(int index) => _groups.FindBySlot(index);
        public DocumentGroup GetGroup(string name) => _groups.FindByName(name);

        public void SaveGroup(string name, int? index = null)
        {
            if (DocumentWindowMgr == null)
            {
                Debug.Assert(false, "IVsUIShellDocumentWindowMgr", String.Empty, 0);
                return;
            }

            var group = _groups.FindByName(name);

            using (var stream = new VsOleStream())
            {
                var hr = DocumentWindowMgr.SaveDocumentWindowPositions(0, stream);
                if (hr != VSConstants.S_OK)
                {
                    Debug.Assert(false, "SaveDocumentWindowPositions", String.Empty, hr);

                    if (group != null)
                    {
                        _groups.Remove(group);
                    }
                    return;
                }
                stream.Seek(0, SeekOrigin.Begin);

                if (group == null)
                {
                    group = new DocumentGroup
                            {
                                Name = name,
                                Positions = stream.ToArray()
                            };
                    _groups.Add(group);
                }
                else
                {
                    group.Positions = stream.ToArray();
                }

                if (index.HasValue && index <= SlotMax && group.Slot != index)
                {
                    // Find out if there is an existing item in the desired slot
                    var resident = _groups.FindBySlot((int)index);
                    if (resident != null)
                    {
                        resident.Slot = null;
                    }

                    group.Slot = index;

                    //// Reorder the slots, but preserve the order of the rest
                    //_groups = _groups.Where(g => g.Slot.HasValue)
                    //                 .OrderBy(g => g.Slot)
                    //                 .Union(_groups.Where(g => !g.Slot.HasValue))
                    //                 .ToList();
                }
            }

            SaveGroupsForSolution();
        }

        public void ApplyGroup(int index)
        {
            ApplyGroupCore(_groups.FindBySlot(index));
        }

        public void ApplyGroup(string name)
        {
            ApplyGroupCore(_groups.FindByName(name));
        }

        private void ApplyGroupCore(DocumentGroup group)
        {
            if (group == null)
            {
                return;
            }

            using (var stream = new VsOleStream())
            {
                stream.Write(group.Positions, 0, group.Positions.Length);
                stream.Seek(0, SeekOrigin.Begin);

                var hr = DocumentWindowMgr.ReopenDocumentWindows(stream);
                if (hr != VSConstants.S_OK)
                {
                    Debug.Assert(false, "ReopenDocumentWindows", String.Empty, hr);
                }
            }
        }

        public void RemoveGroup(int index)
        {
            RemoveGroupCore(_groups.FindBySlot(index));
        }

        public void RemoveGroup(string name)
        {
            RemoveGroupCore(_groups.FindByName(name));
        }

        private void RemoveGroupCore(DocumentGroup group)
        {
            if (group == null)
            {
                return;
            }

            _groups.Remove(group);
            SaveGroupsForSolution();
        }

        public void ClearGroups()
        {
            _groups.Clear();
            SaveGroupsForSolution();
        }

        public int? FindFreeSlot()
        {
            var slotted = _groups.Where(g => g.Slot.HasValue)
                                 .OrderBy(g => g.Slot)
                                 .ToList();

            if (!slotted.Any())
            {
                return SlotMin;
            }

            for (var i = SlotMin; i <= SlotMax; i++)
            {
                if (slotted.Any(g => g.Slot != i))
                {
                    return i;
                }
            }

            return null;
        }

        private List<DocumentGroup> LoadGroupsForSolution()
        {
            var solution = SolutionName;
            if (!string.IsNullOrWhiteSpace(solution))
            {
                try
                {
                    var settingsMgr = new ShellSettingsManager(ServiceProvider);
                    var store = settingsMgr.GetReadOnlySettingsStore(SettingsScope.UserSettings);
                    if (store.PropertyExists(StorageCollectionPath, solution))
                    {
                        var tabs = store.GetString(StorageCollectionPath, solution);
                        return JsonConvert.DeserializeObject<List<DocumentGroup>>(tabs);
                    }
                }
                catch (Exception) { }
            }
            return new List<DocumentGroup>();
        }

        private void SaveGroupsForSolution()
        {
            var solution = SolutionName;
            if (string.IsNullOrWhiteSpace(solution))
            {
                return;
            }

            var settingsMgr = new ShellSettingsManager(ServiceProvider);
            var store = settingsMgr.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!store.CollectionExists(StorageCollectionPath))
            {
                store.CreateCollection(StorageCollectionPath);
            }

            if (!_groups.Any())
            {
                store.DeleteProperty(StorageCollectionPath, solution);
                return;
            }

            var tabs = JsonConvert.SerializeObject(_groups);
            store.SetString(StorageCollectionPath, solution, tabs);
        }
    }
}
