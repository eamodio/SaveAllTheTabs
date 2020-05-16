using System;
using System.ComponentModel.Design;
using System.Linq;
using System.Runtime.InteropServices;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SaveAllTheTabs.Polyfills;

namespace SaveAllTheTabs.Commands
{
    /// <summary>
    /// Command handler
    /// </summary>
    internal class PackageCommands
    {
        [Guid(PackageGuids.CmdSetGuidString)]
        private enum CommandIds
        {
            SaveTabs = 0x0100,
            RestoreTabsListMenu = 0x0200
        }

        [Guid(PackageGuids.SavedTabsWindowCmdSetGuidString)]
        private enum SavedTabsWindowCommandIds
        {
            SavedTabsWindow = 0x0100
        }

        [Guid(PackageGuids.StashCmdSetGuidString)]
        private enum StashCommandIds
        {
            StashSaveTabs = 0x0100,
            StashRestoreTabs = 0x0200
        }

        private SaveAllTheTabsPackage Package { get; }
        private IServiceProvider ServiceProvider => Package;

        /// <summary>
        /// Gets the instance of the command.
        /// </summary>
        public static PackageCommands Instance { get; private set; }

        /// <summary>
        /// Initializes the singleton instance of the command.
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        public static void Initialize(SaveAllTheTabsPackage package)
        {
            Instance = new PackageCommands(package);
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="PackageCommands"/> class.
        /// Adds our command handlers for menu (commands must exist in the command table file)
        /// </summary>
        /// <param name="package">Owner package, not null.</param>
        private PackageCommands(SaveAllTheTabsPackage package)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
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
            ThreadHelper.ThrowIfNotOnUIThread();
            var guid = typeof(CommandIds).GUID;

            var commandId = new CommandID(guid, (int)CommandIds.SaveTabs);
            var command = new OleMenuCommand(ExecuteSaveTabsCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+D,Ctrl+S", "Text Editor::Ctrl+D,Ctrl+S");

            commandId = new CommandID(guid, (int)CommandIds.RestoreTabsListMenu);
            command = new OleMenuCommand(null, commandId);
            commandService.AddCommand(command);

            new RestoreTabsListCommands(Package).SetupCommands(commandService);

            guid = typeof(SavedTabsWindowCommandIds).GUID;

            commandId = new CommandID(guid, (int)SavedTabsWindowCommandIds.SavedTabsWindow);
            command = new OleMenuCommand(ExecuteSavedTabsWindowCommand, commandId);
            commandService.AddCommand(command);
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+D,Ctrl+W", "Text Editor::Ctrl+D,Ctrl+W");

            guid = typeof(StashCommandIds).GUID;

            commandId = new CommandID(guid, (int)StashCommandIds.StashSaveTabs);
            command = new OleMenuCommand(ExecuteStashSaveTabsCommand, commandId);
            command.BeforeQueryStatus += CommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
            Package.Environment.SetKeyBindings(command, "Global::Ctrl+D,Ctrl+C", "Text Editor::Ctrl+D,Ctrl+C");

            commandId = new CommandID(guid, (int)StashCommandIds.StashRestoreTabs);
            command = new OleMenuCommand(ExecuteStashRestoreTabsCommand, commandId);
            command.BeforeQueryStatus += StashRestoreTabsCommandOnBeforeQueryStatus;
            commandService.AddCommand(command);
            Package.Environment.SetKeyBindings(command,
                                               "Global::Ctrl+D,Ctrl+V", "Text Editor::Ctrl+D,Ctrl+V",
                                               "Global::Ctrl+D,`", "Text Editor::Ctrl+D,`");
        }

        private void CommandOnBeforeQueryStatus(object sender, EventArgs eventArgs)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            var command = sender as OleMenuCommand;
            if (command == null)
            {
                return;
            }

            command.Enabled = Package.DocumentManager != null && Package.Environment.GetDocumentWindows().Any();
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

        private void ExecuteSavedTabsWindowCommand(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            // Get the instance number 0 of this tool window. This window is single instance so this instance
            // is actually the only one.
            // The last flag is set to true so that if the tool window does not exists it will be created.
            var window = Package.FindToolWindow(typeof(SavedTabsToolWindow), 0, true);
            if (window?.Frame == null)
            {
                throw new NotSupportedException("Cannot create tool window");
            }

            var windowFrame = (IVsWindowFrame)window.Frame;
            ErrorHandler.ThrowOnFailure(windowFrame.Show());
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
