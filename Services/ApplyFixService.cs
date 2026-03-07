using EnvDTE;
using Microsoft.VisualStudio.Shell;
using System;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix.Services
{
    /// <summary>
    /// Parses a ```csharp code block from LLM response text and writes it
    /// back to the target file, then reloads the document in VS.
    /// </summary>
    public sealed class ApplyFixService
    {
        private static readonly Regex CodeBlockRegex = new Regex(
            @"```(?:csharp|cs)?\s*\r?\n(.*?)\r?\n```",
            RegexOptions.Singleline | RegexOptions.Compiled);

        private readonly AsyncPackage _package;

        public ApplyFixService(AsyncPackage package) => _package = package;

        /// <summary>
        /// Extracts code from responseText and writes it to filePath.
        /// Returns (success, message) tuple.
        /// </summary>
        public async Task<(bool Success, string Message)> ApplyAsync(
            string responseText, string filePath, CancellationToken ct)
        {
            if (string.IsNullOrWhiteSpace(filePath))
                return (false, "No target file path — open a file in VS first.");

            if (!File.Exists(filePath))
                return (false, $"File not found: {filePath}");

            var match = CodeBlockRegex.Match(responseText);
            if (!match.Success)
                return (false, "No ```csharp code block found in the AI response.\nMake sure the AI returned a complete file.");

            var newContent = match.Groups[1].Value;
            if (string.IsNullOrWhiteSpace(newContent))
                return (false, "Extracted code block is empty.");

            try
            {
                // Write the fixed content
                File.WriteAllText(filePath, newContent, System.Text.Encoding.UTF8);

                // Reload in VS so the editor reflects changes
                await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);
                var dte = await _package.GetServiceAsync(typeof(DTE)) as DTE;
                if (dte != null)
                {
                    foreach (Document doc in dte.Documents)
                    {
                        if (string.Equals(doc.FullName, filePath, StringComparison.OrdinalIgnoreCase))
                        {
                            doc.Close(vsSaveChanges.vsSaveChangesNo);
                            break;
                        }
                    }
                    dte.ItemOperations.OpenFile(filePath);
                }

                return (true, $"Fix applied to {Path.GetFileName(filePath)}");
            }
            catch (Exception ex)
            {
                return (false, $"Failed to write file: {ex.Message}");
            }
        }
    }
}
