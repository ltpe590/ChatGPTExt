using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading.Tasks;
using System.Windows.Documents;

namespace ChatGptVsix
{
    internal sealed class AskChatGptSelectionCommand
    {
        public const int CommandId = 0x0101;
        public static readonly Guid CommandSet = new Guid("9e64f5c5-0c65-4a2a-9c6b-9b1c2d0d7b0f");

        private readonly AsyncPackage _package;

        private AskChatGptSelectionCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;

            var menuCommandID = new CommandID(CommandSet, CommandId);
            var menuItem = new MenuCommand((_, __) =>
            {
                ThreadHelper.JoinableTaskFactory.Run(ExecuteAsync);
            }, menuCommandID);

            commandService.AddCommand(menuItem);
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var commandService = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            _ = new AskChatGptSelectionCommand(package, commandService);
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE;
            if (dte == null) return;
            if (dte?.ActiveDocument == null) return;

            var sel = dte.ActiveDocument.Selection as EnvDTE.TextSelection;
            var selectedText = sel?.Text;

            if (string.IsNullOrWhiteSpace(selectedText))
                selectedText = dte.ActiveDocument.Object("TextDocument") is TextDocument td ? td.StartPoint.CreateEditPoint().GetText(td.EndPoint) : "";

            // Show tool window
            var window = await _package.ShowToolWindowAsync(typeof(ChatGptToolWindow), 0, true, _package.DisposalToken);
            if (window?.Frame == null)
                return;

            if (window.Content is ChatGptToolWindowControl ui)
            {
                var prompt =
$@"Explain / review this code and suggest improvements:

```csharp
{selectedText}
```";
                await ui.SendAsync(prompt);
            }
        }
    }
}
