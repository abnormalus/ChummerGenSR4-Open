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
            bool coreBook = string.Equals(code, "SR4", StringComparison.OrdinalIgnoreCase);
            // Edition prefixes commonly occur on every PDF in a collection. A core
            // alias must never turn an explicitly named supplement into the rulebook.
            var supplements = coreBook ? XmlManager.Instance.Load("books.xml").SelectNodes("/chummer/books/book")
                .Cast<System.Xml.XmlNode>().Where(book => book["code"]?.InnerText != "SR4")
                .Select(book => Normalize(book["name"]?.InnerText ?? "")).Where(name => name.Length > 0).ToArray()
                : Array.Empty<string>();
            var scored = files.Select(path => {
                string stem = Normalize(Path.GetFileNameWithoutExtension(path));
                int score = names.Any(n => stem == n) ? 3 : names.Any(n =>
                    Regex.IsMatch(" " + stem + " ", @"\b" + string.Join(@"\b.*\b", n.Split(' ').Select(Regex.Escape)) + @"\b")) ? 2 :
                    (" " + stem + " ").Contains(" " + Normalize(code) + " ") ? 1 : 0;
                if (coreBook)
                {
                    // Tokenize familiar core-title words even in compact filenames.
                    string tokens = string.Join(" ", Regex.Matches(stem,
                        @"sr[\s]*4[\s]*a(?=\b|core|rulebook)|shadowrun|anniversary|twentieth|fourth|edition|rulebook|20th|4th|4e|4a|[\p{L}\p{N}]+")
                        .Cast<Match>().Select(match => match.Value));
                    bool supplement = supplements.Any(title => Regex.IsMatch(" " + tokens + " ", @"\b" +
                        string.Join(@"\s+", title.Split(' ').Select(Regex.Escape)) + @"\b"));
                    bool abbreviation = Regex.IsMatch(tokens, @"\bsr\s*4\s*a\b|\bshadowrun\s+4a\b");
                    bool anniversary = Regex.IsMatch(tokens, @"\b(?:anniversary|20th|twentieth)\b");
                    bool fourthEdition = Regex.IsMatch(tokens, @"\b(?:4e|4a|4th|fourth|4|sr4)\b");
                    bool shadowrun = Regex.IsMatch(tokens, @"\bshadowrun\b");
                    bool otherEdition = Regex.IsMatch(tokens, @"\b(?:5e|5th|fifth|6e|6th|sixth)\b");
                    bool relatedDocument = Regex.IsMatch(tokens, @"\b(?:character\s*sheet|cheat\s*sheet|quick\s*start|errata|preview|summary)\b");
                    if (supplement || otherEdition || relatedDocument) score = 0;
                    else if (abbreviation || Regex.IsMatch(tokens, @"\bsr4\b") && anniversary || shadowrun && anniversary && (fourthEdition || Regex.IsMatch(tokens, @"\b(?:20th|twentieth)\b"))) score = 4;
                    else if (shadowrun && fourthEdition && Regex.IsMatch(tokens, @"\b(?:core|rulebook|rule book)\b")) score = Math.Max(score, 2);
                }
                return (path, score);
            }).Where(x => x.score > 0).ToArray();
            int best = scored.Length == 0 ? 0 : scored.Max(x => x.score);
            return scored.Where(x => x.score == best).Select(x => x.path).OrderBy(p => p, StringComparer.OrdinalIgnoreCase).ToArray();
        }
    }
}
