using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
using System;
using System.Runtime.InteropServices;
using System.Threading;

namespace ChatGptVsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("AI Assistant", "Multi-provider AI coding assistant for Visual Studio", "1.0")]
    [ProvideMenuResource("Menus.ctmenu", 1)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.NoSolution_string,  PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideAutoLoad(VSConstants.UICONTEXT.SolutionExists_string, PackageAutoLoadFlags.BackgroundLoad)]
    [ProvideToolWindow(typeof(ChatGptToolWindow), Style = VsDockStyle.Tabbed,
                       Window = EnvDTE.Constants.vsWindowKindOutput)]
    // Register Options page: Tools > Options > AI Assistant
    [ProvideOptionPage(typeof(ChatGptOptionsPage), "AI Assistant", "Provider & Model", 0, 0, true)]
    [Guid(PackageGuidString)]
    public sealed class ChatGptPackage : AsyncPackage
    {
        public const string PackageGuidString = "7c21f4d1-6dc0-4b67-b2b2-29d5f8d3f4a1";

        protected override async System.Threading.Tasks.Task InitializeAsync(
            CancellationToken cancellationToken,
            IProgress<ServiceProgressData> progress)
        {
            await JoinableTaskFactory.SwitchToMainThreadAsync(cancellationToken);
            await ShowChatGptToolWindowCommand.InitializeAsync(this);
            await AskChatGptSelectionCommand.InitializeAsync(this);
            await AskChatGptFixErrorsCommand.InitializeAsync(this);
        }
    }
}
