using ChatGptVsix.Services;
using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.ComponentModel.Design;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix
{
    internal sealed class AskChatGptFixErrorsCommand
    {
        public const int CommandId = 0x0102;
        public static readonly Guid CommandSet = new Guid("9e64f5c5-0c65-4a2a-9c6b-9b1c2d0d7b0f");

        private readonly AsyncPackage _package;

        private AskChatGptFixErrorsCommand(AsyncPackage package, OleMenuCommandService commandService)
        {
            _package = package;
            var id   = new CommandID(CommandSet, CommandId);
            commandService.AddCommand(new MenuCommand((_, __) =>
                ThreadHelper.JoinableTaskFactory.Run(ExecuteAsync), id));
        }

        public static async Task InitializeAsync(AsyncPackage package)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();
            var cs = await package.GetServiceAsync(typeof(IMenuCommandService)) as OleMenuCommandService;
            if (cs != null) _ = new AskChatGptFixErrorsCommand(package, cs);
        }

        private async Task ExecuteAsync()
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync();

            // 1. Get active file path
            var dte      = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
            var filePath = dte?.ActiveDocument?.FullName;

            // 2. Read errors for this file (or all errors if no file open)
            var errorSvc = new ErrorContextService(_package);
            var errors   = await errorSvc.GetErrorsAsync(filePath, CancellationToken.None)
                                         .ConfigureAwait(true);

            if (errors.Count == 0)
            {
                // No errors — show tool window with message
                var w = await _package.ShowToolWindowAsync(
                    typeof(ChatGptToolWindow), 0, true, _package.DisposalToken);
                if (w?.Content is ChatGptToolWindowControl ui)
                    ui.ResponseBox.Text = "No errors or warnings found in the error list for this file.";
                return;
            }

            // 3. Read file content
            string fileContent = "";
            if (!string.IsNullOrEmpty(filePath) && File.Exists(filePath))
            {
                try { fileContent = File.ReadAllText(filePath); }
                catch { fileContent = "(could not read file)"; }
            }

            // 4. Build prompt
            var prompt = BuildFixPrompt(filePath, fileContent, errors);

            // 5. Show tool window, set fix target, and send
            var window = await _package.ShowToolWindowAsync(
                typeof(ChatGptToolWindow), 0, true, _package.DisposalToken);

            if (window?.Content is ChatGptToolWindowControl control)
            {
                control.SetFixTarget(filePath);  // enables Apply Fix button after response
                await control.SendAsync(prompt).ConfigureAwait(true);
            }
        }

        private static string BuildFixPrompt(
            string? filePath,
            string fileContent,
            IReadOnlyList<Services.ErrorItem> errors)
        {
            var sb = new StringBuilder();

            sb.AppendLine("You are a C# expert embedded in Visual Studio.");
            sb.AppendLine("Fix ALL the compiler errors and warnings listed below.");
            sb.AppendLine();

            if (!string.IsNullOrWhiteSpace(filePath))
                sb.AppendLine($"File: {filePath}");

            sb.AppendLine();
            sb.AppendLine($"## Errors and Warnings ({errors.Count} total)");
            sb.AppendLine();

            foreach (var err in errors)
                sb.AppendLine($"- [{err.Severity.ToUpper()}] Line {err.Line}, Col {err.Column}: {err.Message}");

            sb.AppendLine();
            sb.AppendLine("## Source File");
            sb.AppendLine();
            sb.AppendLine("```csharp");
            sb.AppendLine(fileContent);
            sb.AppendLine("```");
            sb.AppendLine();
            sb.AppendLine("## Instructions");
            sb.AppendLine("1. Fix every error and warning listed above.");
            sb.AppendLine("2. Return the COMPLETE corrected file inside a single ```csharp block.");
            sb.AppendLine("3. After the code block, add a brief bullet list of what you changed.");
            sb.AppendLine("4. Do not change any logic or behaviour beyond what is needed to fix the errors.");

            return sb.ToString();
        }
    }
}
