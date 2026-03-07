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
    public sealed class ErrorContextService : IErrorContextService
    {
        private readonly AsyncPackage _package;

        public ErrorContextService(AsyncPackage package) => _package = package;

        public async Task<IReadOnlyList<ErrorItem>> GetErrorsAsync(string? filePathFilter, CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            var result = new List<ErrorItem>();
            try
            {
                var dte2      = await _package.GetServiceAsync(typeof(DTE)) as DTE2;
                var errorList = dte2?.ToolWindows?.ErrorList;
                if (errorList == null) return result;

                var items = errorList.ErrorItems;
                for (int i = 1; i <= items.Count; i++)
                {
                    var item = items.Item(i);

                    if (!string.IsNullOrEmpty(filePathFilter))
                    {
                        try
                        {
                            if (!string.Equals(
                                Path.GetFullPath(item.FileName ?? ""),
                                Path.GetFullPath(filePathFilter),
                                StringComparison.OrdinalIgnoreCase))
                                continue;
                        }
                        catch { continue; }
                    }

                    string severity = item.ErrorLevel switch
                    {
                        vsBuildErrorLevel.vsBuildErrorLevelHigh   => "error",
                        vsBuildErrorLevel.vsBuildErrorLevelMedium => "warning",
                        _                                          => "message"
                    };
                    if (severity == "message") continue;

                    result.Add(new ErrorItem
                    {
                        File     = item.FileName ?? "",
                        Line     = item.Line,
                        Column   = item.Column,
                        Code     = "",  // DTE ErrorItem does not expose error code
                        Message  = item.Description ?? "",
                        Severity = severity,
                        Project  = item.Project ?? ""
                    });
                }
            }
            catch { /* best-effort */ }

            return result;
        }
    }
}
