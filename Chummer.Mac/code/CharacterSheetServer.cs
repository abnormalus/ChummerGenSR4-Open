#nullable enable
using System;
using System.Linq;
using System.Net;
using System.Text;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Hosting.Server;
using Microsoft.AspNetCore.Hosting.Server.Features;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;

namespace Chummer;

// Serve one in-memory sheet on loopback. No character files or directories are exposed.
// HTTP navigation avoids WebView2 NavigateToString's 2 MB limit and shares the PDF reader's transport.
internal sealed class CharacterSheetServer : IAsyncDisposable
{
    private readonly WebApplication _server;
    private readonly string _token = Guid.NewGuid().ToString("N");
    private Sheet? _sheet;
    private sealed record Sheet(string Route, byte[] Content);
    private int _disposed;
    internal Uri BaseUri { get; private set; } = null!;

    private CharacterSheetServer()
    {
        var builder = WebApplication.CreateSlimBuilder(new WebApplicationOptions {
            Args = Array.Empty<string>(), ContentRootPath = AppContext.BaseDirectory, EnvironmentName = "Production" });
        builder.Logging.ClearProviders();
        builder.WebHost.UseKestrel(options => {
            options.Listen(IPAddress.Loopback, 0);
            options.Limits.MaxRequestBodySize = 0;
            options.Limits.RequestHeadersTimeout = TimeSpan.FromSeconds(10);
        });
        _server = builder.Build();
        _server.Run(ServeAsync);
    }

    internal static async Task<CharacterSheetServer> CreateAsync()
    {
        var result = new CharacterSheetServer();
        try
        {
            await result._server.StartAsync().ConfigureAwait(false);
            string origin = result._server.Services.GetRequiredService<IServer>()
                .Features.Get<IServerAddressesFeature>()!.Addresses.Single();
            result.BaseUri = new Uri(origin.TrimEnd('/') + "/" + result._token + "/");
            return result;
        }
        catch { await result.DisposeAsync().ConfigureAwait(false); throw; }
    }

    internal Uri SetSheet(string html, int generation)
    {
        ObjectDisposedException.ThrowIf(Volatile.Read(ref _disposed) != 0, this);
        string route = BaseUri.AbsolutePath + "sheet-" + generation + ".html";
        Volatile.Write(ref _sheet, new Sheet(route, Encoding.UTF8.GetBytes(html)));
        return new Uri(BaseUri, "sheet-" + generation + ".html");
    }

    private async Task ServeAsync(HttpContext context)
    {
        var sheet = Volatile.Read(ref _sheet);
        var request = context.Request;
        var response = context.Response;
        if (sheet == null || (request.Method != "GET" && request.Method != "HEAD") ||
            !string.Equals(request.Path.Value, sheet.Route, StringComparison.Ordinal))
        { response.StatusCode = 404; return; }
        response.ContentType = "text/html; charset=utf-8";
        response.ContentLength = sheet.Content.Length;
        response.Headers.CacheControl = "no-store";
        response.Headers.XContentTypeOptions = "nosniff";
        response.Headers["Content-Security-Policy"] = "default-src 'none'; script-src 'unsafe-inline'; style-src 'unsafe-inline'; img-src data:; font-src data:; connect-src 'none'; object-src 'none'; base-uri 'none'; frame-ancestors 'none'; form-action 'none'";
        if (request.Method == "GET")
            await response.Body.WriteAsync(sheet.Content, context.RequestAborted).ConfigureAwait(false);
    }

    public async ValueTask DisposeAsync()
    {
        if (Interlocked.Exchange(ref _disposed, 1) != 0) return;
        Volatile.Write(ref _sheet, null);
        try
        {
            using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(5));
            await _server.StopAsync(timeout.Token).ConfigureAwait(false);
        }
        finally { await _server.DisposeAsync().ConfigureAwait(false); }
    }
}
