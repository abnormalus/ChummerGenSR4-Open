using System;
using System.IO;
using System.Net;
using System.Linq;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using System.Threading;
using System.Threading.Tasks;

namespace Chummer
{
    // One reader serves one chosen book plus bundled assets, only on loopback. The
    // unguessable route and strict asset root prevent access to arbitrary local files.
    internal sealed class PdfLocalServer : IDisposable
    {
        private readonly WebApplication server;
        private readonly CancellationTokenSource cancellation = new();
        private readonly string bookPath;
        private readonly string assetRoot = Path.GetFullPath(Path.Combine(AppContext.BaseDirectory, "pdfjs"));
        private readonly string token = Guid.NewGuid().ToString("N");
        internal Uri BaseUri { get; }
        internal Uri ViewerUri(int page) => new(BaseUri, "reader.html#page=" + Math.Max(1, page));
        internal PdfLocalServer(string path)
        {
            bookPath = Path.GetFullPath(path);
            if (!File.Exists(bookPath)) throw new FileNotFoundException();
            if (!File.Exists(Path.Combine(assetRoot, "reader.html"))) throw new FileNotFoundException();
            // Kestrel binds an ephemeral loopback port directly on both desktop platforms,
            // without Windows HTTP.sys URL reservations or a hand-written HTTP parser.
            var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions {
                Args = Array.Empty<string>(), ContentRootPath = AppContext.BaseDirectory, EnvironmentName = "Production" });
            builder.Logging.ClearProviders();
            builder.WebHost.UseKestrel(options => {
                options.Listen(IPAddress.Loopback, 0);
                options.Limits.MaxRequestBodySize = 0;
                options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
            });
            server = builder.Build();
            server.Run(ServeAsync);
            try
            {
                Task.Run(() => server.StartAsync()).GetAwaiter().GetResult();
                string origin = server.Services.GetRequiredService<IServer>().Features.Get<IServerAddressesFeature>().Addresses.Single();
                BaseUri = new Uri(origin.TrimEnd('/') + "/" + token + "/");
            }
            catch { server.DisposeAsync().AsTask().GetAwaiter().GetResult(); throw; }
        }
        private async Task ServeAsync(HttpContext context)
        {
            try
            {
                var request = context.Request;
                var response = context.Response;
                string route = request.Path.Value;
                string prefix = "/" + token + "/";
                if ((request.Method != "GET" && request.Method != "HEAD") || !route.StartsWith(prefix, StringComparison.Ordinal))
                { response.StatusCode = 404; return; }
                string relative = Uri.UnescapeDataString(route.Substring(prefix.Length));
                bool book = relative == "document.pdf";
                string path = book ? bookPath : Path.GetFullPath(Path.Combine(assetRoot, relative));
                if ((!book && !path.StartsWith(assetRoot + Path.DirectorySeparatorChar, StringComparison.Ordinal)) || !File.Exists(path))
                { response.StatusCode = 404; return; }
                response.Headers["Cache-Control"] = "no-store";
                response.Headers["X-Content-Type-Options"] = "nosniff";
                response.Headers["Content-Security-Policy"] = "default-src 'self'; script-src 'self' 'wasm-unsafe-eval'; worker-src 'self' blob:; style-src 'self' 'unsafe-inline'; img-src 'self' data: blob:; font-src 'self' data: blob:; connect-src 'self'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'";
                response.ContentType = Path.GetExtension(path).ToLowerInvariant() switch
                {
                    ".html" => "text/html; charset=utf-8", ".mjs" or ".js" => "text/javascript; charset=utf-8",
                    ".css" => "text/css", ".wasm" => "application/wasm", ".pdf" => "application/pdf",
                    ".svg" => "image/svg+xml", ".png" => "image/png", _ => "application/octet-stream"
                };
                using var file = new FileStream(path, FileMode.Open, FileAccess.Read, FileShare.Read, 65536, true);
                long start = 0, end = file.Length - 1;
                string requestedRange = request.Headers["Range"].ToString();
                if (book && !string.IsNullOrEmpty(requestedRange))
                {
                    if (!TryRange(requestedRange, file.Length, out start, out end))
                    { response.StatusCode = 416; response.Headers["Content-Range"] = "bytes */" + file.Length; return; }
                    response.StatusCode = 206;
                    response.Headers["Content-Range"] = $"bytes {start}-{end}/{file.Length}";
                }
                if (book) response.Headers["Accept-Ranges"] = "bytes";
                response.ContentLength = end - start + 1;
                if (request.Method == "HEAD") return;
                file.Position = start;
                long remaining = response.ContentLength.Value;
                var buffer = new byte[65536];
                while (remaining > 0)
                {
                    int count = await file.ReadAsync(buffer.AsMemory(0, (int)Math.Min(buffer.Length, remaining)), cancellation.Token).ConfigureAwait(false);
                    if (count == 0) break;
                    await response.Body.WriteAsync(buffer.AsMemory(0, count), cancellation.Token).ConfigureAwait(false);
                    remaining -= count;
                }
            }
            catch (Exception error) when (error is IOException || error is HttpListenerException || error is OperationCanceledException || error is ObjectDisposedException)
            { if (!cancellation.IsCancellationRequested) AppDiagnostics.Record("pdf-response-failed", error); }
            catch (Exception error) { AppDiagnostics.Record("pdf-response-failed", error); context.Response.StatusCode = 500; }

        }
        internal static bool TryRange(string value, long length, out long start, out long end)
        {
            start = 0; end = length - 1;
            if (!value.StartsWith("bytes=", StringComparison.Ordinal) || length < 1) return false;
            var parts = value.Substring(6).Split('-');
            if (parts.Length != 2) return false;
            if (parts[0] == "")
            {
                if (!long.TryParse(parts[1], out var suffix) || suffix < 1) return false;
                start = Math.Max(0, length - suffix);
            }
            else
            {
                if (!long.TryParse(parts[0], out start) || start < 0 || start >= length) return false;
                if (parts[1] != "" && (!long.TryParse(parts[1], out end) || end < start)) return false;
                end = Math.Min(end, length - 1);
            }
            return true;
        }
        public void Dispose()
        {
            cancellation.Cancel();
            Task.Run(async () =>
            {
                try { using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(2)); await server.StopAsync(timeout.Token); }
                finally { await server.DisposeAsync(); }
            }).GetAwaiter().GetResult();
        }
    }
}
