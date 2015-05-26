using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace SaveAllTheTabs.Commands
{
    internal class SavedTabsWindowToolbarCommands
    {
        [Guid(PackageGuids.SavedTabsWindowToolbarCmdSetGuidString)]
        private enum SavedTabsWindowToolbarCommandIds
        {
            SavedTabsWindowToolbar = 0x0100,
            SavedTabsWindowToolbarUpdateTabs = 0x0200,
            SavedTabsWindowToolbarRemoveTabs = 0x0300,
            SavedTabsWindowToolbarRestoreTabs = 0x0400,
            SavedTabsWindowToolbarResetTabs = 0x0500
        }

        private SaveAllTheTabsPackage Package { get; }

        public SavedTabsWindowToolbarCommands(SaveAllTheTabsPackage package)
        {
            Package = package;
        }

        public CommandID SetupToolbar(OleMenuCommandService commandService)
        {
            var guid = typeof(SavedTabsWindowToolbarCommandIds).GUID;

            SetupCommands(commandService);
            return new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbar);
        }

        private void SetupCommands(OleMenuCommandService commandService)
        {
            var guid = typeof(SavedTabsWindowToolbarCommandIds).GUID;

            var commandId = new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbarUpdateTabs);
            var command = new OleMenuCommand(ExecuteUpdateCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbarRemoveTabs);
            command = new OleMenuCommand(ExecuteRemoveCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbarRestoreTabs);
            command = new OleMenuCommand(ExecuteRestoreCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbarResetTabs);
            command = new OleMenuCommand(ExecuteResetCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private void CommandOnBeforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            command.Enabled = Package.DocumentManager?.GetSelectedGroup() != null;
        }

        private void ExecuteUpdateCommand(object sender, EventArgs e)
        {
            var selected = Package.DocumentManager?.GetSelectedGroup();
            if (selected == null)
            {
                return;
            }

            Package.DocumentManager.SaveGroup(selected.Name, selected.Slot);
        }

        private void ExecuteRemoveCommand(object sender, EventArgs e)
        {
            var selected = Package.DocumentManager?.GetSelectedGroup();
            if (selected == null)
            {
                return;
            }

            Package.DocumentManager?.RemoveGroup(selected);
        }

        private void ExecuteRestoreCommand(object sender, EventArgs e)
        {
            var selected = Package.DocumentManager?.GetSelectedGroup();
            if (selected == null)
            {
                return;
            }

            Package.DocumentManager?.RestoreGroup(selected);
        }

        private void ExecuteResetCommand(object sender, EventArgs e)
        {
            var selected = Package.DocumentManager?.GetSelectedGroup();
            if (selected == null)
            {
                return;
            }

            Package.DocumentManager?.RestoreGroup(selected, true);
        }
    }
}