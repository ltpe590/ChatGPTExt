using System.Threading;
using System.Threading.Tasks;

namespace ChatGptVsix.Services
{
    public sealed class EditorContext
    {
        public string? FilePath { get; init; }
        public string? ContentType { get; init; }   // e.g. "CSharp"
        public string? SelectionText { get; init; }
        public string? SurroundingText { get; init; } // e.g. ~200-400 lines window
        public int? SelectionStartLine { get; init; }
        public int? SelectionEndLine { get; init; }
    }

    public interface IEditorContextService
    {
        Task<EditorContext> GetCurrentAsync(int surroundingLineCount, CancellationToken ct);
    }
}