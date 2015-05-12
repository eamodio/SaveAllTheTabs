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
    public class DocumentGroup
    {
        public string Name { get; set; }
        public byte[] Positions { get; set; }
    }

    public interface IDocumentManager
    {
        int GroupCount { get; }
        DocumentGroup GetGroup(int index);

        void SaveGroup(string name);
        void ApplyGroup(int index);
        void ClearGroups();
    }

    public class DocumentManager : IDocumentManager
    {
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
        public DocumentGroup GetGroup(int index) => _groups.GetOrDefault(index);

        public void SaveGroup(string name)
        {
            if (DocumentWindowMgr == null)
            {
                Debug.Assert(false, "IVsUIShellDocumentWindowMgr", String.Empty, 0);
                return;
            }

            var group = _groups.Find(g => g.Name == name);

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
                    _groups.Add(new DocumentGroup
                                {
                                    Name = name,
                                    Positions = stream.ToArray()
                                });
                }
                else
                {
                    group.Positions = stream.ToArray();
                }
            }

            SaveGroupsForSolution();
        }

        public void ApplyGroup(int index)
        {
            var group = _groups.GetOrDefault(index);
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

        public void ClearGroups()
        {
            _groups.Clear();
            SaveGroupsForSolution();
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

    public static class Extensions
    {
        public static DocumentGroup GetOrDefault(this List<DocumentGroup> groups, int index)
        {
            if (index < 0 || index >= groups.Count)
            {
                return null;
            }

            return groups?[index];
        }
    }
}
