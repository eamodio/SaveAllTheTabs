using System;
using System.Diagnostics.CodeAnalysis;
using System.Runtime.InteropServices;
using System.Windows.Threading;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using SaveAllTheTabs.Commands;
using SaveAllTheTabs.Polyfills;

namespace SaveAllTheTabs
{
    /// <summary>
    /// This is the class that implements the package exposed by this assembly.
    /// </summary>
    /// <remarks>
    /// <para>
    /// The minimum requirement for a class to be considered a valid package for Visual Studio
    /// is to implement the IVsPackage interface and register itself with the shell.
    /// This package uses the helper classes defined inside the Managed Package Framework (MPF)
    /// to do it: it derives from the Package class that provides the implementation of the
    /// IVsPackage interface and uses the registration attributes defined in the framework to
    /// register itself and its components with the shell. These attributes tell the pkgdef creation
    /// utility what data to put into .pkgdef file.
    /// </para>
    /// <para>
    /// To get loaded into VS, the package must be referred by &lt;Asset Type="Microsoft.VisualStudio.VsPackage" ...&gt; in .vsixmanifest file.
    /// </para>
    /// </remarks>
    [PackageRegistration(UseManagedResourcesOnly = true)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [ProvideAutoLoad(UIContextGuids80.SolutionExists)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [Guid(PackageGuids.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    [ProvideToolWindow(typeof(SavedTabsToolWindow), Style = VsDockStyle.Tabbed, Window = PackageGuids.SolutionExploreWindowGuidString)]
    [ProvideService(typeof(PackageProviderService))]
    public sealed class SaveAllTheTabsPackage : Package
    {
        public event EventHandler SolutionChanged;

        internal static DTE2 Dte => _dte ?? (_dte = ServiceProvider.GlobalProvider.GetService(typeof(DTE)) as DTE2);
        private static DTE2 _dte;

        public DTE2 Environment => Dte;
        internal IDocumentManager DocumentManager { get; private set; }

        private PackageProviderService _packageProvider;
        private SolutionEvents _solutionEvents;

        /// <summary>
        /// Initializes a new instance of the <see cref="SaveAllTheTabsPackage"/> class.
        /// </summary>
        public SaveAllTheTabsPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
        }

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        protected async override void Initialize()
        {
            _packageProvider = new PackageProviderService(this);
            DocumentManager = new DocumentManager(this);

            PackageCommands.Initialize(this);

            base.Initialize();

            // Hook up event handlers
            await Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
            {
                // Must save the solution events, otherwise it seems to get GC'd
                _solutionEvents = Environment.Events.SolutionEvents;
                _solutionEvents.Opened += OnSolutionOpened;
                _solutionEvents.AfterClosing += OnSolutionClosed;

            }), DispatcherPriority.ApplicationIdle, null);
        }

        private void OnSolutionOpened()
        {
            SolutionChanged?.Invoke(this, EventArgs.Empty);
            UpdateCommandsUI(this);
        }

        private void OnSolutionClosed()
        {
            SolutionChanged?.Invoke(this, EventArgs.Empty);
            UpdateCommandsUI(this);
        }

        public void UpdateCommandsUI()
        {
            UpdateCommandsUI(this);
        }

        public static void UpdateCommandsUI(IServiceProvider sp)
        {
            var shell = (IVsUIShell)sp.GetService(typeof(IVsUIShell));
            if (shell == null)
            {
                return;
            }

            var hr = shell.UpdateCommandUI(0);
            ErrorHandler.ThrowOnFailure(hr);
        }
    }
}
