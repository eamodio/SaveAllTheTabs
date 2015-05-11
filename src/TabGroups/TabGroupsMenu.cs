//------------------------------------------------------------------------------
// <copyright file="TabGroupsMenu.cs" company="Company">
//     Copyright (c) Company.  All rights reserved.
// </copyright>
//------------------------------------------------------------------------------

using System;
using System.ComponentModel.Design;
using Microsoft.VisualStudio.Shell;

namespace TabGroups
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal sealed class TabGroupsMenu
    {
        enum CommandIds
        {
            SaveGroup1 = 0x0100,
            RestoreGroup1 = 0x0200
        }

        /// <summary>
        /// Command ID.
        /// </summary>
        public const int CommandId = 0x1020;

        /// <summary>
        /// Command menu group (command set GUID).
        /// </summary>
        public static readonly Guid MenuGroup = new Guid("54ea2b58-3ea2-489f-b4eb-3b7e88a663c2");
        public static readonly Guid TabGroupsMenuGroup = new Guid("288739ea-2a43-496d-84f9-329a51d47bdc");

        /// <summary>
        /// VS Package that provides this command, not null.
        /// </summary>
        private TabGroupsPackage Package { get; }

        /// <summary>
        /// Gets the service provider from the owner package.
        /// </summary>
        private IServiceProvider ServiceProvider => Package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static TabGroupsMenu Instance { get; private set; }

        /// <summary>
        /// Initializes a new instance of the <see cref="TabGroupsMenu"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private TabGroupsMenu(TabGroupsPackage package)
        {
            if (package == null)
            {
                throw new ArgumentNullException(nameof(package));
            }

            Package = package;

            var commandService = ServiceProvider.GetService(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (commandService != null)
            {
                var commandId = new CommandID(MenuGroup, CommandId);
                var command = new OleMenuCommand(null, commandId);
                command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
                commandService.AddCommand(command);

                SetupCommands(commandService);
            }
        }

        private void SetupCommands(OleMenuCommandService commandService)
        {
            var commandId = new CommandID(TabGroupsMenuGroup, (int)CommandIds.SaveGroup1);
            var command = new OleMenuCommand(ExecuteSaveCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);

            commandId = new CommandID(TabGroupsMenuGroup, (int)CommandIds.RestoreGroup1);
            command = new OleMenuCommand(ExecuteRestoreCommand, commandId);
            command.BeforeQueryStatus += RestoreCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
        }

        private void ExecuteSaveCommand(object sender, EventArgs e)
        {
            Package.DocumentManager?.Save(1);
        }

        private void ExecuteRestoreCommand(object sender, EventArgs e)
        {
            Package.DocumentManager?.Restore(1);
        }

        private void CommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var commandOle = sender as OleMenuCommand;
            if (commandOle == null)
            {
                return;
            }

            commandOle.Enabled = Package.DocumentManager != null;
        }

        private void RestoreCommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            var commandOle = sender as OleMenuCommand;
            if (commandOle == null)
            {
                return;
            }

            commandOle.Enabled = (Package.DocumentManager?.CanRestore(1)).GetValueOrDefault();
        }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(TabGroupsPackage package)
        {
            Instance = new TabGroupsMenu(package);
        }
    }
}
