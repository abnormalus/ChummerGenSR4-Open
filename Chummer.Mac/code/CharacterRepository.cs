#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Globalization;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Net.Http.Json;
using System.Text.Json;
using System.Threading.Tasks;

namespace Chummer;

internal sealed record RepositoryRevision(long RepositoryId, string Branch, string Path, string Sha,
    DateTimeOffset LastSynced);
internal sealed class RepositoryTokenExpiredException() :
    IOException("The GitHub token has expired or was revoked. Create a new token to reconnect.");

internal sealed record RepositoryFile(string Name, string Path, string Sha);

internal sealed class CharacterRepository
{
    private static readonly HttpClient Http = new(new HttpClientHandler { AllowAutoRedirect = false })
    {
        BaseAddress = new Uri("https://api.github.com/"),
        Timeout = TimeSpan.FromSeconds(30)
    };

    private readonly HttpClient client;
    private readonly string token;
    private readonly string route;
    internal string Name { get; }
    internal string Folder { get; }
    internal DateTimeOffset? ExpiresAt { get; private set; }
    internal RepositoryConnection Connection => new(Name, Folder, token, ExpiresAt, Id, Branch);
    internal long Id { get; private set; }
    internal string Branch { get; private set; } = "";

    internal CharacterRepository(string name, string folder, string token, HttpClient? client = null)
    {
        string[] repository = name.Trim().Split('/');
        if (repository.Length != 2 || repository.Any(part => !ValidSegment(part) ||
            part.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '.' && c != '-' && c != '_')))
            throw new IOException("Enter the GitHub repository as owner/name.");
        if (folder.Split('/').Any(part => !ValidSegment(part)))
            throw new IOException("Enter a repository-relative folder such as characters.");
        if (!token.StartsWith("github_pat_", StringComparison.Ordinal) || token.Any(c => !char.IsAsciiLetterOrDigit(c) && c != '_'))
            throw new IOException("Enter a fine-grained GitHub personal access token.");
        Name = string.Join("/", repository);
        Folder = folder;
        route = "repos/" + EncodePath(Name);
        this.token = token;
        this.client = client ?? Http;
    }

    internal CharacterRepository(RepositoryConnection connection)
        : this(connection.Name, connection.Folder, connection.Token ?? "")
    {
        Id = connection.Id;
        Branch = connection.Branch;
        ExpiresAt = connection.ExpiresAt;
    }

    internal static bool ValidSegment(string value) => !string.IsNullOrWhiteSpace(value) &&
        value is not "." and not ".." && !value.Any(c => char.IsControl(c) || c == '/' || c == '\\');

    private static string EncodePath(string path) =>
        string.Join("/", path.Split('/').Select(Uri.EscapeDataString));

    private async Task<JsonDocument?> RequestAsync(HttpMethod method, string path, object? body = null,
        bool allowMissing = false)
    {
        using var request = new HttpRequestMessage(method, path);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);
        request.Headers.UserAgent.ParseAdd("ChummerGenSR4");
        request.Headers.Accept.ParseAdd("application/vnd.github+json");
        request.Headers.Add("X-GitHub-Api-Version", "2022-11-28");
        if (body != null) request.Content = JsonContent.Create(body);

        using var response = await client.SendAsync(request);
        if (response.StatusCode == HttpStatusCode.Unauthorized)
            throw new RepositoryTokenExpiredException();
        if (response.Headers.TryGetValues("GitHub-Authentication-Token-Expiration", out var expiration) &&
            DateTimeOffset.TryParse(expiration.FirstOrDefault()?.Replace(" UTC", " +00:00", StringComparison.Ordinal),
                CultureInfo.InvariantCulture,
                DateTimeStyles.AssumeUniversal, out var expiresAt))
            ExpiresAt = expiresAt;
        if (allowMissing && response.StatusCode == HttpStatusCode.NotFound) return null;
        if (!response.IsSuccessStatusCode)
        {
            if (method == HttpMethod.Put && (int)response.StatusCode >= 500)
                throw new IOException("GitHub did not confirm the upload. Your edits are still open. Check the remote file before retrying.");
            throw new IOException(response.StatusCode switch
            {
                HttpStatusCode.Forbidden => "GitHub denied the request. Check token permissions, organization approval, and rate limits.",
                HttpStatusCode.NotFound => "GitHub could not find the repository or file, or the token cannot access it.",
                HttpStatusCode.Conflict or HttpStatusCode.UnprocessableEntity =>
                    "GitHub rejected the save. The remote file may have changed, or the branch may restrict writes. Your edits are still open.",
                _ => $"GitHub returned HTTP {(int)response.StatusCode}."
            });
        }
        return await JsonDocument.ParseAsync(await response.Content.ReadAsStreamAsync());
    }

    internal async Task VerifyAsync()
    {
        using var document = await RequestAsync(HttpMethod.Get, route);
        var repository = document!.RootElement;
        if (!repository.GetProperty("private").GetBoolean() ||
            repository.GetProperty("visibility").GetString() != "private")
            throw new IOException("Select a private GitHub repository.");
        long id = repository.GetProperty("id").GetInt64();
        string branch = repository.GetProperty("default_branch").GetString()!;
        if (Id != 0 && (Id != id || Branch != branch))
            throw new IOException("The repository or its default branch changed. Reconnect before continuing.");
        Id = id;
        Branch = branch;
    }

    private string Contents(string path) => route + "/contents/" + EncodePath(path);
    private string OnBranch(string path) => Contents(path) + "?ref=" + Uri.EscapeDataString(Branch);

    internal async Task<IReadOnlyList<RepositoryFile>> ListAsync()
    {
        await VerifyAsync();
        using var document = await RequestAsync(HttpMethod.Get, OnBranch(Folder), allowMissing: true);
        if (document == null) return Array.Empty<RepositoryFile>();
        if (document.RootElement.ValueKind != JsonValueKind.Array)
            throw new IOException("The configured character folder is a file. Choose a folder in repository setup.");
        return document.RootElement.EnumerateArray()
            .Where(item => item.GetProperty("type").GetString() == "file")
            .Where(item => item.GetProperty("name").GetString()!.EndsWith(".chum", StringComparison.OrdinalIgnoreCase))
            .Select(item => new RepositoryFile(item.GetProperty("name").GetString()!,
                item.GetProperty("path").GetString()!, item.GetProperty("sha").GetString()!))
            .OrderBy(file => file.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    internal async Task<byte[]> DownloadAsync(RepositoryFile file)
    {
        await VerifyAsync();
        if (file.Sha.Length != 40 || !file.Sha.All(char.IsAsciiHexDigit))
            throw new IOException("GitHub returned an invalid character revision.");
        using var document = await RequestAsync(HttpMethod.Get, route + "/git/blobs/" + file.Sha);
        var item = document!.RootElement;
        if (item.GetProperty("sha").GetString() != file.Sha)
            throw new IOException("GitHub returned a different character revision.");
        if (item.GetProperty("encoding").GetString() != "base64")
            throw new IOException("GitHub could not return this character as a downloadable snapshot.");
        return Convert.FromBase64String(item.GetProperty("content").GetString()!);
    }

    internal async Task<RepositoryRevision> SaveAsync(string path, RepositoryRevision? expected, byte[] bytes)
    {
        await VerifyAsync();
        if (expected != null && (expected.RepositoryId != Id || expected.Branch != Branch || expected.Path != path))
            throw new IOException("This working copy is linked to a different repository, branch, or file.");
        using var existing = await RequestAsync(HttpMethod.Get, OnBranch(path), allowMissing: true);
        if (existing != null && (existing.RootElement.ValueKind != JsonValueKind.Object ||
            existing.RootElement.GetProperty("type").GetString() != "file"))
            throw new IOException("A repository entry already uses that path. Choose another filename.");
        string? current = existing?.RootElement.GetProperty("sha").GetString();
        if (current != expected?.Sha)
            throw new IOException(expected == null
                ? "A remote file already uses that name. Choose another filename."
                : "The remote file changed or was removed. Your edits are still open. Open the remote version separately to review it.");
        var payload = new Dictionary<string, string>
        {
            ["message"] = "Save Chummer character",
            ["branch"] = Branch,
            ["content"] = Convert.ToBase64String(bytes)
        };
        if (expected != null) payload["sha"] = expected.Sha;

        try
        {
            using var written = await RequestAsync(HttpMethod.Put, Contents(path), payload);
            string sha = written!.RootElement.GetProperty("content").GetProperty("sha").GetString()!;
            return new RepositoryRevision(Id, Branch, path, sha, DateTimeOffset.UtcNow);
        }
        catch (Exception error) when (error is HttpRequestException or OperationCanceledException or JsonException)
        {
            throw new IOException("GitHub did not confirm the upload. Your edits are still open. Check the remote file before retrying.", error);
        }
    }
}
