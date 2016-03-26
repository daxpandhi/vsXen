/*
The MIT License (MIT)

Copyright (c) 2015 Nukeation Studios.

Permission is hereby granted, free of charge, to any person obtaining a copy 
of this software and associated documentation files (the "Software"), to deal 
in the Software without restriction, including without limitation the rights 
to use, copy, modify, merge, publish, distribute, sublicense, and/or sell 
copies of the Software, and to permit persons to whom the Software is 
furnished to do so, subject to the following conditions:

The above copyright notice and this permission notice shall be included in 
all copies or substantial portions of the Software.

THE SOFTWARE IS PROVIDED "AS IS", WITHOUT WARRANTY OF ANY KIND, EXPRESS OR 
IMPLIED, INCLUDING BUT NOT LIMITED TO THE WARRANTIES OF MERCHANTABILITY, 
FITNESS FOR A PARTICULAR PURPOSE AND NONINFRINGEMENT. IN NO EVENT SHALL THE 
AUTHORS OR COPYRIGHT HOLDERS BE LIABLE FOR ANY CLAIM, DAMAGES OR OTHER 
LIABILITY, WHETHER IN AN ACTION OF CONTRACT, TORT OR OTHERWISE, ARISING 
FROM, OUT OF OR IN CONNECTION WITH THE SOFTWARE OR THE USE OR OTHER DEALINGS 
IN THE SOFTWARE.

https://github.com/daxpandhi/vsXen

*/

using System;
using System.Collections.ObjectModel;
using System.ComponentModel.Composition;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Linq;
using System.Text.RegularExpressions;
using System.Windows.Threading;
using Microsoft.VisualStudio;
using Microsoft.VisualStudio.Editor;
using Microsoft.VisualStudio.Language.Intellisense;
using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text;
using Microsoft.VisualStudio.Text.Editor;
using Microsoft.VisualStudio.Text.Projection;
using Microsoft.VisualStudio.TextManager.Interop;
using Microsoft.VisualStudio.Utilities;
using vsXen.XenCoding.Xaml;

namespace vsXen.Commands
{
    [SuppressMessage("ReSharper", "AutoPropertyCanBeMadeGetOnly.Global")]
    internal abstract class CommandTargetBase<TCommandEnum> : IOleCommandTarget where TCommandEnum : struct, IComparable
    {
        private IOleCommandTarget _nextCommandTarget;
        protected readonly IWpfTextView TextView;

        public Guid CommandGroup
        { get; set; }
        public ReadOnlyCollection<uint> CommandIds
        { get; }

        protected CommandTargetBase(IVsTextView adapter, IWpfTextView textView, params TCommandEnum[] commandIds)
            : this(adapter, textView, typeof(TCommandEnum).GUID, Array.ConvertAll(commandIds, e => Convert.ToUInt32(e, CultureInfo.InvariantCulture)))
        { }

        protected CommandTargetBase(IVsTextView adapter, IWpfTextView textView, Guid commandGroup, params uint[] commandIds)
        {
            CommandGroup = commandGroup;
            CommandIds = new ReadOnlyCollection<uint>(commandIds);
            TextView = textView;

            Dispatcher.CurrentDispatcher.InvokeAsync(() =>
            {
                // Add the target later to make sure it makes it in before other command handlers
                ErrorHandler.ThrowOnFailure(adapter.AddCommandFilter(this, out _nextCommandTarget));
            }, DispatcherPriority.ApplicationIdle);
        }

        protected abstract bool IsEnabled();
        [SuppressMessage("ReSharper", "UnusedParameter.Global")]
        protected abstract bool Execute(TCommandEnum commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut);

        public int Exec(ref Guid pguidCmdGroup, uint nCmdID, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (pguidCmdGroup == CommandGroup && CommandIds.Contains(nCmdID))
            {
                bool result = Execute((TCommandEnum)(object)(int)nCmdID, nCmdexecopt, pvaIn, pvaOut);

                if (result)
                {
                    return VSConstants.S_OK;
                }
            }

            return _nextCommandTarget.Exec(ref pguidCmdGroup, nCmdID, nCmdexecopt, pvaIn, pvaOut);
        }

        [SuppressMessage("ReSharper", "BitwiseOperatorOnEnumWithoutFlags")]
        public int QueryStatus(ref Guid pguidCmdGroup, uint cCmds, OLECMD[] prgCmds, IntPtr pCmdText)
        {
            if (pguidCmdGroup != CommandGroup)
                return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);

