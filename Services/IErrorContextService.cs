using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix.Services
{
    public sealed class ErrorItem
    {
        public string File      { get; init; } = "";
        public int    Line      { get; init; }
        public int    Column    { get; init; }
        public string Code      { get; init; } = "";
        public string Message   { get; init; } = "";
        public string Severity  { get; init; } = "error"; // "error" | "warning"
        public string Project   { get; init; } = "";
    }

    public interface IErrorContextService
    {
        /// <summary>Returns all errors/warnings, optionally filtered to a specific file path.</summary>
        Task<IReadOnlyList<ErrorItem>> GetErrorsAsync(string? filePathFilter, CancellationToken ct);
    }
}
