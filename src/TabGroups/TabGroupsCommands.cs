using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace TabGroups
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal class TabGroupsCommands
    {
        [Guid(TabGroupsPackageGuids.CmdSetGuidString)]
        private enum CommandIds
        {
            SaveTabs = 0x0100,
            RestoreTabsListMenu = 0x0200
        }

        private TabGroupsPackage Package { get; }
        private IServiceProvider ServiceProvider => Package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TabGroupsCommands Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(TabGroupsPackage package)
        {
            Instance = new TabGroupsCommands(package);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabGroupsCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private TabGroupsCommands(TabGroupsPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            Package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                SetupCommands(commandService);
            }
        }

        private void SetupCommands(OleMenuCommandService commandService)
        {
            var guid = typeof(CommandIds).GUID;

            var commandId = new CommandID(guid, (int)CommandIds.SaveTabs);
            var command = new OleMenuCommand(ExecuteSaveTabsCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)CommandIds.RestoreTabsListMenu);
            command = new OleMenuCommand(null, commandId);
            commandService.AddCommand(command);

            new RestoreTabsListCommands(Package).SetupCommands(commandService);
            new SavedTabsWindowCommands(Package).SetupCommands(commandService);
            new StashCommands(Package).SetupCommands(commandService);
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

        private void ExecuteSaveTabsCommand(object sender, EventArgs e)
        {
            var slot = Package.DocumentManager?.FindFreeSlot();
            var name = $"Tabs{slot ?? ((Package.DocumentManager?.GroupCount ?? 0) + 1)}";

            var window = new SaveTabsWindow(name);
            if (window.ShowDialog() == true)
            {
                Package.DocumentManager?.SaveGroup(window.TabsName, slot);
                //Package.UpdateCommandsUI();
            }
        }
    }
}
