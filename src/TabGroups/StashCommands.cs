using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace TabGroups
{
    internal class StashCommands
    {
        [Guid(TabGroupsPackageGuids.StashCmdSetGuidString)]
        private enum StashCommandIds
        {
            StashSaveTabs = 0x0100,
            StashRestoreTabs = 0x0200
        }

        private TabGroupsPackage Package { get; }

        public StashCommands(TabGroupsPackage package)
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

            commandId = new CommandID(guid, (int)StashCommandIds.StashRestoreTabs);
            command = new OleMenuCommand(ExecuteStashRestoreTabsCommand, commandId);
            command.BeforeQueryStatus += StashRestoreTabsCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
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
            Package.DocumentManager?.RestoreStashGroup();
        }

        private void ExecuteStashSaveTabsCommand(object sender, EventArgs e)
        {
            Package.DocumentManager?.SaveStashGroup();
        }
    }
}
