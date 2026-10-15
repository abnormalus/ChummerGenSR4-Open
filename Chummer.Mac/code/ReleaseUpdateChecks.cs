#nullable enable
using System;
using System.Net;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Chummer
{
    internal static class ReleaseUpdateChecks
    {
        private sealed class Responses : HttpMessageHandler
        {
            internal int Requests;
            internal string Comparison = "ahead";
            internal string Json = "";
            protected override Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellation)
            {
                cancellation.ThrowIfCancellationRequested();
                Requests++;
                if (request.RequestUri?.Host != "api.github.com" || !request.RequestUri.AbsolutePath.StartsWith("/repos/abnormalus/ChummerGenSR4-Open/", StringComparison.Ordinal))
                    throw new InvalidOperationException("Updater contacted an unexpected destination.");
                string response = request.RequestUri.AbsolutePath.Contains("/compare/", StringComparison.Ordinal)
                    ? "{\"status\":\"" + Comparison + "\"}" : Json;
                return Task.FromResult(new HttpResponseMessage(HttpStatusCode.OK) { Content = new StringContent(response) });
            }
        }
        internal static void Run()
        {
            const string current = "1111111111111111111111111111111111111111";
            const string remote = "2222222222222222222222222222222222222222";
            using var responses = new Responses { Json = "{\"tag_name\":\"Nightly\",\"draft\":false,\"body\":\"Development builds from commit [" + remote + "]\"}" };
            using var client = new HttpClient(responses);
            void Check(bool valid, string label) { if (!valid) throw new InvalidOperationException(label); Console.WriteLine("PASS " + label); }
            var release = ReleaseUpdates.CheckAsync(CancellationToken.None, client, current).GetAwaiter().GetResult();
            Check(release.Newer && release.Revision == remote && responses.Requests == 2, "update check identifies a newer Nightly using source ancestry");
            foreach (string state in new[] { "behind", "diverged", "identical" })
            {
                responses.Comparison = state;
                Check(!ReleaseUpdates.CheckAsync(CancellationToken.None, client, current).GetAwaiter().GetResult().Newer,
                    "update check does not offer a downgrade for " + state + " source history");
            }
            int before = responses.Requests;
            Check(!ReleaseUpdates.CheckAsync(CancellationToken.None, client, "").GetAwaiter().GetResult().Newer && responses.Requests == before + 1,
                "unidentified development builds do not get misleading update claims");
            before = responses.Requests;
            Check(!ReleaseUpdates.CheckAsync(CancellationToken.None, client, remote).GetAwaiter().GetResult().Newer && responses.Requests == before + 1,
                "installed Nightly is recognized without a comparison request");
            using var cancelled = new CancellationTokenSource(); cancelled.Cancel();
            bool cancellationObserved = false;
            try { ReleaseUpdates.CheckAsync(cancelled.Token, client, current).GetAwaiter().GetResult(); }
            catch (OperationCanceledException) { cancellationObserved = true; }
            Check(cancellationObserved, "update requests respect cancellation");
            responses.Json = "{\"tag_name\":\"Nightly\",\"draft\":false,\"body\":\"missing source\"}";
            bool rejected = false;
            try { ReleaseUpdates.CheckAsync(CancellationToken.None, client, current).GetAwaiter().GetResult(); }
            catch (System.IO.InvalidDataException) { rejected = true; }
            Check(rejected, "malformed release metadata cannot be reported as up to date");
            var window = new frmUpdate(); window.Dispose(); window.Dispose();
            Check(true, "closing and disposing the updater repeatedly releases requests safely");
            // Captured from the pre-modernization implementation with the same explicit seed.
            uint[] expected = { 2505241820, 4267588480, 2524958330, 3682800685, 3542011659,
                1205025162, 2780548418, 1640330559, 1928256307, 3761466531 };
            var seeded = new MTRNG(12345);
            foreach (uint value in expected) if (seeded.NextUInt() != value) throw new InvalidOperationException("Seeded dice sequence changed.");
            Check(true, "seeded dice match the historical sequence after random-provider modernization");
        }
    }
}
