#nullable enable
using System;
using System.IO;
using System.Text;
using System.Xml;

namespace Chummer;

// Owns the temporary Unicode XML wrapper; callers retain item selection, Save
// dispatch, clipboard type changes and publication. No character or global state
// is read here. Callback failures propagate; caller side effects are not rolled back.
internal static class ClipboardDocument
{
    internal static XmlDocument Create(Action<XmlTextWriter> saveItems)
    {
        using var stream = new MemoryStream();
        using var writer = new XmlTextWriter(stream, Encoding.Unicode)
        {
            Formatting = Formatting.Indented, Indentation = 1, IndentChar = '\t'
        };
        writer.WriteStartDocument();
        writer.WriteStartElement("character");
        saveItems(writer);
        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush(); stream.Flush(); stream.Position = 0;
        // Leave the stream open until the writer is disposed; retain the legacy
        // BOM-aware reader/LoadXml path rather than changing encoding semantics.
        using var reader = new StreamReader(stream, Encoding.UTF8, true, 1024, leaveOpen: true);
        var document = new XmlDocument(); document.LoadXml(reader.ReadToEnd());
        return document;
    }
}
