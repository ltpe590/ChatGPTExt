using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.TextManager.Interop;

namespace ChatGptVsix.Services
{
    public sealed class EditorContextService : IEditorContextService
    {
        private readonly AsyncPackage _package;

        public EditorContextService(AsyncPackage package) => _package = package;

        public async Task<EditorContext> GetCurrentAsync(int surroundingLineCount, CancellationToken ct)
        {
            await ThreadHelper.JoinableTaskFactory.SwitchToMainThreadAsync(ct);

            var componentModel = await _package.GetServiceAsync(typeof(SComponentModel)) as IComponentModel;
            if (componentModel == null)
                return new EditorContext();

            var editorAdapters = componentModel.GetService<IVsEditorAdaptersFactoryService>();
            var textManagerObj = await _package.GetServiceAsync(typeof(SVsTextManager));
            var textManager = textManagerObj as IVsTextManager;
            if (textManager == null)
                return new EditorContext();

            if (textManager.GetActiveView(1, null, out IVsTextView vsView) != 0 || vsView == null)
                return new EditorContext();

            IWpfTextView? wpfView = editorAdapters.GetWpfTextView(vsView);
            if (wpfView == null)
                return new EditorContext();

            var snapshot = wpfView.TextSnapshot;
            var selection = wpfView.Selection;

            string? selectionText = selection.IsEmpty ? null : selection.StreamSelectionSpan.GetText();

            // Determine selection line range
            int? startLine = null, endLine = null;
            if (!selection.IsEmpty)
            {
                var start = selection.Start.Position.GetContainingLine().LineNumber;
                var end = selection.End.Position.GetContainingLine().LineNumber;
                startLine = start;
                endLine = end;
            }
            else
            {
                var caretLine = wpfView.Caret.Position.BufferPosition.GetContainingLine().LineNumber;
                startLine = caretLine;
                endLine = caretLine;
            }

            // Surrounding window
            int s = Math.Max(0, (startLine ?? 0) - surroundingLineCount);
            int e = Math.Min(snapshot.LineCount - 1, (endLine ?? 0) + surroundingLineCount);

            var startPos = snapshot.GetLineFromLineNumber(s).Start.Position;
            var endPos = snapshot.GetLineFromLineNumber(e).End.Position;
            string surrounding = snapshot.GetText(startPos, endPos - startPos);

            // Best-effort file path
            string? filePath = null;

            if (vsView is IVsTextView textView)
            {
                textView.GetBuffer(out IVsTextLines lines);
                if (lines is IVsUserData userData)
                {
                    var guid = typeof(IVsUserData).GUID;
                    userData.GetData(ref guid, out object data);
                }
            }

            return new EditorContext
            {
                FilePath = filePath,
                ContentType = snapshot.TextBuffer.ContentType?.DisplayName,
                SelectionText = selectionText,
                SurroundingText = surrounding,
                SelectionStartLine = startLine,
                SelectionEndLine = endLine
            };
        }
    }
}