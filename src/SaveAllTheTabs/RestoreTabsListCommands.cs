using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;

namespace SaveAllTheTabs
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

                var dteCommand = Package.Environment.Commands.Item(commandId.Guid, commandId.ID);
                var bindings = ((object[])dteCommand.Bindings).ToList();

                var index = (i - RestoreTabsListCommandIds.RestoreTabsListStart) + 1;
                //bindings.Add(String.Format("Global::Ctrl+D, {0}", index));
                bindings.Add(String.Format("Global::Ctrl+Shift+{0}", index));
                //bindings.Add(String.Format("Global::Ctrl+{0}", index));
                dteCommand.Bindings = bindings.ToArray();
            }
        }

        private static int GetGroupIndex(OleMenuCommand command) =>
            (command.CommandID.ID - (int)RestoreTabsListCommandIds.RestoreTabsListStart) + 1;

        private void ExecuteRestoreTabsCommand(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            var index = GetGroupIndex(command);
            var reset = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            Package.DocumentManager?.RestoreGroup(index, reset);
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