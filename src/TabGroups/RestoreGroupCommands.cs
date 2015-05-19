using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace TabGroups
{
    internal class RestoreGroupCommands
    {
        [Guid(TabGroupsPackageGuids.RestoreGroupsCmdSetGuidString)]
        private enum RestoreGroupListCommandIds
        {
            RestoreGroupPlaceholder = 0x0100,
            RestoreGroupStart = 0x0101,
            RestoreGroupEnd = RestoreGroupStart + 8,
        }

        private TabGroupsPackage Package { get; }

        public RestoreGroupCommands(TabGroupsPackage package)
        {
            Package = package;
        }

        public void SetupCommands(OleMenuCommandService commandService)
        {
            if (Package.DocumentManager == null)
            {
                return;
            }

            var guid = typeof(RestoreGroupListCommandIds).GUID;

            var commandId = new CommandID(guid, (int)RestoreGroupListCommandIds.RestoreGroupPlaceholder);
            var command = new OleMenuCommand(null, commandId);
            command.BeforeQueryStatus += RestoreGroupPlaceholderCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            for (var i = (int)RestoreGroupListCommandIds.RestoreGroupStart; i <= (int)RestoreGroupListCommandIds.RestoreGroupEnd; i++)
            {
                commandId = new CommandID(guid, i);
                command = new OleMenuCommand(ExecuteRestoreGroupCommand, commandId);
                command.BeforeQueryStatus += RestoreGroupCommandOnBeforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private static int GetGroupIndex(OleMenuCommand command) =>
            (command.CommandID.ID - (int)RestoreGroupListCommandIds.RestoreGroupStart) + 1;

        private void ExecuteRestoreGroupCommand(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            var index = GetGroupIndex(command);
            Package.DocumentManager?.RestoreGroup(index);
        }

        private void RestoreGroupCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
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

        private void RestoreGroupPlaceholderCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
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