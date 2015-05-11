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
    public interface IDocumentManager
    {
        void Save(int group);
        void Restore(int group);

        bool CanRestore(int group);
    }

    public class DocumentManager : IDocumentManager
    {
        private const string StorageCollectionPath = "TabGroups";

        private TabGroupsPackage Package { get; }
        private System.IServiceProvider ServiceProvider => Package;
        private IVsUIShellDocumentWindowMgr DocumentWindowMgr { get; }
        private string SolutionName => Package.Environment.Solution?.FullName;

        private readonly Dictionary<string, byte[]> _groups;

        public DocumentManager(TabGroupsPackage package)
        {
            Package = package;

            // Load presets for the current solution
            _groups = LoadTabGroupsForSolution();

            DocumentWindowMgr = ServiceProvider.GetService(typeof(IVsUIShellDocumentWindowMgr)) as IVsUIShellDocumentWindowMgr;
        }

        private Dictionary<string, byte[]> LoadTabGroupsForSolution()
        {
            var solution = SolutionName;
            if (!string.IsNullOrWhiteSpace(solution))
            {
                var settingsMgr = new ShellSettingsManager(ServiceProvider);
                var store = settingsMgr.GetReadOnlySettingsStore(SettingsScope.UserSettings);
                if (store.PropertyExists(StorageCollectionPath, solution))
                {
                    var tabs = store.GetString(StorageCollectionPath, solution);
                    return JsonConvert.DeserializeObject<Dictionary<string, byte[]>>(tabs);
                }
            }

            return new Dictionary<string, byte[]>();
        }

        private void SaveTabGroupsForSolution()
        {
            var solution = SolutionName;
            if (string.IsNullOrWhiteSpace(solution))
            {
                return;
            }

            var groups = _groups.Where(_ => _.Value?.Length > 0)
                                .ToDictionary(_ => _.Key, _ => _.Value);

            var settingsMgr = new ShellSettingsManager(ServiceProvider);
            var store = settingsMgr.GetWritableSettingsStore(SettingsScope.UserSettings);

            if (!store.CollectionExists(StorageCollectionPath))
            {
                store.CreateCollection(StorageCollectionPath);
            }

            if (!groups.Any())
            {
                store.DeleteProperty(StorageCollectionPath, solution);
                return;
            }

            var tabs = JsonConvert.SerializeObject(groups);
            store.SetString(StorageCollectionPath, solution, tabs);
        }

        private const string GroupIdFormat = "Group{0}";
        private static string GetGroupId(int group) => string.Format(GroupIdFormat, group);

        public void Save(int group)
        {
            var groupId = GetGroupId(group);
            _groups.Remove(groupId);

            if (DocumentWindowMgr == null)
            {
                Debug.Assert(false, "IVsUIShellDocumentWindowMgr", String.Empty, 0);
                return;
            }

            using (var stream = new VsOleStream())
            {
                var hr = DocumentWindowMgr.SaveDocumentWindowPositions(0, stream);
                if (hr != VSConstants.S_OK)
                {
                    Debug.Assert(false, "SaveDocumentWindowPositions", String.Empty, hr);
                    return;
                }

                stream.Seek(0, SeekOrigin.Begin);
                _groups[groupId] = stream.ToArray();
            }

            SaveTabGroupsForSolution();
        }

        public void Restore(int group)
        {
            byte[] tabs;
            if (!_groups.TryGetValue(GetGroupId(group), out tabs))
            {
                return;
            }

            using (var stream = new VsOleStream())
            {
                stream.Write(tabs, 0, tabs.Length);
                stream.Seek(0, SeekOrigin.Begin);

                var hr = DocumentWindowMgr.ReopenDocumentWindows(stream);
                if (hr != VSConstants.S_OK)
                {
                    Debug.Assert(false, "ReopenDocumentWindows", String.Empty, hr);
                }
            }
        }

        public bool CanRestore(int group)
        {
            byte[] tabs;
            return _groups.TryGetValue(GetGroupId(group), out tabs) && tabs != null;
        }
    }
}