            for (int i = 0; i < cCmds; i++)
            {
                if (CommandIds.Contains(prgCmds[i].cmdID))
                {
                    if (IsEnabled())
                    {
                        prgCmds[i].cmdf = (uint)(OLECMDF.OLECMDF_ENABLED | OLECMDF.OLECMDF_SUPPORTED);
                        return VSConstants.S_OK;
                    }

                    prgCmds[0].cmdf = (uint)OLECMDF.OLECMDF_SUPPORTED;
                }
            }

            return _nextCommandTarget.QueryStatus(ref pguidCmdGroup, cCmds, prgCmds, pCmdText);
        }
    }

    internal class ZenCoding : CommandTargetBase<VSConstants.VSStd2KCmdID>
    {
        private ICompletionBroker _broker;

        private static Regex _bracket = new Regex(@"<([a-z0-9]*)\b[^>]*>([^<]*)</\1>", RegexOptions.IgnoreCase);
        private static Regex _quotes = new Regex("(=\"()\")", RegexOptions.IgnoreCase);

        public ZenCoding(IVsTextView adapter, IWpfTextView textView, ICompletionBroker broker)
            : base(adapter, textView, VSConstants.VSStd2KCmdID.TAB, VSConstants.VSStd2KCmdID.BACKTAB)
        {
            _broker = broker;
        }

        protected override bool Execute(VSConstants.VSStd2KCmdID commandId, uint nCmdexecopt, IntPtr pvaIn, IntPtr pvaOut)
        {
            if (commandId == VSConstants.VSStd2KCmdID.TAB && !_broker.IsCompletionActive(TextView))
            {
                if (InvokeZenCoding())
                {
                    return true;
                }
            }

            return false;
        }

        private bool InvokeZenCoding()
        {
            Span zenSpan = GetText();

            if (zenSpan.Length == 0 || TextView.Selection.SelectedSpans[0].Length > 0 || !IsValidTextBuffer())
                return false;

            string zenSyntax = TextView.TextBuffer.CurrentSnapshot.GetText(zenSpan);

            string result = XamlParser.Parse(zenSyntax);

            if (!string.IsNullOrEmpty(result))
            {
                Dispatcher.CurrentDispatcher.BeginInvoke(new Action(() =>
                {
                    ITextSelection selection = UpdateTextBuffer(zenSpan, result);

                    Span newSpan = new Span(zenSpan.Start, selection.SelectedSpans[0].Length);

                    vsXenPackage.ExecuteCommand("Edit.FormatSelection");
                    SetCaret(newSpan, false);

                    selection.Clear();
                }), DispatcherPriority.ApplicationIdle, null);

                return true;
            }

            return false;
        }

        private bool IsValidTextBuffer()
        {
            var projection = TextView.TextBuffer as IProjectionBuffer;

            if (projection != null)
            {
                var snapshotPoint = TextView.Caret.Position.BufferPosition;

                var buffers = projection.SourceBuffers; //.Where(s => s.ContentType.IsOfType("xaml"));

                return buffers.Select(buffer => TextView.BufferGraph.MapDownToBuffer(snapshotPoint, PointTrackingMode.Negative, buffer, PositionAffinity.Predecessor)).All(point => !point.HasValue);
            }

            return true;
        }

        private void SetCaret(Span zenSpan, bool isReverse)
        {
            string text = TextView.TextBuffer.CurrentSnapshot.GetText();
            Span quote = FindTabSpan(zenSpan, isReverse, text, _quotes);
            Span bracket = FindTabSpan(zenSpan, isReverse, text, _bracket);

            if (bracket.Start > 0 && (quote.Start == 0 ||
                                      (!isReverse && (bracket.Start < quote.Start)) ||
                                      (isReverse && (bracket.Start > quote.Start))))
            {
                quote = bracket;
            }

            if (zenSpan.Contains(quote.Start))
            {
                MoveTab(quote);
                return;
            }

            if (!isReverse)
            {
                MoveTab(new Span(zenSpan.End, 0));
            }
        }

        private void MoveTab(Span quote)
        {
            TextView.Caret.MoveTo(new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, quote.Start));
        }

        private static Span FindTabSpan(Span zenSpan, bool isReverse, string text, Regex regex)
        {
            MatchCollection matches = regex.Matches(text);

            if (!isReverse)
            {
                foreach (Group @group in matches.Cast<Match>().Select(match => match.Groups[2]).Where(@group => @group.Index >= zenSpan.Start))
                {
                    return new Span(@group.Index, @group.Length);
                }
            }
            else
            {
                for (int i = matches.Count - 1; i >= 0; i--)
                {
                    Group group = matches[i].Groups[2];

                    if (group.Index < zenSpan.End)
                    {
                        return new Span(group.Index, group.Length);
                    }
                }
            }

            return new Span();
        }

        private ITextSelection UpdateTextBuffer(Span zenSpan, string result)
        {
            TextView.TextBuffer.Replace(zenSpan, result);

            SnapshotPoint point = new SnapshotPoint(TextView.TextBuffer.CurrentSnapshot, zenSpan.Start);
            SnapshotSpan snapshot = new SnapshotSpan(point, result.Length);
            TextView.Selection.Select(snapshot, false);

            //EditorExtensionsPackage.ExecuteCommand("Edit.FormatSelection");

            return TextView.Selection;
        }

        private Span GetText()
        {
            int position = TextView.Caret.Position.BufferPosition.Position;

            if (position >= 0)
            {
                var line = TextView.TextBuffer.CurrentSnapshot.GetLineFromPosition(position);
                string text = line.GetText().TrimEnd();

                if (string.IsNullOrWhiteSpace(text) || text.Length < position - line.Start || text.Length + line.Start > position)
                    return new Span();

                string result = text.Substring(0, position - line.Start).TrimStart();

                if (result.Length > 0 && !text.Contains("<") && !char.IsWhiteSpace(result.Last()))
                {
                    return new Span(line.Start.Position + text.IndexOf(result, StringComparison.OrdinalIgnoreCase), result.Length);
                }
            }

            return new Span();
        }

        protected override bool IsEnabled()
        {
            return true;
        }
    }

    [Export(typeof(IVsTextViewCreationListener))]
    [ContentType("XAML")]
    [TextViewRole(PredefinedTextViewRoles.Document)]
    public class HtmlViewCreationListener : IVsTextViewCreationListener
    {
        [Import]
        public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService
        { get; set; }

        [Import]
        public ICompletionBroker CompletionBroker
        { get; set; }

        public void VsTextViewCreated(IVsTextView textViewAdapter)
        {
            var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

            textView.Properties.GetOrCreateSingletonProperty(() => new ZenCoding(textViewAdapter, textView, CompletionBroker));

            //textView.MouseHover += textView_MouseHover;
            textView.Closed += textView_Closed;
        }

        //void textView_MouseHover(object sender, MouseHoverEventArgs e)
        //{
        //    if (InspectMode.IsInspectModeEnabled)
        //    {
        //        var doc = WebEssentialsPackage.DTE.ActiveDocument;
        //        if (doc != null)
        //        {
        //            InspectMode.Select(e.View.TextDataModel.DocumentBuffer.GetFileName(), e.Position);
        //        }
        //    }
        //}

        private void textView_Closed(object sender, EventArgs e)
        {
            IWpfTextView view = (IWpfTextView)sender;

            //view.MouseHover -= textView_MouseHover;
            view.Closed -= textView_Closed;
        }
    }

    //[Export(typeof(IVsTextViewCreationListener))]
    //[ContentType("XAML")]
    //[TextViewRole(PredefinedTextViewRoles.Document)]
    //public class HtmlxViewCreationListener : IVsTextViewCreationListener
    //{
    //    [Import]
    //    public IVsEditorAdaptersFactoryService EditorAdaptersFactoryService
    //    { get; set; }

    // [Import] public ICompletionBroker CompletionBroker { get; set; }

    // public void VsTextViewCreated(IVsTextView textViewAdapter) { var textView = EditorAdaptersFactoryService.GetWpfTextView(textViewAdapter);

    // var formatter = ComponentLocatorForContentType<IEditorFormatterProvider, IComponentContentTypes>.ImportOne(HtmlContentTypeDefinition.HtmlContentType).Value;

    //        textView.Properties.GetOrCreateSingletonProperty<SurroundWith>(() => new SurroundWith(textViewAdapter, textView));
    //        textView.Properties.GetOrCreateSingletonProperty<ExpandSelection>(() => new ExpandSelection(textViewAdapter, textView));
    //        textView.Properties.GetOrCreateSingletonProperty<ContractSelection>(() => new ContractSelection(textViewAdapter, textView));
    //        textView.Properties.GetOrCreateSingletonProperty<EnterFormat>(() => new EnterFormat(textViewAdapter, textView, formatter, CompletionBroker));
    //        textView.Properties.GetOrCreateSingletonProperty<MinifySelection>(() => new MinifySelection(textViewAdapter, textView));
    //        textView.Properties.GetOrCreateSingletonProperty<HtmlGoToDefinition>(() => new HtmlGoToDefinition(textViewAdapter, textView));
    //        textView.Properties.GetOrCreateSingletonProperty<HtmlFindAllReferences>(() => new HtmlFindAllReferences(textViewAdapter, textView));
    //    }
    //}
}