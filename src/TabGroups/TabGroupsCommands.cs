using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio.Shell;

namespace TabGroups
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TabGroupsCommands
    {
        [Guid(TabGroupsPackageGuids.CmdSetGuidString)]
        enum CommandIds
        {
            SaveGroup = 0x0100,
            GroupListMenu = 0x0200,
            ClearGroups = 0x0300
        }

        [Guid(TabGroupsPackageGuids.GroupsCmdSetGuidString)]
        private enum GroupListCommandIds
        {
            ApplyGroupStart = 0x0100,
            ApplyGroupEnd = ApplyGroupStart + 8,
            ApplyGroupPlaceholder = 0x0200
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

            commandId = new CommandID(guid, (int)CommandIds.GroupListMenu);
            command = new OleMenuCommand(null, commandId);
            commandService.AddCommand(command);

            commandId = new CommandID(guid, (int)CommandIds.ClearGroups);
            command = new OleMenuCommand(ExecuteClearGroupsCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            SetupApplyGroupCommands(commandService);
        }

        private void SetupApplyGroupCommands(OleMenuCommandService commandService)
        {
            if (Package.DocumentManager == null)
            {
                return;
            }

            var guid = typeof(GroupListCommandIds).GUID;

            CommandID commandId;
            OleMenuCommand command;

            for (var i = (int)GroupListCommandIds.ApplyGroupStart; i <= (int)GroupListCommandIds.ApplyGroupEnd; i++)
            {
                commandId = new CommandID(guid, i);
                command = new OleMenuCommand(ExecuteApplyGroupCommand, commandId);
                command.BeforeQueryStatus += ApplyGroupCommandOnBeforeQueryStatus;
                commandService.AddCommand(command);
            }

            commandId = new CommandID(guid, (int)GroupListCommandIds.ApplyGroupPlaceholder);
            command = new OleMenuCommand(null, commandId);
            command.BeforeQueryStatus += ApplyGroupPlaceholderCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private const string GroupNameFormat = "Group{0}";
        private static string GetGroupName(int index) => string.Format(GroupNameFormat, index);
        private static int GetApplyGroupCommandIndex(OleMenuCommand command) => command.CommandID.ID - (int)GroupListCommandIds.ApplyGroupStart;

        private void ExecuteSaveGroupCommand(object sender, EventArgs e)
        {
            var name = GetGroupName((Package.DocumentManager?.GroupCount ?? 0) + 1);
            Package.DocumentManager?.SaveGroup(name);
            //Package.UpdateCommandsUI();
        }

        private void ExecuteClearGroupsCommand(object sender, EventArgs e)
        {
            Package.DocumentManager?.ClearGroups();
            //Package.UpdateCommandsUI();
        }

        private void ExecuteApplyGroupCommand(object sender, EventArgs e)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            var index = GetApplyGroupCommandIndex(command);
            if (index == -1)
            {
                return;
            }
            Package.DocumentManager?.ApplyGroup(index);
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

        private void ApplyGroupCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            var index = GetApplyGroupCommandIndex(command);
            var group = Package.DocumentManager.GetGroup(index);
            if (group != null)
            {
                command.Text = $"{index + 1} {group.Name}";
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
            command.Visible = Package.DocumentManager?.GroupCount == 0;
        }
    }
}
