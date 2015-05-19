using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace TabGroups
{
    internal class ApplyGroupCommands
    {
        [Guid(TabGroupsPackageGuids.ApplyGroupsCmdSetGuidString)]
        private enum ApplyGroupListCommandIds
        {
            ApplyGroupPlaceholder = 0x0100,
            ApplyGroupStart = 0x0101,
            ApplyGroupEnd = ApplyGroupStart + 8,
        }

        private TabGroupsPackage Package { get; }

        public ApplyGroupCommands(TabGroupsPackage package)
        {
            Package = package;
        }

        public void SetupCommands(OleMenuCommandService commandService)
        {
            if (Package.DocumentManager == null)
            {
                return;
            }

            var guid = typeof(ApplyGroupListCommandIds).GUID;

            var commandId = new CommandID(guid, (int)ApplyGroupListCommandIds.ApplyGroupPlaceholder);
            var command = new OleMenuCommand(null, commandId);
            command.BeforeQueryStatus += ApplyGroupPlaceholderCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            for (var i = (int)ApplyGroupListCommandIds.ApplyGroupStart; i <= (int)ApplyGroupListCommandIds.ApplyGroupEnd; i++)
            {
                commandId = new CommandID(guid, i);
                command = new OleMenuCommand(ExecuteApplyGroupCommand, commandId);
                command.BeforeQueryStatus += ApplyGroupCommandOnBeforeQueryStatus;
                commandService.AddCommand(command);
            }
        }

        private static int GetGroupIndex(OleMenuCommand command) =>
            (command.CommandID.ID - (int)ApplyGroupListCommandIds.ApplyGroupStart) + 1;

        private void ExecuteApplyGroupCommand(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            var index = GetGroupIndex(command);
            Package.DocumentManager?.ApplyGroup(index);
        }

        private void ApplyGroupCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
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

        private void ApplyGroupPlaceholderCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
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