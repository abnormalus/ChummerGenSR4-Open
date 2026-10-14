using System;
using System.IO;
using System.Linq;
using Majorsilence.Forms;

namespace Chummer
{
    internal static class SourcebookReader
    {
        internal static bool TryResolve(string reference, Character character, out SourcebookInfo book, out int page)
        {
            book = null; page = 0;
            var parts = (reference ?? "").Split((char[])null, StringSplitOptions.RemoveEmptyEntries);
            if (parts.Length != 2 || !int.TryParse(parts[1], out page) || page < 1) return false;
            string code = character?.Options.BookFromAltCode(parts[0]) ?? parts[0];
            book = GlobalOptions.Instance.SourcebookInfo.FirstOrDefault(info => string.Equals(info.Code, code, StringComparison.OrdinalIgnoreCase));
            long adjusted = (long)page + (book?.Offset ?? 0);
            if (adjusted < 1 || adjusted > int.MaxValue) return false;
            page = (int)adjusted;
            return true;
        }
        internal static void Open(string reference, Character character)
        {
            var owner = Application.OpenForms.Cast<Form>().LastOrDefault(form => form.Visible);
            if (!TryResolve(reference, character, out var book, out int page))
            { AppErrors.Inform(owner, "Message_PdfInvalidReference"); return; }
            if (string.IsNullOrWhiteSpace(book?.Path))
            { AppErrors.Inform(owner, "Message_PdfNotLinked"); return; }
            OpenDocument(book, page, owner);
        }
        internal static void OpenDocument(SourcebookInfo book, int page, Form owner)
        {
            if (!File.Exists(book.Path))
            { AppErrors.Inform(owner, "Message_PdfMissing"); return; }
            PdfLocalServer server = null;
            Form reader = null;
            WebBrowser browser = null;
            try
            {
                browser = new WebBrowser { Dock = DockStyle.Fill };
                if (!browser.IsWebViewFunctional)
                { browser.Dispose(); AppErrors.Inform(owner, "Message_PdfUnavailable"); return; }
                server = new PdfLocalServer(book.Path);
                reader = new Form { Text = "Chummer — " + book.Code, Width = 1000, Height = 800,
                    StartPosition = FormStartPosition.CenterParent };
                reader.Controls.Add(browser);
                var session = server;
                var readerBrowser = browser;
                reader.FormClosed += (_, _) => { session.Dispose(); readerBrowser.Dispose(); AppDiagnostics.Record("pdf-reader-closed"); };
                browser.WebMessageReceived += (_, args) =>
                {
                    if (args.Body == "pdf-ready") AppDiagnostics.Record("pdf-document-ready");
                    else if (args.Body == "pdf-error") AppDiagnostics.Record("pdf-document-error");
                };
                reader.Show(owner);
                browser.Navigate(server.ViewerUri(page));
                AppDiagnostics.Record("pdf-reader-opened");
            }
            catch (Exception error)
            {
                server?.Dispose();
                reader?.Dispose();
                browser?.Dispose();
                AppDiagnostics.Record("pdf-open-failed", error);
                AppErrors.Inform(owner, "Message_PdfOpenFailed");
            }
        }
    }
}
