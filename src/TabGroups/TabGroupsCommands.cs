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
            SaveGroup = 0x0100,
            RestoreGroupListMenu = 0x0200,
            ClearGroups = 0x0300
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

            var commandId = new CommandID(guid, (int)CommandIds.SaveGroup);
            var command = new OleMenuCommand(ExecuteSaveGroupCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)CommandIds.RestoreGroupListMenu);
            command = new OleMenuCommand(null, commandId);
            commandService.AddCommand(command);

            //commandId = new CommandID(guid, (int)CommandIds.ClearGroups);
            //command = new OleMenuCommand(ExecuteClearGroupsCommand, commandId);
            //command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            //commandService.AddCommand(command);

            new RestoreGroupCommands(Package).SetupCommands(commandService);
            new GroupsWindowCommands(Package).SetupCommands(commandService);
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

        private void ExecuteSaveGroupCommand(object sender, EventArgs e)
        {
            var slot = Package.DocumentManager?.FindFreeSlot();
            var name = $"New Tab Group {slot ?? ((Package.DocumentManager?.GroupCount ?? 0) + 1)}";

            var window = new SaveTabGroupWindow(name);
            if (window.ShowDialog() == true)
            {
                Package.DocumentManager?.SaveGroup(window.GroupName, slot);
                //Package.UpdateCommandsUI();
            }
        }

        private void ExecuteClearGroupsCommand(object sender, EventArgs e)
        {
            Package.DocumentManager?.ClearGroups();
            //Package.UpdateCommandsUI();
        }
    }
}
