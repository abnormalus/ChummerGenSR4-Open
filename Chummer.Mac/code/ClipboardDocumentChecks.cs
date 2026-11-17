#nullable enable
using System;
using System.IO;
using System.Xml;

namespace Chummer;

internal static class ClipboardDocumentChecks
{
    internal static void Run(Action<bool, string> check)
    {
        XmlTextWriter? retained = null; Stream? stream = null;
        var document = ClipboardDocument.Create(writer =>
        {
            retained = writer; stream = writer.BaseStream;
            writer.WriteStartElement("gear"); writer.WriteElementString("name", "<&雪😀"); writer.WriteEndElement();
            writer.WriteStartElement("weapon"); writer.WriteAttributeString("id", "one"); writer.WriteEndElement();
        });
        const string expected = "<?xml version=\"1.0\" encoding=\"utf-16\"?><character><gear><name>&lt;&amp;雪😀</name></gear><weapon id=\"one\" /></character>";
        check(document.OuterXml == expected, "Clipboard wrapper retains fixed Unicode escaping root declaration and multiple items");
        check(retained!.WriteState == WriteState.Closed && !stream!.CanRead && !stream.CanWrite,
            "Clipboard wrapper releases its writer and stream after success");
        check(ClipboardDocument.Create(_ => { }).OuterXml == "<?xml version=\"1.0\" encoding=\"utf-16\"?><character />",
            "Clipboard wrapper retains the empty character document");
        var failure = new InvalidOperationException("Synthetic callback failure"); Exception? caught = null;
        try
        {
            ClipboardDocument.Create(writer => { retained = writer; stream = writer.BaseStream; writer.WriteStartElement("partial"); throw failure; });
        }
        catch (Exception error) { caught = error; }
        check(ReferenceEquals(caught, failure) && retained!.WriteState == WriteState.Closed && !stream!.CanRead,
            "Clipboard callback failure propagates unchanged and releases temporary resources");
        bool rejected = false;
        try { ClipboardDocument.Create(writer => { retained = writer; stream = writer.BaseStream; writer.WriteRaw("&undefined;"); }); }
        catch (XmlException) { rejected = true; }
        check(rejected && retained!.WriteState == WriteState.Closed && !stream!.CanRead,
            "Clipboard XML parse failure rejects incomplete publication and releases temporary resources");
    }
}
