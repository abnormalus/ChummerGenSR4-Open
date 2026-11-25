#nullable enable
using System;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Reflection;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.VisualTree;
using SkiaSharp;

namespace Chummer;

internal static class NativeRepositoryPortraitChecks
{
    internal static async Task RunAsync(frmMain owner, Action<bool, string> check)
    {
        using var bitmap = new SKBitmap(128, 64);
        bitmap.Erase(SKColors.Teal);
        using var encoded = bitmap.Encode(SKEncodedImageFormat.Png, 100);
        string portrait = Convert.ToBase64String(encoded.ToArray());
        const string legacy = "<character><name>Legacy snapshot</name><mugshotbase64>{0}</mugshotbase64></character>";
        byte[] legacySnapshot = Encoding.Unicode.GetPreamble()
            .Concat(Encoding.Unicode.GetBytes(string.Format(legacy, portrait))).ToArray();
        byte[]? legacyPreview = RepositoryPortrait.ReadThumbnail(legacySnapshot);
        using var legacyBitmap = legacyPreview == null ? null : SKBitmap.Decode(legacyPreview);
        check(legacyBitmap is { Width: 96, Height: 96 },
            "Repository preview reads the legacy embedded portrait in UTF-16");
        check(RepositoryPortrait.ReadThumbnail(Encoding.UTF8.GetBytes(
            "<!DOCTYPE character [<!ENTITY bad SYSTEM 'file:///unreadable'>]><character><mugshot>&bad;</mugshot></character>")) == null,
            "Repository previews reject external XML entities");

        using var server = new RepositoryServer(portrait);
        using var client = new HttpClient(server) { BaseAddress = new Uri("https://api.github.com/") };
        var field = typeof(frmMain).GetField("characterRepository", BindingFlags.Instance | BindingFlags.NonPublic)!;
        var previous = field.GetValue(owner);
        field.SetValue(owner, new CharacterRepository("test/characters", "characters", "github_pat_example", client));
        var pending = owner.OpenRepositoryAsync();
        var window = NativeDialogHost.ActiveWindow!;
        try
        {
            var list = window.GetVisualDescendants().OfType<ListBox>().Single(control => control.Name == "RepositoryCharacterList");
            await WaitAsync(() => list.Items.Count == 2 && list.GetVisualDescendants().OfType<Image>().Any(image => image.Source != null));
            var rows = list.Items.Cast<RepositoryCharacterRow>().ToArray();
            check(rows[0].File.Name == "Alice Echo.chum" && rows[0].Portrait is { PixelSize.Width: 96, PixelSize.Height: 96 } &&
                rows[1].Portrait == null && list.GetVisualDescendants().OfType<TextBlock>().Any(text => text.Text == "BN" && text.IsEffectivelyVisible),
                "Open from Repository shows an embedded portrait and initials for an unreadable portrait");
            var search = window.GetVisualDescendants().OfType<TextBox>().Single();
            var footer = ((Grid)window.Content!).Children.OfType<WrapPanel>().Single();
            window.Width = 320;
            window.Height = 360;
            Avalonia.Controls.Documents.TextElement.SetFontSize((Grid)window.Content!, search.FontSize * 1.5);
            await WaitAsync(() => window.ClientSize.Width <= 320 && footer.Bounds.Height > 35);
            var longFilename = list.GetVisualDescendants().OfType<TextBlock>().Single(text => text.Text == rows[1].File.Name);
            check(longFilename.Bounds.Height > longFilename.FontSize * 2 &&
                search.Bounds.Bottom <= list.Bounds.Top && list.Bounds.Height > 0 &&
                list.Bounds.Bottom <= footer.Bounds.Top && footer.Bounds.Bottom <= window.ClientSize.Height &&
                footer.Children.All(button => button.Bounds.Right <= footer.Bounds.Width + 1),
                "Repository picker wraps long filenames and keeps its actions visible at 150% text and narrow width");
            search.Text = "echo";
            await WaitAsync(() => list.Items.Count == 1);
            check(ReferenceEquals(list.Items[0], rows[0]) && rows[0].Portrait != null,
                "Filename search retains the existing thumbnail and row identity");
        }
        finally
        {
            window.Close();
            await pending;
            field.SetValue(owner, previous);
        }
        await Avalonia.Threading.Dispatcher.UIThread.InvokeAsync(() => { }, Avalonia.Threading.DispatcherPriority.ApplicationIdle);
        check(server.Blobs == 2 && NativeDialogHost.Count == 0 &&
            Majorsilence.Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled &&
            !Majorsilence.Forms.Application.OpenForms.OfType<Majorsilence.Forms.MessageBoxForm>().Any(),
            "Closing the picker releases ownership without reopening or redownloading a character");
    }

    private static async Task WaitAsync(Func<bool> condition)
    {
        var timeout = DateTime.UtcNow.AddSeconds(8);
        while (!condition())
        {
            if (DateTime.UtcNow >= timeout) throw new InvalidOperationException("Repository portrait picker did not complete.");
            await Task.Delay(20);
        }
    }

    private sealed class RepositoryServer(string portrait) : HttpMessageHandler
    {
        private static readonly string Alice = new('a', 40), Broken = new('b', 40);
        private int blobs;
        internal int Blobs => blobs;

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            string path = request.RequestUri!.AbsolutePath;
            object payload;
            if (path.EndsWith("/contents/characters", StringComparison.Ordinal))
                payload = new[]
                {
                    new { type = "file", name = "Alice Echo.chum", path = "characters/Alice Echo.chum", sha = Alice, size = 1024 },
                    new { type = "file", name = "Broken Note with a deliberately long filename for narrow character lists.chum", path = "characters/Broken Note with a deliberately long filename for narrow character lists.chum", sha = Broken, size = 128 }
                };
            else if (path.Contains("/git/blobs/", StringComparison.Ordinal))
            {
                Interlocked.Increment(ref blobs);
                await Task.Delay(40, cancellationToken);
                bool valid = path.EndsWith(Alice, StringComparison.Ordinal);
                string xml = valid
                    ? $"<character><mainmugshotindex>1</mainmugshotindex><mugshots><mugshot>corrupt</mugshot><mugshot>{portrait}</mugshot></mugshots></character>"
                    : "<character><mugshot>https://example.invalid/portrait.png</mugshot></character>";
                payload = new { sha = valid ? Alice : Broken, encoding = "base64", content = Convert.ToBase64String(Encoding.UTF8.GetBytes(xml)) };
            }
            else
                payload = new { @private = true, visibility = "private", id = 42, default_branch = "main" };
            return new HttpResponseMessage(HttpStatusCode.OK)
            {
                Content = new StringContent(JsonSerializer.Serialize(payload), Encoding.UTF8, "application/json")
            };
        }
    }
}
