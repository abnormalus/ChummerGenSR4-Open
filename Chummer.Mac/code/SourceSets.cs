#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json;

namespace Chummer;

// Separate from PDF associations and character settings: a set contains book codes only.
internal sealed class SourceSets
{
    private readonly string path;
    internal SourceSets(string path) => this.path = path;
    internal static SourceSets UserLibrary => new(Path.Combine(MacPaths.UserDirectory, "source-sets.json"));

    internal Dictionary<string, string[]> Read()
    {
        var result = new Dictionary<string, string[]>(StringComparer.OrdinalIgnoreCase);
        if (!File.Exists(path)) return result;
        var data = JsonSerializer.Deserialize<Dictionary<string, string[]>>(File.ReadAllText(path))
            ?? throw new InvalidDataException("Invalid source-set library.");
        foreach (var entry in data)
        {
            if (string.IsNullOrWhiteSpace(entry.Key) || entry.Value == null)
                throw new InvalidDataException("Invalid source set.");
            result.Add(entry.Key, Normalize(entry.Value));
        }
        return result;
    }

    internal void Save(string name, IEnumerable<string> books)
    {
        name = name.Trim();
        if (name.Length == 0 || name.Length > 100 || name.Any(char.IsControl))
            throw new ArgumentException("A source-set name must contain 1–100 characters and no control characters.");
        var sets = Read(); // Never replace an unreadable library with an empty one.
        sets.Remove(name);
        sets.Add(name, Normalize(books));
        Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(path))!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(sets, new JsonSerializerOptions { WriteIndented = true }));
            File.Move(temporary, path, true);
        }
        finally { if (File.Exists(temporary)) File.Delete(temporary); }
    }

    internal static string[] Normalize(IEnumerable<string> books) => books
        .Where(b => !string.IsNullOrWhiteSpace(b)).Append("SR4")
        .Distinct(StringComparer.Ordinal).OrderBy(b => b, StringComparer.Ordinal).ToArray();
}
