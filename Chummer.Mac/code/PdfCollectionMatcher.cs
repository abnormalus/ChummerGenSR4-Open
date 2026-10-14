using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.RegularExpressions;

namespace Chummer
{
    internal static class PdfCollectionMatcher
    {
        internal static string[] Scan(string folder) => Directory.EnumerateFiles(folder, "*",
            new EnumerationOptions { RecurseSubdirectories = true, IgnoreInaccessible = true,
                AttributesToSkip = FileAttributes.ReparsePoint }).Where(p =>
                string.Equals(Path.GetExtension(p), ".pdf", StringComparison.OrdinalIgnoreCase)).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();

        // Treat punctuation and spacing as wildcards, but require complete words for short codes.
        internal static string[] Candidates(IEnumerable<string> files, string code, params string[] titles)
        {
            string Normalize(string value) => Regex.Replace(value.ToLowerInvariant(), @"[^\p{L}\p{N}]+", " ").Trim();
            var names = titles.Where(t => !string.IsNullOrWhiteSpace(t)).Select(Normalize).Distinct().ToArray();
            var scored = files.Select(path => {
                string stem = Normalize(Path.GetFileNameWithoutExtension(path));
                int score = names.Any(n => stem == n) ? 3 : names.Any(n =>
                    Regex.IsMatch(" " + stem + " ", @"\b" + string.Join(@"\b.*\b", n.Split(' ').Select(Regex.Escape)) + @"\b")) ? 2 :
                    (" " + stem + " ").Contains(" " + Normalize(code) + " ") ? 1 : 0;
                return (path, score);
            }).Where(x => x.score > 0).ToArray();
            int best = scored.Length == 0 ? 0 : scored.Max(x => x.score);
            return scored.Where(x => x.score == best).Select(x => x.path).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
