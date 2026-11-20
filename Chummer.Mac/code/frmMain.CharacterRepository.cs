#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Data.Converters;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Styling;
using Avalonia.Threading;
using Majorsilence.Forms;
using Button = Avalonia.Controls.Button;
using ListBox = Avalonia.Controls.ListBox;
using StackPanel = Avalonia.Controls.StackPanel;
using TextBox = Avalonia.Controls.TextBox;

namespace Chummer;

public partial class frmMain
{
    private CharacterRepository? characterRepository;
    private RepositoryConnection? savedRepository;
    private bool repositoryBusy;
    private bool renewalReminderShown;

    private bool RepositoryTokenExpired => savedRepository != null &&
        (characterRepository == null || characterRepository.ExpiresAt <= DateTimeOffset.UtcNow);

    private void InitializeCharacterRepositoryMenu()
    {
        var connect = new ToolStripMenuItem("Connect Character Repository…");
        var open = new ToolStripMenuItem("Open from Repository…");
        var save = new ToolStripMenuItem("Save to Repository…");
        fileMenu.DropDownItems.Insert(6, connect);
        fileMenu.DropDownItems.Insert(7, open);
        fileMenu.DropDownItems.Insert(8, save);

        void Refresh()
        {
            connect.Enabled = !repositoryBusy;
            open.Enabled = !repositoryBusy && savedRepository != null;
            save.Enabled = open.Enabled && ActiveMdiChild is frmCreate or frmCareer;
        }

        async Task Run(string title, Func<Task> action, bool repositoryOperation = false)
        {
            if (repositoryBusy) return;
            repositoryBusy = true;
            Refresh();
            try
            {
                if (repositoryOperation && RepositoryTokenExpired && !await RenewRepositoryAsync()) return;
                try { await action(); }
                catch (RepositoryTokenExpiredException) when (repositoryOperation && !IsDisposed)
                {
                    if (await RenewRepositoryAsync()) await action();
                }
            }
            catch (RepositoryTokenExpiredException error) when (repositoryOperation)
            {
                ExpireRepositoryToken();
                if (!IsDisposed) MessageBox.Show(error.Message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            catch (Exception error) when (error is IOException or HttpRequestException or
                OperationCanceledException or UnauthorizedAccessException or JsonException or
                FormatException or ArgumentException)
            {
                string message = error is HttpRequestException or OperationCanceledException
                    ? "GitHub is unavailable. Check your connection and try again."
                    : error.Message;
                if (!IsDisposed) MessageBox.Show(message, title, MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            finally
            {
                repositoryBusy = false;
                if (!IsDisposed) Refresh();
            }
        }

        async Task RemindIfExpiredAsync()
        {
            if (!IsDisposed && !repositoryBusy && NativeDialogHost.Count == 0 &&
                !renewalReminderShown && RepositoryTokenExpired)
                await Run("Renew GitHub Token", async () => { await RenewRepositoryAsync(); });
        }

        connect.Click += async (_, _) => await Run("Connect Character Repository",
            () => ConnectRepositoryAsync(false));
        open.Click += async (_, _) => await Run("Open from Repository", OpenRepositoryAsync, true);
        save.Click += async (_, _) =>
        {
            switch (ActiveMdiChild)
            {
                case frmCreate creation when HasRepositoryLink(creation.RepositoryLocalFile):
                    creation.SaveCharacter();
                    return;
                case frmCareer career when HasRepositoryLink(career.RepositoryLocalFile):
                    career.SaveCharacter();
                    return;
            }
            await Run("Save to Repository", SaveRepositoryAsync, true);
        };
        MdiChildActivate += (_, _) => Refresh();
        var timer = new DispatcherTimer { Interval = TimeSpan.FromMinutes(1) };
        timer.Tick += async (_, _) => await RemindIfExpiredAsync();
        FormClosed += (_, _) => timer.Stop();
        Shown += async (_, _) =>
        {
            if (MacPaths.TestUserDirectory != null) return;
            try
            {
                savedRepository = RepositoryCredentialStore.Read();
                if (savedRepository?.Token != null)
                    characterRepository = new CharacterRepository(savedRepository);
            }
            catch (Exception error) when (error is IOException or JsonException or ArgumentException)
            {
                MessageBox.Show(error.Message, "Connect Character Repository", MessageBoxButtons.OK, MessageBoxIcon.Warning);
            }
            Refresh();
            timer.Start();
            await RemindIfExpiredAsync();
        };
        Refresh();
    }

    private void ExpireRepositoryToken()
    {
        characterRepository = null;
        if (savedRepository?.Token == null) return;
        savedRepository = savedRepository with { Token = null };
        try { RepositoryCredentialStore.Save(savedRepository); }
        catch (IOException error) { AppDiagnostics.Record("repository-credential-clear-failed", error); }
    }

    private async Task<bool> RenewRepositoryAsync()
    {
        renewalReminderShown = true;
        ExpireRepositoryToken();
        await ConnectRepositoryAsync(true);
        return characterRepository != null;
    }

    private static Window RepositoryDialog(string title, StackPanel content)
    {
        var window = new Window
        {
            Title = title,
            Width = 530,
            SizeToContent = SizeToContent.Height,
            WindowStartupLocation = WindowStartupLocation.CenterOwner,
            CanResize = false,
            Content = content
        };
        NativeControlPresentation.AddTheme(window.Styles);
        return window;
    }

    private static StackPanel RepositoryPanel() => new() { Margin = new Thickness(20), Spacing = 10 };

    private static TextBlock RepositoryLabel(string text) => new()
    {
        Text = text,
        TextWrapping = Avalonia.Media.TextWrapping.Wrap
    };

    internal async Task ConnectRepositoryAsync(bool renewing)
    {
        var panel = RepositoryPanel();
        var dialog = RepositoryDialog(renewing ? "Renew GitHub Token" : "Connect Character Repository", panel);
        var status = RepositoryLabel(characterRepository == null ? "" : $"Connected to {characterRepository.Name}");
        var name = new TextBox { Text = savedRepository?.Name, PlaceholderText = "owner/repository", IsReadOnly = renewing };
        var folder = new TextBox { Text = savedRepository?.Folder ?? "characters", IsReadOnly = renewing };
        var token = new TextBox { PasswordChar = '●', PlaceholderText = "Paste the new token" };
        Avalonia.Automation.AutomationProperties.SetName(name, "GitHub repository");
        Avalonia.Automation.AutomationProperties.SetName(folder, "Character folder");
        Avalonia.Automation.AutomationProperties.SetName(token, "GitHub token");
        var create = new Button { Content = "Create private repository in GitHub" };
        var authorize = new Button { Content = renewing ? "Open GitHub to create a replacement token" : "Create fine-grained token in GitHub" };
        var connect = new Button { Content = renewing ? "Verify and save new token" : "Verify and connect", IsDefault = true };
        var disconnect = new Button { Content = "Disconnect", IsVisible = savedRepository != null };
        if (renewing)
            panel.Children.Add(RepositoryLabel("Your GitHub token has expired or was revoked. Create a replacement to use this repository again."));
        else
        {
            panel.Children.Add(RepositoryLabel("Use any private repository you have access to. If you create one, select Private and add a README on GitHub."));
            panel.Children.Add(create);
        }
        panel.Children.Add(RepositoryLabel("Repository"));
        panel.Children.Add(name);
        panel.Children.Add(RepositoryLabel("Character folder"));
        panel.Children.Add(folder);
        panel.Children.Add(authorize);
        panel.Children.Add(RepositoryLabel("In GitHub, choose Only select repositories and select the repository above. Paste the new token below. Chummer saves it in your system credential store."));
        panel.Children.Add(token);
        panel.Children.Add(connect);
        panel.Children.Add(disconnect);
        if (renewing)
        {
            var later = new Button { Content = "Renew later", IsCancel = true };
            later.Click += (_, _) => dialog.Close();
            panel.Children.Add(later);
        }
        panel.Children.Add(status);
        void Launch(string url)
        {
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (System.ComponentModel.Win32Exception) { status.Text = "Could not open your browser. Visit " + url; }
        }

        create.Click += (_, _) => Launch("https://github.com/new?name=sr4-dossier-characters&visibility=private&description=Private%20Chummer%20character%20storage");
        authorize.Click += (_, _) =>
        {
            string owner = (name.Text ?? "").Split('/')[0].Trim();
            string url = "https://github.com/settings/personal-access-tokens/new?name=Chummer%20Character%20Repository&contents=write";
            if (owner.Length > 0) url += "&target_name=" + Uri.EscapeDataString(owner);
            Launch(url);
            token.Focus();
        };
        disconnect.Click += (_, _) =>
        {
            try
            {
                RepositoryCredentialStore.Remove();
                characterRepository = null;
                savedRepository = null;
                dialog.Close();
            }
            catch (IOException error) { status.Text = error.Message; }
        };
        connect.Click += async (_, _) =>
        {
            connect.IsEnabled = false;
            disconnect.IsEnabled = false;
            status.Text = "Checking private repository access…";
            try
            {
                string replacement = token.Text?.Trim() ?? "";
                var candidate = renewing
                    ? new CharacterRepository(savedRepository! with { Token = replacement, ExpiresAt = null })
                    : new CharacterRepository(name.Text ?? "", folder.Text?.Trim() ?? "", replacement);
                await candidate.ListAsync();
                if (!dialog.IsVisible) return;
                RepositoryCredentialStore.Save(candidate.Connection);
                dialog.Close(candidate);
            }
            catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or JsonException)
            {
                status.Text = error is HttpRequestException or OperationCanceledException
                    ? "GitHub is unavailable. Check your connection and try again." : error.Message;
            }
            finally
            {
                connect.IsEnabled = true;
                disconnect.IsEnabled = true;
            }
        };

        var connection = await NativeDialogHost.ShowAsync<CharacterRepository>(dialog, this);
        token.Text = "";
        if (connection != null)
        {
            characterRepository = connection;
            savedRepository = connection.Connection;
            renewalReminderShown = false;
        }
    }

    internal async Task OpenRepositoryAsync()
    {
        var repository = characterRepository!;
        var panel = RepositoryPanel();
        var dialog = RepositoryDialog($"Open from {repository.Name}", panel);
        var search = new TextBox { PlaceholderText = "Search filenames", IsEnabled = false };
        var status = RepositoryLabel("Loading characters…");
        using var previews = new CancellationTokenSource();
        var cancellation = previews.Token;
        RepositoryCharacterRow[] rows = [];
        var list = new ListBox
        {
            Name = "RepositoryCharacterList",
            Height = 250,
            ItemTemplate = new FuncDataTemplate<RepositoryCharacterRow>((row, _) =>
            {
                var initials = new TextBlock
                {
                    Text = RepositoryPortrait.Initials(row.File.Name),
                    HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Center,
                    VerticalAlignment = VerticalAlignment.Center
                };
                var image = new Avalonia.Controls.Image { Width = 36, Height = 36, Stretch = Stretch.UniformToFill };
                image.Bind(Avalonia.Controls.Image.SourceProperty, new Avalonia.Data.Binding(nameof(RepositoryCharacterRow.Portrait)) { Source = row });
                initials.Bind(Visual.IsVisibleProperty, new Avalonia.Data.Binding(nameof(RepositoryCharacterRow.Portrait))
                    { Source = row, Converter = ObjectConverters.IsNull });
                var avatar = new Border
                {
                    Name = "RepositoryCharacterAvatar",
                    Width = 36,
                    Height = 36,
                    ClipToBounds = true,
                    CornerRadius = new CornerRadius(5),
                    Child = new Grid { Children = { initials, image } }
                };
                return new StackPanel
                {
                    Orientation = Avalonia.Layout.Orientation.Horizontal,
                    Spacing = 10,
                    Children = { avatar, new TextBlock { Text = row.File.Name, VerticalAlignment = VerticalAlignment.Center } }
                };
            })
        };
        list.Styles.Add(new Style(selector => selector.OfType<Border>().Name("RepositoryCharacterAvatar"))
        {
            Setters = { new Setter(Border.BackgroundProperty, new DynamicResourceExtension("SystemControlBackgroundListLowBrush")) }
        });
        var open = new Button { Content = "Open working copy", IsDefault = true, IsEnabled = false };
        var history = new Button { Content = "View file history in GitHub", IsEnabled = false };
        panel.Children.Add(search);
        panel.Children.Add(list);
        panel.Children.Add(status);
        panel.Children.Add(open);
        open.Click += (_, _) => { if (list.SelectedItem is RepositoryCharacterRow row) dialog.Close(row.File); };
        panel.Children.Add(history);
        list.SelectionChanged += (_, _) => history.IsEnabled = open.IsEnabled = list.SelectedItem is RepositoryCharacterRow;
        history.Click += (_, _) =>
        {
            if (list.SelectedItem is not RepositoryCharacterRow row) return;
            var file = row.File;
            string path = string.Join("/", file.Path.Split('/').Select(Uri.EscapeDataString));
            string branch = Uri.EscapeDataString(repository.Branch);
            string url = $"https://github.com/{repository.Name}/commits/{branch}/{path}";
            try { Process.Start(new ProcessStartInfo(url) { UseShellExecute = true }); }
            catch (System.ComponentModel.Win32Exception) { status.Text = "Could not open your browser. Visit " + url; }
        };
        bool requiresRenewal = false;
        dialog.Opened += async (_, _) =>
        {
            try
            {
                var files = await repository.ListAsync();
                if (!dialog.IsVisible) return;
                rows = files.Select(file => new RepositoryCharacterRow(file)).ToArray();
                void Filter()
                {
                    var matches = rows.Where(row =>
                        row.File.Name.Contains(search.Text ?? "", StringComparison.OrdinalIgnoreCase)).ToArray();
                    list.ItemsSource = matches;
                    status.Text = files.Count == 0 ? "This folder contains no .chum files." :
                        matches.Length == 0 ? "No matching characters." : $"{matches.Length} character files";
                }
                search.TextChanged += (_, _) => Filter();
                search.IsEnabled = true;
                Filter();
                await Parallel.ForEachAsync(rows, new ParallelOptions
                {
                    CancellationToken = cancellation,
                    MaxDegreeOfParallelism = 3
                }, async (row, token) =>
                {
                    if (row.File.Size > RepositoryPortrait.MaximumCharacterBytes)
                        return;
                    try
                    {
                        var bytes = await repository.DownloadBlobAsync(row.File, token);
                        if (RepositoryPortrait.ReadThumbnail(bytes) is { } png)
                            await Dispatcher.UIThread.InvokeAsync(() =>
                            {
                                if (!cancellation.IsCancellationRequested)
                                    row.SetPortrait(png);
                            });
                    }
                    catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException
                        or JsonException or FormatException or InvalidOperationException or System.Collections.Generic.KeyNotFoundException)
                    {
                        // The filename and initials remain usable if an optional preview fails.
                    }
                });
            }
            catch (RepositoryTokenExpiredException)
            {
                if (!dialog.IsVisible) return;
                requiresRenewal = true;
                dialog.Close();
            }
            catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or JsonException)
            {
                status.Text = error is HttpRequestException or OperationCanceledException
                    ? "GitHub is unavailable. Check your connection and reopen the picker." : error.Message;
            }
        };

        var selected = await NativeDialogHost.ShowAsync<RepositoryFile>(dialog, this);
        previews.Cancel();
        list.ItemsSource = null;
        foreach (var row in rows)
            row.Dispose();
        if (requiresRenewal) throw new RepositoryTokenExpiredException();
        if (selected == null) return;
        var bytes = await repository.DownloadAsync(selected);
        if (IsDisposed) return;
        // Each open gets its own working copy. Existing editors and local files stay intact.
        string directory = Path.Combine(MacPaths.UserDirectory, "saves", "repository", Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(directory);
        string fileName = CharacterRepository.ValidSegment(selected.Name) &&
            selected.Name.IndexOfAny(Path.GetInvalidFileNameChars()) < 0 ? selected.Name : "Character.chum";
        string path = Path.Combine(directory, fileName);
        await File.WriteAllBytesAsync(path, bytes);
        WriteRepositoryRevision(path, new RepositoryRevision(repository.Id, repository.Branch, selected.Path,
            selected.Sha, DateTimeOffset.UtcNow));
        if (!IsDisposed) LoadCharacter(path);
    }

    private async Task SaveRepositoryAsync()
    {
        var repository = characterRepository!;
        var editor = ActiveMdiChild;
        bool saved = editor switch
        {
            frmCreate creation => creation.SaveRepositoryWorkingCopy(),
            frmCareer career => career.SaveRepositoryWorkingCopy(),
            _ => false
        };
        if (!saved) return;
        string local = editor switch
        {
            frmCreate creation => creation.RepositoryLocalFile,
            frmCareer career => career.RepositoryLocalFile,
            _ => throw new InvalidOperationException()
        };

        var bytes = File.ReadAllBytes(local);
        string revisionFile = RepositoryRevisionPath(local);
        var revision = File.Exists(revisionFile)
            ? JsonSerializer.Deserialize<RepositoryRevision>(File.ReadAllText(revisionFile)) : null;
        if (revision != null && (revision.RepositoryId != repository.Id || revision.Branch != repository.Branch ||
            !revision.Path.StartsWith(repository.Folder + "/", StringComparison.Ordinal)))
            throw new IOException("This working copy is linked to another repository, branch, or folder. Reconnect to it or use local Save As to link a separate copy.");

        var panel = RepositoryPanel();
        var dialog = RepositoryDialog("Save to Repository", panel);
        panel.Children.Add(RepositoryLabel($"{repository.Name} / {repository.Folder}"));
        var name = new TextBox
        {
            Text = revision == null ? Path.GetFileName(local) : revision.Path[(repository.Folder.Length + 1)..],
            IsReadOnly = revision != null
        };
        panel.Children.Add(name);
        panel.Children.Add(RepositoryLabel(revision == null ? "Choose a filename ending in .chum." :
            $"Last synced {revision.LastSynced.ToLocalTime():g}"));
        var status = RepositoryLabel("");
        var upload = new Button { Content = "Upload saved snapshot", IsDefault = true };
        panel.Children.Add(upload);
        panel.Children.Add(status);
        upload.Click += (_, _) =>
        {
            string fileName = name.Text?.Trim() ?? "";
            if (revision != null) { dialog.Close(revision.Path); return; }
            if (!CharacterRepository.ValidSegment(fileName) || fileName.Length <= 5 ||
                !fileName.EndsWith(".chum", StringComparison.OrdinalIgnoreCase))
            {
                status.Text = "Enter a filename ending in .chum.";
                return;
            }
            dialog.Close(repository.Folder + "/" + fileName);
        };
        string? remote = await NativeDialogHost.ShowAsync<string>(dialog, this);
        if (remote == null) return;
        RepositoryRevision result;
        try { result = await repository.SaveAsync(remote, revision, bytes); }
        catch (RepositoryTokenExpiredException) when (!IsDisposed)
        {
            if (!await RenewRepositoryAsync()) return;
            repository = characterRepository!;
            try { result = await repository.SaveAsync(remote, revision, bytes); }
            catch (RepositoryTokenExpiredException error)
            {
                ExpireRepositoryToken();
                throw new IOException(error.Message, error);
            }
        }
        try { WriteRepositoryRevision(local, result); }
        catch (Exception error) when (error is IOException or UnauthorizedAccessException)
        {
            throw new IOException("GitHub saved the snapshot, but Chummer could not record its revision locally. Open the remote working copy before uploading again.", error);
        }
    }

    internal static bool HasRepositoryLink(string local) =>
        !string.IsNullOrEmpty(local) && File.Exists(RepositoryRevisionPath(local));

    internal bool? SaveLinkedCharacter(Form editor, Character character, Func<byte[]?> capture, Action complete, Func<bool>? stillCurrent = null)
    {
        if (!HasRepositoryLink(character.FileName)) return null;
        var save = SaveLinkedCharacterAsync(editor, character, capture, complete, automatic: false, stillCurrent);
        if (!save.IsCompleted) Majorsilence.Forms.Backends.Platform.Backend.RunModalLoop(save);
        return save.GetAwaiter().GetResult();
    }

    private async Task<bool> SaveLinkedCharacterAsync(Form editor, Character character, Func<byte[]?> capture,
        Action complete, bool automatic, Func<bool>? stillCurrent = null)
    {
        string original = character.FileName;
        if (repositoryBusy)
        {
            if (!automatic) ReportSaveProblem(editor, "A repository operation is already in progress. Retry Save when it finishes.", false);
            return false;
        }

        repositoryBusy = true;
        try
        {
            var revision = JsonSerializer.Deserialize<RepositoryRevision>(File.ReadAllText(RepositoryRevisionPath(original)))
                ?? throw new IOException("Chummer could not read the character's repository link. Reopen it from GitHub or use Save As.");
            if (savedRepository == null || revision.RepositoryId != savedRepository.Id || revision.Branch != savedRepository.Branch)
                throw new IOException("Reconnect this character's repository to sync it, or use Save As for a local copy.");

            byte[] baseline = character.CaptureSnapshot();
            byte[]? bytes = capture();
            if (bytes == null) return false;
            var result = await SaveLinkedSnapshotAsync(revision, bytes, automatic);
            if (result == null) return false;

            // Keep a private working copy for reopening without replacing the
            // user's original local file.
            string cacheRoot = Path.Combine(MacPaths.UserDirectory, "saves", "repository");
            string relative = Path.GetRelativePath(cacheRoot, original);
            bool inCache = !Path.IsPathRooted(relative) && relative != ".." &&
                !relative.StartsWith(".." + Path.DirectorySeparatorChar, StringComparison.Ordinal);
            string cache = inCache ? original : Path.Combine(cacheRoot, Guid.NewGuid().ToString("N"), Path.GetFileName(original));
            try
            {
                CharacterFileDialogs.WriteSnapshot(cache, bytes);
                WriteRepositoryRevision(cache, result);
            }
            catch (Exception error) when (error is IOException or UnauthorizedAccessException)
            {
                throw new IOException("GitHub saved the character, but Chummer could not update its working copy. Reopen the character from GitHub.", error);
            }

            if (editor.IsDisposed || character.FileName != original) return false;
            character.FileName = cache;
            bool unchanged = (stillCurrent?.Invoke() ?? true) && character.CaptureSnapshot().SequenceEqual(baseline);
            if (unchanged)
            {
                complete();
                ClearAutosaveFailure(editor);
            }
            else if (!automatic) ReportSaveProblem(editor, "GitHub saved the earlier revision. Newer edits are still open. Save again to sync them.", false);
            return unchanged;
        }
        catch (Exception error) when (error is IOException or HttpRequestException or OperationCanceledException or
            UnauthorizedAccessException or JsonException or ArgumentException or InvalidOperationException or FormatException or System.Xml.XmlException)
        {
            string message = error is HttpRequestException or OperationCanceledException
                ? "GitHub is unavailable. Your edits are still open. Retry Save or use Save As for a local copy."
                : error.Message;
            ReportSaveProblem(editor, message, automatic);
            return false;
        }
        finally { repositoryBusy = false; }
    }

    private async Task<RepositoryRevision?> SaveLinkedSnapshotAsync(RepositoryRevision revision, byte[] bytes, bool automatic)
    {
        if (RepositoryTokenExpired)
        {
            if (automatic) throw new RepositoryTokenExpiredException();
            if (!await RenewRepositoryAsync()) return null;
        }

        try { return await characterRepository!.SaveAsync(revision.Path, revision, bytes); }
        catch (RepositoryTokenExpiredException)
        {
            ExpireRepositoryToken();
            if (automatic || IsDisposed) throw;
            if (!await RenewRepositoryAsync()) return null;
            try { return await characterRepository!.SaveAsync(revision.Path, revision, bytes); }
            catch (RepositoryTokenExpiredException)
            {
                ExpireRepositoryToken();
                throw;
            }
        }
    }

    private static string RepositoryRevisionPath(string local)
    {
        string fullPath = Path.GetFullPath(local);
        if (System.OperatingSystem.IsWindows()) fullPath = fullPath.ToUpperInvariant();
        string key = Convert.ToHexString(SHA256.HashData(Encoding.UTF8.GetBytes(fullPath)));
        return Path.Combine(MacPaths.SettingsDirectory, "repository-revisions", key + ".json");
    }

    private static void WriteRepositoryRevision(string local, RepositoryRevision revision)
    {
        string path = RepositoryRevisionPath(local);
        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        string temporary = path + "." + Guid.NewGuid().ToString("N") + ".tmp";
        try
        {
            File.WriteAllText(temporary, JsonSerializer.Serialize(revision));
            File.Move(temporary, path, overwrite: true);
        }
        finally { File.Delete(temporary); }
    }
}

public partial class frmCreate
{
    internal string RepositoryLocalFile => _objCharacter.FileName;
    internal bool SaveRepositoryWorkingCopy() =>
        (!(_blnIsDirty || chkCharacterCreated.Checked) && File.Exists(RepositoryLocalFile)) || SaveCharacterLocally();
}

public partial class frmCareer
{
    internal string RepositoryLocalFile => _objCharacter.FileName;
    internal bool SaveRepositoryWorkingCopy() =>
        (!_blnIsDirty && File.Exists(RepositoryLocalFile)) || SaveCharacterLocally();
}
