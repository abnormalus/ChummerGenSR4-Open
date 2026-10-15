#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Chummer;

internal static class BatchCharacterOutput
{
    // Character loading/printing can invoke UI-dependent legacy rules. Keep each unit on
    // the UI context and yield between units rather than moving live models to a worker.
    internal static async Task<XmlDocument> BuildAsync(IReadOnlyList<string> files,
        CancellationToken cancellation, Action<int, int> progress,
        Func<string, Character>? load = null)
    {
        if (files.Count == 0) throw new ArgumentException("Select at least one character.", nameof(files));
        load ??= path => {
            var character = new Character { FileName = path };
            bool loaded = character.Load(false);
            cancellation.ThrowIfCancellationRequested();
            if (!loaded) throw new InvalidDataException("Character could not be loaded.");
            return character;
        };
        using var stream = new MemoryStream();
        using var writer = new XmlTextWriter(stream, Encoding.UTF8);
        writer.WriteStartDocument();
        writer.WriteStartElement("characters");
        progress(0, files.Count);
        for (int index = 0; index < files.Count; index++)
        {
            await Task.Delay(16, cancellation);
            cancellation.ThrowIfCancellationRequested();
            Character character = load(files[index]);
            cancellation.ThrowIfCancellationRequested();
            character.PrintToStream(stream, writer);
            progress(index + 1, files.Count);
        }
        // Cancellation discards the entire document, including a request from the last unit.
        await Task.Delay(16, cancellation);
        cancellation.ThrowIfCancellationRequested();
        writer.WriteEndElement();
        writer.WriteEndDocument();
        writer.Flush();
        stream.Position = 0;
        var document = new XmlDocument();
        document.Load(stream);
        return document;
    }
}
