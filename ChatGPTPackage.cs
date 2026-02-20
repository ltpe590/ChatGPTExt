using System;
using System.Runtime.InteropServices;
using System.Threading;
using Microsoft.VisualStudio.Shell;

namespace ChatGptVsix
{
    [PackageRegistration(UseManagedResourcesOnly = true, AllowsBackgroundLoading = true)]
    [InstalledProductRegistration("ChatGPT VSIX", "ChatGPT tool window + selection command", "1.0")]
    [ProvideMenuResource("Commands.ctmenu", 1)]
    [ProvideToolWindow(typeof(ChatGptToolWindow), Style = VsDockStyle.Tabbed, Window = EnvDTE.Constants.vsWindowKindOutput)]
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
        }
    }
}
