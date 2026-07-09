using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;

namespace StapleExtensionsVS
{
    internal sealed class AttachToStapleEditorCommand
    {
        public const int CommandId = 0x0100;

        public static readonly string EditorProcessName = "Staple.Editor.App";

        public static readonly Guid CommandSet = new Guid("e545ebd8-4bd1-4a57-a3e2-5cf6c46afc91");

        private readonly AsyncPackage package;

        private AttachToStapleEditorCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            this.package = package ?? throw new ArgumentNullException(nameof(package));

            commandService = commandService ?? throw new ArgumentNullException(nameof(commandService));

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand(Execute, menuCommandID);

            commandService.AddCommand(menuItem);
        }

        public static AttachToStapleEditorCommand Instance { get; private set; }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(package.DisposalToken);

            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;

            Instance = new AttachToStapleEditorCommand(package, commandService);
        }

        private void Execute(object sender, EventArgs e)
        {
            ThreadHelper.ThrowIfNotOnUIThread();

            var dte = (DTE2)Package.GetGlobalService(typeof(DTE));

            dte.Solution.SolutionBuild.Build(true);

            var processes = dte.Debugger.LocalProcesses;

            foreach (Process proc in processes)
            {
                if (proc.Name.IndexOf(EditorProcessName, StringComparison.OrdinalIgnoreCase) >= 0)
                {
                    proc.Attach();

                    return;
                }
            }

            VsShellUtilities.ShowMessageBox(
                package,
                $"{EditorProcessName} process not found.",
                "Attach Failed",
                OLEMSGICON.OLEMSGICON_WARNING,
                OLEMSGBUTTON.OLEMSGBUTTON_OK,
                OLEMSGDEFBUTTON.OLEMSGDEFBUTTON_FIRST);
        }
    }
}
