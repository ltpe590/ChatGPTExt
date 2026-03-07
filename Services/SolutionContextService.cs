using EnvDTE;
using EnvDTE80;
using Microsoft.VisualStudio.Shell;
using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix.Services
{
    public sealed class SolutionFile
    {
        public string Project      { get; set; } = "";
        public string FilePath     { get; set; } = "";
        public string RelativePath { get; set; } = "";
        public string Extension    { get; set; } = "";
    }

    /// <summary>
    /// Traverses the DTE Solution tree and exposes the file list + content
    /// for use in richer LLM prompts (e.g. "explain this solution", "generate a new class").
    /// </summary>
    public sealed class SolutionContextService
    {
        private readonly AsyncPackage _package;

        public SolutionContextService(AsyncPackage package) => _package = package;

        /// <summary>Returns all source files in the solution, optionally filtered by extension.</summary>
        public async Task<IReadOnlyList<SolutionFile>> GetFilesAsync(
            string[]? extensionFilter, CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            var result = new List<SolutionFile>();
            try
            {
                var dte      = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
                var solution = dte?.Solution;
                if (solution == null) return result;

                string? solutionDir = null;
                try { solutionDir = Path.GetDirectoryName(solution.FullName); } catch { }

                foreach (Project project in solution.Projects)
                    CollectFiles(project.ProjectItems, project.Name, solutionDir, extensionFilter, result);
            }
            catch { /* best-effort */ }

            return result;
        }

        /// <summary>
        /// Reads up to maxFiles source files and returns their content as a
        /// single context block for LLM prompts.
        /// </summary>
        public async Task<string> BuildContextBlockAsync(
            string[]? extensionFilter, int maxFiles, int maxLinesPerFile, CancellationToken ct)
        {
            var files = await GetFilesAsync(extensionFilter, ct).ConfigureAwait(false);

            var sb    = new System.Text.StringBuilder();
            int count = 0;

            foreach (var f in files)
            {
                if (count >= maxFiles) break;
                if (!File.Exists(f.FilePath)) continue;

                try
                {
                    var lines = File.ReadAllLines(f.FilePath);
                    int take  = Math.Min(lines.Length, maxLinesPerFile);

                    sb.AppendLine($"// FILE: {f.RelativePath}  (project: {f.Project})");
                    sb.AppendLine("```csharp");
                    for (int i = 0; i < take; i++)
                        sb.AppendLine(lines[i]);
                    if (take < lines.Length)
                        sb.AppendLine($"// ... ({lines.Length - take} more lines truncated)");
                    sb.AppendLine("```");
                    sb.AppendLine();
                    count++;
                }
                catch { /* skip unreadable files */ }
            }

            return sb.ToString();
        }

        // ── Helpers ───────────────────────────────────────────────────────────

        private static string MakeRelative(string basePath, string fullPath)
        {
            // .NET 4.7.2 compatible relative path helper
            try
            {
                if (string.IsNullOrEmpty(basePath)) return fullPath;
                var baseUri = new Uri(basePath.TrimEnd(Path.DirectorySeparatorChar) + Path.DirectorySeparatorChar);
                var fileUri = new Uri(fullPath);
                return Uri.UnescapeDataString(baseUri.MakeRelativeUri(fileUri).ToString())
                           .Replace('/', Path.DirectorySeparatorChar);
            }
            catch { return fullPath; }
        }

        private static void CollectFiles(
            ProjectItems? items,
            string projectName,
            string? solutionDir,
            string[]? extensionFilter,
            List<SolutionFile> result)
        {
            ThreadHelper.ThrowIfNotOnUIThread();
            if (items == null) return;

            foreach (ProjectItem item in items)
            {
                try
                {
                    // Recurse into sub-items (folders, sub-projects)
                    if (item.ProjectItems != null && item.ProjectItems.Count > 0)
                        CollectFiles(item.ProjectItems, projectName, solutionDir, extensionFilter, result);

                    if (item.FileCount < 1) continue;
                    var path = item.FileNames[1];
                    if (string.IsNullOrEmpty(path) || !File.Exists(path)) continue;

                    var ext = Path.GetExtension(path).ToLowerInvariant();
                    if (extensionFilter != null && extensionFilter.Length > 0)
                    {
                        bool match = false;
                        foreach (var e in extensionFilter)
                            if (string.Equals(ext, e, StringComparison.OrdinalIgnoreCase)) { match = true; break; }
                        if (!match) continue;
                    }

                    string relative = solutionDir != null ? MakeRelative(solutionDir, path) : path;

                    result.Add(new SolutionFile
                    {
                        Project      = projectName,
                        FilePath     = path,
                        RelativePath = relative,
                        Extension    = ext
                    });
                }
                catch { /* skip problem items */ }
            }
        }
    }
}
