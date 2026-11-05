#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net.Sockets;
using System.Text;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Exercise the production reader/server/webview ownership with a synthetic book,
// independently of character-sheet PDF export. Never changes sourcebook settings.
internal static class NativeSourcebookReaderChecks
{
    private static async Task WaitForAsync(Func<Task<bool>> condition)
    {
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (!await condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Native sourcebook reader operation timed out.");
            await Task.Delay(50);
        }
    }
    private static async Task<bool> TrueAsync(Forms.WebBrowser browser, string expression)
    {
        var script = browser.ExecuteScriptAsync(expression);
        if (await Task.WhenAny(script, Task.Delay(5000)) != script)
            throw new TimeoutException("Native sourcebook reader script timed out.");
        return (await script)?.Trim().Trim('"') == "true";
    }
    internal static async Task RunAsync(Forms.Form owner, string output, Action<bool, string> check)
    {
        string path = Path.Combine(output, "native-reader-synthetic.pdf");
        WriteBook(path);
        var before = Forms.Application.OpenForms.ToArray();
        SourcebookReader.OpenDocument(new SourcebookInfo { Code = "Synthetic reader check", Path = path }, 1, owner);
        var reader = Forms.Application.OpenForms.Single(f => !before.Contains(f));
        try
        {
            var browser = reader.Controls.OfType<Forms.WebBrowser>().Single();
            check(reader.Visible && browser.IsWebViewFunctional, "Native sourcebook opens the actual owned functional reader");
            await WaitForAsync(() => TrueAsync(browser, "document.getElementById('pages')?.textContent === '/ 2' && document.getElementById('status')?.textContent === ''"));
            check(true, "Native sourcebook loads both synthetic pages without an error notice");
            await WaitForAsync(() => TrueAsync(browser, "(() => { const c = document.querySelector('.page canvas'); if (!c || !c.width || !c.height) return false; const d = c.getContext('2d').getImageData(0,0,c.width,c.height).data; for(let i=0;i<d.length;i+=4) if(d[i+3]>0 && d[i]<180 && d[i+1]<180 && d[i+2]<180) return document.querySelector('.textLayer')?.textContent.includes('Chummer reader verification'); return false; })()"));
            check(true, "Native sourcebook renders nonblank page pixels and fixed synthetic text");
            await browser.ExecuteScriptAsync("(() => { const s=document.getElementById('search'); s.value='verification'; s.dispatchEvent(new Event('input',{bubbles:true})); })()");
            await WaitForAsync(() => TrueAsync(browser, "document.getElementById('matches')?.textContent.endsWith('/ 2') === true && document.querySelectorAll('.highlight').length > 0"));
            check(true, "Native sourcebook search highlights the fixed term across two pages");
            await browser.ExecuteScriptAsync("document.getElementById('next').click()");
            await WaitForAsync(() => TrueAsync(browser, "document.getElementById('page')?.value === '2'"));
            check(true, "Native sourcebook next-page control selects the second page");
            await browser.ExecuteScriptAsync("(() => { const z=document.getElementById('zoom'); z.value='1.5'; z.dispatchEvent(new Event('change',{bubbles:true})); })()");
            await WaitForAsync(() => TrueAsync(browser, "document.querySelector('.page[data-page-number=\"2\"]')?.getAttribute('data-loaded') === 'true' && document.getElementById('zoom')?.value === '1.5'"));
            check(true, "Native sourcebook zoom control retains a rendered second page");
            int port = browser.Url!.Port;
            reader.Close();
            check(!Forms.Application.OpenForms.Contains(reader), "Native sourcebook close releases the owned reader form");
            await WaitForAsync(async () =>
            {
                using var client = new TcpClient();
                try { await client.ConnectAsync("127.0.0.1", port).WaitAsync(TimeSpan.FromSeconds(2)); return false; }
                catch (SocketException) { return true; }
            });
            check(true, "Native sourcebook close releases its loopback server port");
        }
        finally { if (Forms.Application.OpenForms.Contains(reader)) reader.Close(); }
    }
    private static void WriteBook(string path)
    {
        var objects = new[] {
            "<< /Type /Catalog /Pages 2 0 R >>", "<< /Type /Pages /Kids [3 0 R 5 0 R] /Count 2 >>",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 7 0 R >> >> /Contents 4 0 R >>", "",
            "<< /Type /Page /Parent 2 0 R /MediaBox [0 0 612 792] /Resources << /Font << /F1 7 0 R >> >> /Contents 6 0 R >>", "",
            "<< /Type /Font /Subtype /Type1 /BaseFont /Helvetica >>" };
        foreach (int index in new[] { 3, 5 })
        {
            string stream = "BT /F1 20 Tf 50 700 Td (Chummer reader verification - page " + (index == 3 ? "one" : "two") + ") Tj ET";
            objects[index] = "<< /Length " + stream.Length + " >>\nstream\n" + stream + "\nendstream";
        }
        var pdf = new StringBuilder("%PDF-1.4\n"); var offsets = new int[8];
        for (int i = 0; i < objects.Length; i++) { offsets[i + 1] = pdf.Length; pdf.Append($"{i + 1} 0 obj\n{objects[i]}\nendobj\n"); }
        int xref = pdf.Length; pdf.Append("xref\n0 8\n0000000000 65535 f \n");
        foreach (int offset in offsets.Skip(1)) pdf.Append(offset.ToString("D10", System.Globalization.CultureInfo.InvariantCulture) + " 00000 n \n");
        pdf.Append($"trailer\n<< /Size 8 /Root 1 0 R >>\nstartxref\n{xref}\n%%EOF\n");
        File.WriteAllText(path, pdf.ToString(), Encoding.ASCII);
    }
}
