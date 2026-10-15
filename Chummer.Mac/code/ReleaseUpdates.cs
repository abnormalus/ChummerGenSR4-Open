#nullable enable
using System;
using System.IO;
using System.Net.Http;
using System.Text.Json;
using System.Text.RegularExpressions;
using System.Threading;
using System.Threading.Tasks;

namespace Chummer
{
    internal sealed record ReleaseInfo(string Notes, string Revision, bool Newer);

    internal static class ReleaseUpdates
    {
        internal const string ReleasePage = "https://github.com/abnormalus/ChummerGenSR4-Open/releases/tag/Nightly";
        private const string Api = "https://api.github.com/repos/abnormalus/ChummerGenSR4-Open/";
        private static readonly HttpClient Client = CreateClient();
        private static HttpClient CreateClient()
        {
            var client = new HttpClient { Timeout = TimeSpan.FromSeconds(15) };
            client.DefaultRequestHeaders.UserAgent.ParseAdd("ChummerGenSR4-UpdateCheck/1.0");
            return client;
        }
        internal static string LocalRevision()
        {
            try
            {
                string revision = File.ReadAllText(Path.Combine(AppContext.BaseDirectory, "source-revision.txt")).Trim();
                return Regex.IsMatch(revision, "^[0-9a-f]{40}$") ? revision : "";
            }
            catch (IOException) { return ""; }
            catch (UnauthorizedAccessException) { return ""; }
        }
        internal static (string Notes, string Revision) Parse(string json)
        {
            using var document = JsonDocument.Parse(json);
            var root = document.RootElement;
            if (root.GetProperty("tag_name").GetString() != "Nightly" || root.GetProperty("draft").GetBoolean())
                throw new InvalidDataException("Expected a published Nightly release.");
            string notes = root.GetProperty("body").GetString() ?? "";
            var match = Regex.Match(notes, @"Development builds from commit \[([0-9a-f]{40})\]");
            if (!match.Success) throw new InvalidDataException("The release does not identify its source revision.");
            return (notes, match.Groups[1].Value);
        }
        internal static async Task<ReleaseInfo> CheckAsync(CancellationToken cancellation, HttpClient? client = null, string? localRevision = null)
        {
            client ??= Client;
            var release = Parse(await client.GetStringAsync(Api + "releases/tags/Nightly", cancellation));
            string current = localRevision ?? LocalRevision();
            bool newer = false;
            // A local development build can be ahead of Nightly. Never offer that as an upgrade.
            if (Regex.IsMatch(current, "^[0-9a-f]{40}$") && current != release.Revision)
            {
                using var comparison = JsonDocument.Parse(await client.GetStringAsync(Api + "compare/" + current + "..." + release.Revision, cancellation));
                newer = comparison.RootElement.GetProperty("status").GetString() == "ahead";
            }
            return new ReleaseInfo(release.Notes, release.Revision, newer);
        }
    }
}
