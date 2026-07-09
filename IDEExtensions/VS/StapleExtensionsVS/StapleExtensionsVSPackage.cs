using Microsoft.VisualStudio.Shell;
using System;
using System.Runtime.InteropServices;
using System.Threading;
using System.Threading.Tasks;

namespace StapleExtensionsVS
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [Guid(PackageGuidString)]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    public sealed class StapleExtensionsVSPackage : AsyncPackage
    {
        public const string PackageGuidString = "3273bc85-0cfd-43bb-b6c7-3622dd8e3317";

#region Package Members
        protected override async Task InitializeAsync(CancellationToken cancellationToken, IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await AttachToStapleEditorCommand.InitializeAsync(this);
        }
#endregion
    }
}
