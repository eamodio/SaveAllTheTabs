using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;
using SaveAllTheTabs.Polyfills;

namespace SaveAllTheTabs.Commands
{
    internal class RestoreTabsListCommands
    {
        [Guid(PackageGuids.RestoreTabsListCmdSetGuidString)]
        private enum RestoreTabsListCommandIds
        {
            RestoreTabsListPlaceholder = 0x0100,
            RestoreTabsListStart = 0x0101,
            RestoreTabsListEnd = RestoreTabsListStart + 8,
        }

        private SaveAllTheTabsPackage Package { get; }

        public RestoreTabsListCommands(SaveAllTheTabsPackage package)
        {
            Package = package;
        }

        public void SetupCommands(OleMenuCommandService commandService)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (Package.DocumentManager == null)
            {
                return;
            }

            var guid = typeof(RestoreTabsListCommandIds).GUID;

            var commandId = new CommandID(guid, (int)RestoreTabsListCommandIds.RestoreTabsListPlaceholder);
            var command = new OleMenuCommand(null, commandId);
            command.BeforeQueryStatus += RestoreTabsListPlaceholderCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            for (var i = (int)RestoreTabsListCommandIds.RestoreTabsListStart; i <= (int)RestoreTabsListCommandIds.RestoreTabsListEnd; i++)
            {
                commandId = new CommandID(guid, i);
                command = new OleMenuCommand(ExecuteRestoreTabsCommand, commandId);
                command.BeforeQueryStatus += RestoreTabsCommandOnBeforeQueryStatus;
                commandService.AddCommand(command);

                var index = GetGroupIndex(command);
                Package.Environment.SetKeyBindings(command, $"Global::Ctrl+D,{index}", $"Text Editor::Ctrl+D,{index}");
            }
        }

        private static int GetGroupIndex(OleMenuCommand command) => (command.CommandID.ID - (int)RestoreTabsListCommandIds.RestoreTabsListStart) + 1;

        private void ExecuteRestoreTabsCommand(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            var index = GetGroupIndex(command);
            Package.DocumentManager?.RestoreGroup(index);
        }

        private void RestoreTabsCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            var index = GetGroupIndex(command);
            var group = Package.DocumentManager.GetGroup(index);
            if (group != null)
            {
                command.Text = $"{index} {group.Name}";
                command.Enabled = true;
                command.Visible = true;
            }
            else
            {
                command.Visible = false;
            }
        }

        private void RestoreTabsListPlaceholderCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            command.Enabled = false;
            command.Visible = Package.DocumentManager?.HasSlotGroups != true;
        }
    }
}