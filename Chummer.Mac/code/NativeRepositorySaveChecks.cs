#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeRepositorySaveChecks
{
    private const BindingFlags Private = BindingFlags.Instance | BindingFlags.Static | BindingFlags.NonPublic;
    private const string Remote = "characters/fixture.chum";

    internal static async Task RunAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        var options = GlobalOptions.Instance;
        bool enabled = options.CharacterAutosaveEnabled;
        int interval = options.CharacterAutosaveMinutes;
        options.SetCharacterAutosave(false, 9);
        check(!options.CharacterAutosaveEnabled && options.CharacterAutosaveMinutes == 9,
            "Autosave can be paused and its interval adjusted");
        options.SetCharacterAutosave(enabled, interval);

        foreach (bool career in new[] { false, true })
            await CheckDestinationAsync(owner, fixture, career, check);
    }

    private static async Task CheckDestinationAsync(frmMain owner, Character fixture, bool career, Action<bool, string> record)
    {
        void check(bool valid, string name) => record(valid, name + " career=" + career);
        using var server = new RepositoryServer();
        using var client = new HttpClient(server) { BaseAddress = new Uri("https://api.github.com/") };
        var repository = new CharacterRepository("test/private", "characters", "github_pat_example", client);
        await repository.VerifyAsync();
        var connectionField = typeof(frmMain).GetField("characterRepository", Private)!;
        var savedField = typeof(frmMain).GetField("savedRepository", Private)!;
        var oldRepository = connectionField.GetValue(owner);
        var oldConnection = savedField.GetValue(owner);
        connectionField.SetValue(owner, repository);
        savedField.SetValue(owner, repository.Connection);

        string directory = Path.Combine(MacPaths.UserDirectory, "repository-saving");
        string original = Path.Combine(career
            ? Path.Combine(MacPaths.UserDirectory, "saves", "repository", "opened-test")
            : directory, "fixture.chum");
        string local = Path.Combine(directory, career ? "local-career.chum" : "local-creation.chum");
        var snapshot = new Character();
        snapshot.LoadSnapshot(fixture.CaptureSnapshot());
        snapshot.Created = career;
        byte[] originalBytes = snapshot.CaptureSnapshot();
        CharacterFileDialogs.WriteSnapshot(original, originalBytes);
        typeof(frmMain).GetMethod("WriteRepositoryRevision", Private)!.Invoke(null,
            new object[] { original, new RepositoryRevision(repository.Id, repository.Branch, Remote, server.Sha, DateTimeOffset.UtcNow) });

        owner.LoadCharacter(original, false);
        using var editor = owner.ActiveMdiChild ?? throw new InvalidOperationException("Editor did not open.");
        var character = (Character)editor.GetType().GetField("_objCharacter", Private)!.GetValue(editor)!;
        var dirty = editor.GetType().GetField("_blnIsDirty", Private)!;
        bool Save() => editor is frmCreate creation ? creation.SaveCharacter() : ((frmCareer)editor).SaveCharacter();
        bool SaveAs() => editor is frmCreate creation
            ? creation.SaveCharacterAs(chooseSavePath: () => local) : ((frmCareer)editor).SaveCharacterAs(() => local);
        Task Autosave() => editor is frmCreate creation ? creation.AutosaveAsync(owner) : ((frmCareer)editor).AutosaveAsync(owner);
        void Change(string alias)
        {
            character.Alias = alias;
            dirty.SetValue(editor, true);
        }

        try
        {
            Change("Repository manual save");
            check(Save() && server.Writes == 1 &&
                  Encoding.Unicode.GetString(server.Content).Contains("Repository manual save", StringComparison.Ordinal),
                "Normal Save uploads a linked character without a success dialog");
            check(frmMain.HasRepositoryLink(character.FileName) && (career
                    ? character.FileName == original && File.ReadAllBytes(original).SequenceEqual(server.Content)
                    : character.FileName != original && File.ReadAllBytes(original).SequenceEqual(originalBytes)),
                "Repository save tracks its private working copy and preserves an external original");
            Change("Repository autosave");
            await Autosave();
            check(server.Writes == 2 && !(bool)dirty.GetValue(editor)!, "Autosave updates the linked GitHub revision");

            server.Sha = new string('c', 40);
            Change("Unsynced edits after a conflict");
            var expectedAlert = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
            Dispatcher.UIThread.Post(async () =>
            {
                var deadline = DateTime.UtcNow.AddSeconds(10);
                while (DateTime.UtcNow < deadline)
                {
                    var dialog = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>()
                        .FirstOrDefault(form => form.Text == "Autosave Failed");
                    if (dialog != null)
                    {
                        expectedAlert.TrySetResult(string.Join(" ", dialog.Controls.OfType<Forms.Label>().Select(label => label.Text)));
                        dialog.Close();
                        return;
                    }
                    await Task.Delay(20);
                }
                expectedAlert.TrySetException(new TimeoutException("Autosave did not surface its conflict."));
            });
            await Autosave();
            check((await expectedAlert.Task).Contains("remote file changed", StringComparison.Ordinal) &&
                  (bool)dirty.GetValue(editor)! && server.Writes == 2,
                "An autosave conflict reports the problem and retains pending edits");

            check(SaveAs() &&
                  character.FileName == local && !frmMain.HasRepositoryLink(local) && server.Writes == 2,
                "Save As switches to a local copy without uploading");
            Change("Local autosave");
            await Autosave();
            check(server.Writes == 2 && Encoding.Unicode.GetString(File.ReadAllBytes(local)).Contains("Local autosave", StringComparison.Ordinal),
                "Autosave follows the local Save As destination");
        }
        finally
        {
            dirty.SetValue(editor, false);
            editor.Close();
            connectionField.SetValue(owner, oldRepository);
            savedField.SetValue(owner, oldConnection);
        }
    }

    private sealed class RepositoryServer : HttpMessageHandler
    {
        internal string Sha { get; set; } = new string('a', 40);
        internal int Writes { get; private set; }
        internal byte[] Content { get; private set; } = Array.Empty<byte>();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            // An asynchronous response exercises the same dispatcher path as a live request.
            await Task.Yield();
            object response;
            if (request.RequestUri!.AbsolutePath == "/repos/test/private")
                response = new { @private = true, visibility = "private", id = 41L, default_branch = "main" };
            else if (request.RequestUri.AbsolutePath == "/repos/test/private/contents/" + Remote)
            {
                if (request.Method == HttpMethod.Put)
                {
                    using var body = JsonDocument.Parse(await request.Content!.ReadAsStringAsync(cancellationToken));
                    if (body.RootElement.GetProperty("sha").GetString() != Sha)
                        throw new InvalidOperationException("Upload omitted its expected revision.");
                    Content = Convert.FromBase64String(body.RootElement.GetProperty("content").GetString()!);
                    Sha = (++Writes).ToString("x40");
                    response = new { content = new { sha = Sha } };
                }
                else response = new { type = "file", sha = Sha };
            }
            else throw new InvalidOperationException("Unexpected repository test request.");

            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(response))
            };
        }
    }
}
