using ChatGptVsix.Services;
using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.ComponentModel.Design;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix
{
    internal sealed class AskChatGptSelectionCommand
    {
        public const int CommandId    = 0x0101; // Tools menu
        public const int CommandIdCtx = 0x0103; // Editor right-click
        public static readonly Guid CommandSet = new Guid("9e64f5c5-0c65-4a2a-9c6b-9b1c2d0d7b0f");

        private readonly AsyncPackage _package;

        private AskChatGptSelectionCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;

            // Register in Tools menu
            commandService.AddCommand(new MenuCommand(
                (_, __) => ThreadHelper.JoinableTaskFactory.Run(ExecuteAsync),
                new CommandID(CommandSet, CommandId)));

            // Register in editor right-click context menu (same handler)
            commandService.AddCommand(new MenuCommand(
                (_, __) => ThreadHelper.JoinableTaskFactory.Run(ExecuteAsync),
                new CommandID(CommandSet, CommandIdCtx)));
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs != null) _ = new AskChatGptSelectionCommand(package, cs);
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            int surroundingLines = 80;
            var page = (ChatGptOptionsPage)_package.GetDialogPage(typeof(ChatGptOptionsPage));
            if (page != null) surroundingLines = page.SurroundingLineCount;

            var contextService = new EditorContextService(_package);
            var ctx = await contextService.GetCurrentAsync(surroundingLines, CancellationToken.None)
                                          .ConfigureAwait(true);

            var window = await _package.ShowToolWindowAsync(
                typeof(ChatGptToolWindow), 0, true, _package.DisposalToken);

            if (window?.Content is not ChatGptToolWindowControl ui) return;

            var prompt = BuildPrompt(ctx);
            await ui.SendAsync(prompt).ConfigureAwait(true);
        }

        private static string BuildPrompt(EditorContext ctx)
        {
            var sb = new System.Text.StringBuilder();
            sb.AppendLine("You are a C# coding assistant embedded in Visual Studio.");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(ctx.FilePath))
            {
                sb.AppendLine("File     : " + ctx.FilePath);
                sb.AppendLine("Language : " + ctx.ContentType);
                sb.AppendLine();
            }

            if (!string.IsNullOrWhiteSpace(ctx.SelectionText))
            {
                sb.AppendLine("Selected code:");
                sb.AppendLine("```csharp");
                sb.AppendLine(ctx.SelectionText);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("1. Identify issues (correctness, performance, async/threading).");
                sb.AppendLine("2. Suggest improvements.");
                sb.AppendLine("3. Provide a corrected snippet (changed parts only).");
            }
            else if (!string.IsNullOrWhiteSpace(ctx.SurroundingText))
            {
                sb.AppendLine("Current file context:");
                sb.AppendLine("```csharp");
                sb.AppendLine(ctx.SurroundingText);
                sb.AppendLine("```");
                sb.AppendLine();
                sb.AppendLine("No specific selection. Please review the code and suggest improvements.");
            }
            else
            {
                sb.AppendLine("No code context available. Please ask your question.");
            }

            return sb.ToString();
        }
    }
}
