using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace TabGroups
{
    internal class GroupsWindowToolbarCommands
    {
        [Guid(TabGroupsPackageGuids.GroupsWindowToolbarCmdSetGuidString)]
        private enum GroupsWindowToolbarCommandIds
        {
            GroupsWindowToolbar = 0x0100,
            GroupsWindowToolbarApplyGroup = 0x0200,
            GroupsWindowToolbarResetToGroup = 0x0300,
            GroupsWindowToolbarRemoveGroup = 0x0400
        }

        private TabGroupsPackage Package { get; }

        public GroupsWindowToolbarCommands(TabGroupsPackage package)
        {
            Package = package;
        }

        public CommandID SetupToolbar(OleMenuCommandService commandService)
        {
            var guid = typeof(GroupsWindowToolbarCommandIds).GUID;

            SetupCommands(commandService);
            return new CommandID(guid, (int)GroupsWindowToolbarCommandIds.GroupsWindowToolbar);
        }

        private void SetupCommands(OleMenuCommandService commandService)
        {
            var guid = typeof(GroupsWindowToolbarCommandIds).GUID;

            var commandId = new CommandID(guid, (int)GroupsWindowToolbarCommandIds.GroupsWindowToolbarApplyGroup);
            var command = new OleMenuCommand(ExecuteApplyCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)GroupsWindowToolbarCommandIds.GroupsWindowToolbarResetToGroup);
            command = new OleMenuCommand(ExecuteResetToCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)GroupsWindowToolbarCommandIds.GroupsWindowToolbarRemoveGroup);
            command = new OleMenuCommand(ExecuteRemoveCommand, commandId);
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

        private void ExecuteApplyCommand(object sender, EventArgs e)
        {
            var selected = Package.DocumentManager?.GetSelectedGroup();
            if (selected == null)
            {
                return;
            }

            Package.DocumentManager?.ApplyGroup(selected);
        }

        private void ExecuteResetToCommand(object sender, EventArgs e)
        {
            var selected = Package.DocumentManager?.GetSelectedGroup();
            if (selected == null)
            {
                return;
            }

            Package.Environment.GetDocumentWindows().CloseAll();
            Package.DocumentManager?.ApplyGroup(selected);
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
    }
}