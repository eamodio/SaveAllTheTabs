using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;

namespace TabGroups
{
    internal class SavedTabsWindowCommands
    {
        [Guid(TabGroupsPackageGuids.SavedTabsWindowCmdSetGuidString)]
        private enum SavedTabsWindowCommandIds
        {
            SavedTabsWindow = 0x0100
        }

        private TabGroupsPackage Package { get; }

        public SavedTabsWindowCommands(TabGroupsPackage package)
        {
            Package = package;
        }

        public void SetupCommands(OleMenuCommandService commandService)
        {
            var guid = typeof(SavedTabsWindowCommandIds).GUID;

            var commandId = new CommandID(guid, (int)SavedTabsWindowCommandIds.SavedTabsWindow);
            var command = new OleMenuCommand(ExecuteSavedTabsWindowCommand, commandId);
            commandService.AddCommand(command);
        }

        private void ExecuteSavedTabsWindowCommand(object sender, EventArgs e)
        {
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = Package.FindToolWindow(typeof(SavedTabsToolWindow), 0, true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
        }
    }
}
