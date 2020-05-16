using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using SaveAllTheTabs.Polyfills;

namespace SaveAllTheTabs.Commands
{
    public class SavedTabsWindowCommands
    {
        [Guid(PackageGuids.SavedTabsWindowToolbarCmdSetGuidString)]
        private enum SavedTabsWindowToolbarCommandIds
        {
            SavedTabsWindowToolbar = 0x0100,
            SavedTabsWindowToolbarSaveToTabs = 0x0200,
            SavedTabsWindowToolbarDeleteTabs = 0x0300,
            SavedTabsWindowToolbarRestoreTabs = 0x0400,
            SavedTabsWindowToolbarOpenTabs = 0x0500,
            SavedTabsWindowToolbarCloseTabs = 0x0600
        }

        [Guid(PackageGuids.SavedTabsWindowContextMenuCmdSetGuidString)]
        private enum SavedTabsWindowContextMenuCommandIds
        {
            SavedTabsWindowContextMenu = 0x0100
        }

        private SaveAllTheTabsPackage Package { get; }
        private OleMenuCommandService CommandService { get; }

        public SavedTabsWindowCommands(SaveAllTheTabsPackage package)
        {
            Package = package;
            CommandService = ((IServiceProvider)package)?.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
        }

        public CommandID SetupToolbar()
        {
            var guid = typeof(SavedTabsWindowToolbarCommandIds).GUID;

            SetupCommands(CommandService);
            return new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbar);
        }

        public void ShowContextMenu(int x, int y)
        {
            var commandId = new CommandID(typeof(SavedTabsWindowContextMenuCommandIds).GUID, (int)SavedTabsWindowContextMenuCommandIds.SavedTabsWindowContextMenu);
            CommandService?.ShowContextMenu(commandId, x, y);
        }

        private void SetupCommands(OleMenuCommandService commandService)
        {
            var guid = typeof(SavedTabsWindowToolbarCommandIds).GUID;

            var commandId = new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbarSaveToTabs);
            var command = new OleMenuCommand(ExecuteSaveToCommand, commandId);
            command.BeforeQueryStatus += SaveToCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbarRestoreTabs);
            command = new OleMenuCommand(ExecuteRestoreCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbarOpenTabs);
            command = new OleMenuCommand(ExecuteOpenCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbarCloseTabs);
            command = new OleMenuCommand(ExecuteCloseCommand, commandId);
            command.BeforeQueryStatus += CloseCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)SavedTabsWindowToolbarCommandIds.SavedTabsWindowToolbarDeleteTabs);
            command = new OleMenuCommand(ExecuteDeleteCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private void SaveToCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            var group = Package.DocumentManager?.GetSelectedGroup();
            command.Enabled = group != null && !group.IsUndo && Package.Environment.GetDocumentWindows().Any();
        }

        private void CloseCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            command.Enabled = Package.DocumentManager?.GetSelectedGroup() != null && Package.Environment.GetDocumentWindows().Any();
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

        private void ExecuteSaveToCommand(object sender, EventArgs e)
        {
            var selected = Package.DocumentManager?.GetSelectedGroup();
            if (selected == null)
            {
                return;
            }

            Package.DocumentManager.SaveGroup(selected.Name, selected.Slot);
        }

        private void ExecuteDeleteCommand(object sender, EventArgs e)
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

        private void ExecuteOpenCommand(object sender, EventArgs e)
        {
            var selected = Package.DocumentManager?.GetSelectedGroup();
            if (selected == null)
            {
                return;
            }

            Package.DocumentManager?.OpenGroup(selected);
        }

        private void ExecuteCloseCommand(object sender, EventArgs e)
        {
            var selected = Package.DocumentManager?.GetSelectedGroup();
            if (selected == null)
            {
                return;
            }

            Package.DocumentManager?.CloseGroup(selected);
        }
    }
}