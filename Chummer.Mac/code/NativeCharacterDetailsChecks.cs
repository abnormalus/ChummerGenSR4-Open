#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCharacterDetailsChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static async Task ClosedAsync()
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(5);
        while (NativeDialogHost.ActiveWindow is CharacterDetailsWindow)
        { if (DateTime.UtcNow > deadline) throw new TimeoutException(); await Task.Delay(10); }
        await Task.Delay(20);
    }
    internal static async Task RunAsync(Forms.Form main, Character source, string output, Action<bool, string> check)
    {
        byte[] original = source.CaptureSnapshot();
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original); character.Created = career;
            character.Description = "Paragraph one.\r\n\r\nParagraph two: Ω ¥.";
            character.Background = "Background"; character.Concept = "Concept"; character.Notes = "Original notes";
            using Forms.Form owner = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            owner.MdiParent = main; owner.Show();
            string mode = career ? "career" : "creation";
            try
            {
                Field<Forms.TabControl>(owner, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(owner, "tabCharacterInfo");
                var parent = Field<Forms.Label>(owner, "lblDescription").Parent!;
                var expand = (Forms.Button)parent.Controls.Find("expandCharacterDetail0", false)[0]; expand.PerformClick();
                var window = (CharacterDetailsWindow)NativeDialogHost.ActiveWindow!;
                check(window.Editors[0].Text == character.Description && !Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled,
                    "Native " + mode + " expanded details preserve original paragraphs and modal ownership");
                window.Editors[0].Text = "Discarded draft"; window.Close(); await ClosedAsync();
                check(character.Description.StartsWith("Paragraph one.") && Forms.AvaloniaHostInterop.ToAvaloniaWindow(main).IsEnabled,
                    "Native " + mode + " cancelled detail draft leaves the character unchanged");
                ((Forms.Button)parent.Controls.Find("expandCharacterDetail3", false)[0]).PerformClick();
                window = (CharacterDetailsWindow)NativeDialogHost.ActiveWindow!;
                check(window.Sections.SelectedIndex == 3, "Native " + mode + " notes expansion opens the requested section");
                await Task.Delay(80);
                var notes = window.Editors[3]; notes.Focus(); notes.CaretIndex = notes.Text!.Length;
                notes.RaiseEvent(new TextInputEventArgs { RoutedEvent = InputElement.TextInputEvent, Text = " Ω" });
                string typed = notes.Text!; window.UndoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                bool undone = notes.Text == "Original notes";
                window.RedoButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                check(undone && notes.Text == typed && typed.EndsWith(" Ω"), "Native " + mode + " detail toolbar uses standard text undo and redo");
                notes.Text = "New notes\n\nPlain text <literal> & Ω"; window.Editors[1].Text = "New background";
                window.Width = 720; window.Height = 540; await Task.Delay(100);
                using (var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192)))
                { bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-character-details-" + mode + "-2x.png"), PngBitmapEncoderOptions.Default); }
                check(notes.Bounds.Width > 300 && notes.Bounds.Height > 150 && window.AcceptButton.IsEffectivelyVisible,
                    "Native " + mode + " expanded details resize with readable editing space");
                window.AcceptButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await ClosedAsync();
                var reloaded = new Character(); reloaded.LoadSnapshot(character.CaptureSnapshot());
                check(character.Notes == notes.Text && character.Background == "New background" && character.Concept == "Concept" &&
                    reloaded.Description == character.Description && reloaded.Notes == character.Notes && reloaded.Background == character.Background,
                    "Native " + mode + " accepted details update existing fields and survive unchanged character format");
            }
            finally
            {
                NativeDialogHost.ActiveWindow?.Close();
                owner.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(owner, false); owner.Close();
            }
        }
        check(source.CaptureSnapshot().AsSpan().SequenceEqual(original), "Native expanded detail editing preserves the source fixture");
    }
}
