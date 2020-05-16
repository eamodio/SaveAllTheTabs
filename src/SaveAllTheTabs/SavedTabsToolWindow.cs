using System;
using System.ComponentModel.Design;
using System.Runtime.InteropServices;
using System.Windows.Forms;
using Microsoft.VisualStudio.Shell;
using SaveAllTheTabs.Commands;
using SaveAllTheTabs.Polyfills;

namespace SaveAllTheTabs
{
    /// <summary>
    /// This class implements the tool window exposed by this package and hosts a user control.
    /// </summary>
    /// <remarks>
    /// In Visual Studio tool windows are composed of a frame (implemented by the shell) and a pane,
    /// usually implemented by the package implementer.
    /// <para>
    /// This class derives from the ToolWindowPane class provided from the MPF in order to use its
    /// implementation of the IVsUIElementPane interface.
    /// </para>
    /// </remarks>
    [Guid("55627d91-9a2e-410e-8472-ac651ae62d7b")]
    public class SavedTabsToolWindow : ToolWindowPane
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="SavedTabsToolWindow"/> class.
        /// </summary>
        public SavedTabsToolWindow() : base(null)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            Caption = "Saved Tabs";

            // Set the image that will appear on the tab of the window frame
            // when docked with an other window
            // The resource ID correspond to the one defined in the resx file
            // while the Index is the offset in the bitmap strip. Each image in
            // the strip being 16x16.
            BitmapResourceID = 301;
            BitmapIndex = 1;

            var packageProvider = ServiceProvider.GlobalProvider.GetService(typeof(PackageProviderService)) as PackageProviderService;
            var package = packageProvider?.Package;

            var commands = new SavedTabsWindowCommands(package);
            ToolBar = commands.SetupToolbar();

            // This is the user control hosted by the tool window; Note that, even if this class implements IDisposable,
            // we are not calling Dispose on this object. This is because ToolWindowPane calls Dispose on
            // the object returned by the Content property.
            Content = new SavedTabsToolWindowControl(package, commands);
        }

        protected override bool PreProcessMessage(ref Message m)
        {
            // Stop the default handling of the ESC key
            if (m.Msg == 256 && m.LParam.ToInt32() == 65537 && m.WParam.ToInt32() == 27)
            {
                return true;
            }
            return base.PreProcessMessage(ref m);
        }
    }
}
