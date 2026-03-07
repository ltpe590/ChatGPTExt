using System;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.VisualStudio.ComponentModelHost;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Shell;
using Microsoft.VisualStudio.Shell.Interop;
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
            if (componentModel == null) return new EditorContext();

            var editorAdapters = componentModel.GetService<IVsEditorAdaptersFactoryService>();

            var textManager = await _package.GetServiceAsync(typeof(SVsTextManager)) as IVsTextManager;
            if (textManager == null) return new EditorContext();

            if (textManager.GetActiveView(1, null, out IVsTextView vsView) != 0 || vsView == null)
                return new EditorContext();

            IWpfTextView? wpfView = editorAdapters.GetWpfTextView(vsView);
            if (wpfView == null) return new EditorContext();

            var snapshot  = wpfView.TextSnapshot;
            var selection = wpfView.Selection;

            string? selectionText = selection.IsEmpty ? null : selection.StreamSelectionSpan.GetText();

            int? startLine, endLine;
            if (!selection.IsEmpty)
            {
                startLine = selection.Start.Position.GetContainingLine().LineNumber;
                endLine   = selection.End.Position.GetContainingLine().LineNumber;
            }
            else
            {
                var caretLine = wpfView.Caret.Position.BufferPosition.GetContainingLine().LineNumber;
                startLine = endLine = caretLine;
            }

            int s = Math.Max(0, (startLine ?? 0) - surroundingLineCount);
            int e = Math.Min(snapshot.LineCount - 1, (endLine ?? 0) + surroundingLineCount);

            var startPos  = snapshot.GetLineFromLineNumber(s).Start.Position;
            var endPos    = snapshot.GetLineFromLineNumber(e).End.Position;
            string surrounding = snapshot.GetText(startPos, endPos - startPos);

            // ── FilePath via IVsRunningDocumentTable ──────────────────────────
            string? filePath = null;
            try
            {
                vsView.GetBuffer(out IVsTextLines vsLines);
                if (vsLines != null)
                {
                    var rdt = await _package.GetServiceAsync(typeof(SVsRunningDocumentTable))
                                  as IVsRunningDocumentTable;
                    if (rdt != null)
                    {
                        rdt.GetRunningDocumentsEnum(out IEnumRunningDocuments rdtEnum);
                        if (rdtEnum != null)
                        {
                            uint[] cookies = new uint[1];
                            uint fetched;
                            while (rdtEnum.Next(1, cookies, out fetched) == 0 && fetched == 1)
                            {
                                rdt.GetDocumentInfo(cookies[0],
                                    out _, out _, out _, out string moniker,
                                    out _, out _, out IntPtr docDataPtr);

                                if (docDataPtr == IntPtr.Zero) continue;

                                var docObj = System.Runtime.InteropServices.Marshal
                                    .GetObjectForIUnknown(docDataPtr);
                                System.Runtime.InteropServices.Marshal.Release(docDataPtr);

                                if (object.ReferenceEquals(docObj, vsLines))
                                {
                                    filePath = moniker;
                                    break;
                                }
                            }
                        }
                    }
                }
            }
            catch { /* FilePath is best-effort — never break context capture */ }

            return new EditorContext
            {
                FilePath           = filePath,
                ContentType        = snapshot.TextBuffer.ContentType?.DisplayName,
                SelectionText      = selectionText,
                SurroundingText    = surrounding,
                SelectionStartLine = startLine,
                SelectionEndLine   = endLine
            };
        }
    }
}
