using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Drawing.Design;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using Task = System.Threading.Tasks.Task;


namespace DevExpress13_1ControlInVisualStudio15
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
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    //[ProvideAutoLoad(UIContextGuids80.ToolboxInitialized, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideToolboxItems(1)]
    [InstalledProductRegistration("#110", "#112", "1.0", IconResourceID = 400)] // Info on this package for Help/About
    [Guid(DevExpress13_1ControlsInstallerPackage.PackageGuidString)]
    [SuppressMessage("StyleCop.CSharp.DocumentationRules", "SA1650:ElementDocumentationMustBeSpelledCorrectly", Justification = "pkgdef, VS and vsixmanifest are valid VS terms")]
    public sealed class DevExpress13_1ControlsInstallerPackage : AsyncPackage
    {
        /// <summary>
        /// Devexpress13_1ControlsInstallerPackage GUID string.
        /// </summary>
        public const string PackageGuidString = "51ecf07a-13c0-4a4d-8032-c452534bbdcb";

        /// <summary>
        /// 
        /// </summary>
        private readonly List<ToolboxItem> _items = new List<ToolboxItem>();

        private IToolboxService _tbxService;

        /// <summary>
        /// Initializes a new instance of the <see cref="DevExpress13_1ControlsInstallerPackage"/> class.
        /// </summary>
        public DevExpress13_1ControlsInstallerPackage()
        {
            // Inside this method you can place any initialization code that does not require
            // any Visual Studio service because at this point the package object is created but
            // not sited yet inside Visual Studio environment. The place to do all the other
            // initialization is the Initialize method.
            ToolboxInitialized += new EventHandler(PackageWinformsToolbox_ToolboxInitialized);
            ToolboxUpgraded += new EventHandler(PackageWinformsToolbox_ToolboxUpgraded);
        }

        #region Package Members

        /// <summary>
        /// Initialization of the package; this method is called right after the package is sited, so this is the place
        /// where you can put all the initialization code that rely on services provided by VisualStudio.
        /// </summary>
        /// <param name="cancellationToken">A cancellation token to monitor for initialization cancellation, which can occur when VS is shutting down.</param>
        /// <param name="progress">A provider for progress updates.</param>
        /// <returns>A task representing the async work of package initialization, or an already completed task if there is none. Do not return null from this method.</returns>
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            _tbxService = (IToolboxService)(await GetServiceAsync(typeof(IToolboxService)));

            IEnumerable<string> devexpressPaths = Directory.GetDirectories(@"C:\Windows\Microsoft.NET\assembly\GAC_MSIL")
                .Where(dir => Path.GetFileName(dir).StartsWith("DevExpress"))
                .ToArray() ;

            foreach (var devexpressPath in devexpressPaths)
            {
                foreach (var file in GetDllPath(devexpressPath))
                {
                    Assembly assembly = Assembly.LoadFile(file);
                    foreach (ToolboxItem item in ToolboxService.GetToolboxItems(assembly, null, false))
                    {
                        _items.Add(item);
                    }
                }
            };

            IEnumerable<string> GetDllPath(string path)
            {
                foreach (var dllPath in Directory.GetFiles(path).Where(f => Path.GetExtension(f) == ".dll"))
                {
                    yield return dllPath;
                }

                foreach (var childPath in Directory.GetDirectories(path))
                {
                    foreach (var dllPath in GetDllPath(childPath))
                    {
                        yield return dllPath;
                    }
                }
            }

            //_items.Clear();

            //foreach (var file in Directory.GetFiles(@"D:\Program Files (x86)\DevExpress 13.2\Components\Bin\Framework"))
            //{
            //    if (Path.GetExtension(file) == ".dll")
            //    {
            //        Assembly assembly = Assembly.LoadFile(file);
            //        foreach (ToolboxItem item in ToolboxService.GetToolboxItems(assembly, null, false))
            //        {
            //            //if (item.Filter.Cast<ToolboxItemFilterAttribute>().Any(f => f.FilterString.StartsWith("DevExpress")))
            //            //{
            //            _items.Add(item);
            //            //}
            //        }
            //    }
            //}

            // When initialized asynchronously, the current thread may be a background thread at this point.
            // Do any initialization that requires the UI thread after switching to the UI thread.
            await this.JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
        }

        #endregion

        /// <summary>
        /// This method is called when the toolbox content version (the parameter to the ProvideToolboxItems
        /// attribute) changes.  This tells Visual Studio that items may have changed 
        /// and need to be reinstalled.
        /// </summary>
        private void PackageWinformsToolbox_ToolboxUpgraded(object sender, EventArgs e)
        {
            RemoveToolboxItems();
            InstallToolboxItems();
        }

        /// <summary>
        /// This method will add items to the toolbox.  It is called the first time the toolbox
        /// is used after this package has been installed.
        /// </summary>
        private void PackageWinformsToolbox_ToolboxInitialized(object sender, EventArgs e)
        {
            InstallToolboxItems();
        }

        /// <summary>
        /// Removes all the toolbox items installed by this package (those which came from this
        /// assembly).
        /// </summary>
        private void RemoveToolboxItems()
        {
            foreach (var item in _items)
            {
                _tbxService.RemoveToolboxItem(item);
            }
        }

        /// <summary>
        /// Installs all the toolbox items defined in this assembly.
        /// </summary>
        private void InstallToolboxItems()
        {
            // For demonstration purposes, this assembly includes toolbox items and loads them from itself.
            // It is of course possible to load toolbox items from a different assembly by either:
            // a)  loading the assembly yourself and calling ToolboxService.GetToolboxItems
            // b)  calling AssemblyName.GetAssemblyName("...") and then ToolboxService.GetToolboxItems(assemblyName)
            // D:\Program Files (x86)\DevExpress 13.2\Components\Bin\Framework

            foreach (ToolboxItem item in _items)
            {
                _tbxService.AddToolboxItem(item, (string)item.Properties["GroupName"]);
            }
        }
    }
}
