using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using System.Windows.Input;
using Microsoft.VisualStudio.Shell;
using SaveAllTheTabs.Polyfills;

namespace SaveAllTheTabs.Commands
{
    internal class StashCommands
    {
        [Guid(PackageGuids.StashCmdSetGuidString)]
        private enum StashCommandIds
        {
            StashSaveTabs = 0x0100,
            StashRestoreTabs = 0x0200
        }

        private SaveAllTheTabsPackage Package { get; }

        public StashCommands(SaveAllTheTabsPackage package)
        {
            Package = package;
        }

        public void SetupCommands(OleMenuCommandService commandService)
        {
            var guid = typeof(StashCommandIds).GUID;

            var commandId = new CommandID(guid, (int)StashCommandIds.StashSaveTabs);
            var command = new OleMenuCommand(ExecuteStashSaveTabsCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+D,Ctrl+C");

            commandId = new CommandID(guid, (int)StashCommandIds.StashRestoreTabs);
            command = new OleMenuCommand(ExecuteStashRestoreTabsCommand, commandId);
            command.BeforeQueryStatus += StashRestoreTabsCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+D,Ctrl+V", "Global::Ctrl+D,Ctrl+Shift+V", "Global::Ctrl+D,`", "Global::Ctrl+D,Shift+`");
        }

        private void CommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            command.Enabled = Package.DocumentManager != null;
        }

        private void StashRestoreTabsCommandOnBeforeQueryStatus(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            command.Enabled = Package.DocumentManager?.HasStashGroup == true;
        }

        private void ExecuteStashRestoreTabsCommand(object sender, EventArgs e)
        {
            var reset = Keyboard.IsKeyDown(Key.LeftShift) || Keyboard.IsKeyDown(Key.RightShift);
            Package.DocumentManager?.RestoreStashGroup(reset);
        }

        private void ExecuteStashSaveTabsCommand(object sender, EventArgs e)
        {
            Package.DocumentManager?.SaveStashGroup();
        }
    }
}
