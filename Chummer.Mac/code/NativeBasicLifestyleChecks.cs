#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeBasicLifestyleChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Character character, string output, Action<bool, string> check)
    {
        var books = character.Options.Books.ToArray(); var before = character.CaptureSnapshot();
        try
        {
            character.Options.Books.Clear(); character.Options.Books.Add("SR4");
            var target = new Lifestyle(character);
            using (var prompt = new frmSelectLifestyle(target, character))
            {
                var pending = prompt.ShowDialogAsync(owner); var window = (BasicLifestylePickerWindow)NativeDialogHost.ActiveWindow!;
                window.SearchBox.Text = "Low"; await Task.Delay(100);
                check(window.RoommatesInput.Minimum == 0 && window.RoommatesInput.Maximum == 10 && window.PercentageInput.Maximum == 900 && window.PercentageInput.Value == 100,
                    "Native basic lifestyle integer controls retain roommate and percentage bounds and defaults");
                window.RoommatesInput.Text = "2"; window.PercentageInput.Text = "33"; await Task.Delay(30);
                check(window.Model.Details!.Cost == string.Format("{0:###,###,##0¥}", 792), "Native basic lifestyle typing updates the established preview price");
                window.RoommatesInput.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_IncreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(30);
                check(window.Model.Roommates == 3, "Native basic lifestyle standard spinner updates the roommate draft");
                window.RoommatesInput.Text = "2"; window.PercentageInput.Text = ""; await Task.Delay(30);
                check(!window.AcceptButton.IsEnabled && !window.AddMoreButton.IsEnabled && window.Model.Details!.Cost == "",
                    "Native basic lifestyle cleared required percentage blocks both acceptance actions and clears preview");
                window.PercentageInput.Text = "0"; await Task.Delay(30);
                check(window.AcceptButton.IsEnabled && window.Model.Details!.Cost == string.Format("{0:###,###,##0¥}", 0), "Native basic lifestyle explicit zero share remains valid");
                window.PercentageInput.Text = "33";
                var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previous = nativeOwner.RequestedThemeVariant;
                try
                {
                    foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                    {
                        nativeOwner.RequestedThemeVariant = theme; window.Width = 800; window.Height = 560; await Task.Delay(100);
                        check(window.ActualThemeVariant == theme && window.RoommatesInput.Bounds.Width >= 110 && window.PercentageInput.Bounds.Width >= 110,
                            "Native basic lifestyle follows " + theme + " owner theme with usable numeric layout");
                        using var image = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192));
                        image.Render(window); image.Save(Path.Combine(output, "native-basic-lifestyle-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                    }
                }
                finally { nativeOwner.RequestedThemeVariant = previous; }
                window.Accept(true);
                check(await pending == Forms.DialogResult.OK && prompt.AddAgain && target.Name == "Low" && target.Cost == 2000 && target.Roommates == 2 && target.Percentage == 33,
                    "Native basic lifestyle returns accepted base cost and separate financial choices");
            }
            var source = new Lifestyle(character) { Name = "Low", LifestyleName = "Synthetic residence", Roommates = 3, Percentage = 25, Months = 4 };
            using (var prompt = new frmSelectLifestyle(new Lifestyle(character), character))
            {
                prompt.SetLifestyle(source); var pending = prompt.ShowDialogAsync(owner); var window = (BasicLifestylePickerWindow)NativeDialogHost.ActiveWindow!;
                check(!window.ItemList.IsEnabled && !window.SearchBox.IsEnabled && window.Model.Roommates == 3 && window.Model.Percentage == 25,
                    "Native basic lifestyle edit preloads drafts and locks catalog identity");
                window.PercentageInput.Value = 50; window.Close();
                check(await pending == Forms.DialogResult.Cancel && !prompt.AddAgain && source.Percentage == 25 && source.Roommates == 3 && source.Months == 4,
                    "Native basic lifestyle canceled edit preserves its original source");
            }
            using (var prompt = new frmSelectLifestyle(new Lifestyle(character), character))
            {
                prompt.SetLifestyle(source); var pending = prompt.ShowDialogAsync(owner); var window = (BasicLifestylePickerWindow)NativeDialogHost.ActiveWindow!;
                character.Options.Books.Clear(); window.Accept(false);
                check(!pending.IsCompleted && !window.Model.CanAccept && source.Name == "Low", "Native basic lifestyle changed source cannot silently substitute another residence");
                window.Close(); await pending;
            }
            character.Options.Books.Add("SR4");
            check(character.CaptureSnapshot().AsSpan().SequenceEqual(before), "Native basic lifestyle prompt never inserts inventory or changes the owned character");
            foreach (bool career in new[] { false, true })
            {
                var copy = new Character(); copy.LoadSnapshot(character.CaptureSnapshot()); copy.Created = career; copy.Nuyen = 100000;
                using Forms.Form editor = career ? new frmCareer(copy) { Loading = true, MdiParent = owner } : new frmCreate(copy) { Loading = true, MdiParent = owner }; editor.Show();
                try
                {
                    void Execute(string method, bool accept, int roommates = 2, int percent = 33)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            var window = (BasicLifestylePickerWindow)NativeDialogHost.ActiveWindow!;
                            if (!window.Model.IsEditing) window.SearchBox.Text = "Low";
                            window.RoommatesInput.Value = roommates; window.PercentageInput.Value = percent;
                            if (accept) window.Accept(false); else window.Close();
                        });
                        editor.GetType().GetMethod(method, Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                    }
                    string mode = career ? "career" : "creation";
                    int count = copy.Lifestyles.Count, money = copy.Nuyen;
                    editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
                    Execute("cmdAddLifestyle_Click", false);
                    check(copy.Lifestyles.Count == count && copy.Nuyen == money && !Field<bool>(editor, "_blnIsDirty"), "Native " + mode + " canceled basic lifestyle add preserves owned data");
                    Execute("cmdAddLifestyle_Click", true); var added = copy.Lifestyles.Last();
                    check(copy.Lifestyles.Count == count + 1 && added.Name == "Low" && added.Cost == 2000 && added.Roommates == 2 && added.Percentage == 33 && copy.Nuyen == money && (!career || added.Months == 0),
                        "Native " + mode + " basic lifestyle add retains existing financial and month policy");
                    added.LifestyleName = "Synthetic residence"; added.Months = 4; added.Notes = "Synthetic residence notes"; added.Purchased = true;
                    var tree = Field<Forms.TreeView>(editor, "treLifestyles"); var node = tree.Nodes[0].Nodes.Cast<Forms.TreeNode>().Single(item => item.Tag?.ToString() == added.InternalId); tree.SelectedNode = node;
                    editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
                    Execute("treLifestyles_DoubleClick", false, 1, 50);
                    check(copy.Lifestyles.Contains(added) && added.Roommates == 2 && added.Percentage == 33 && added.Months == 4 && added.Notes == "Synthetic residence notes" && added.Purchased && !Field<bool>(editor, "_blnIsDirty"), "Native " + mode + " canceled basic lifestyle edit preserves original object and months");
                    tree.SelectedNode = node; Execute("treLifestyles_DoubleClick", true, 1, 50);
                    var edited = copy.Lifestyles.Single(item => item.InternalId == added.InternalId);
                    check(!ReferenceEquals(added, edited) && edited.Name == "Low" && edited.Months == 4 && edited.LifestyleName == "Synthetic residence" && edited.Roommates == 1 && edited.Percentage == 50 && edited.Notes == "Synthetic residence notes" && edited.Purchased && copy.Nuyen == money && Field<bool>(editor, "_blnIsDirty"),
                        "Native " + mode + " accepted basic lifestyle edit preserves GUID months and residence name");
                    var restored = new Character(); restored.LoadSnapshot(copy.CaptureSnapshot()); var saved = restored.Lifestyles.Single(item => item.InternalId == edited.InternalId);
                    check(saved.Roommates == 1 && saved.Percentage == 50 && saved.Months == 4 && saved.LifestyleName == "Synthetic residence" && saved.Notes == "Synthetic residence notes" && saved.Purchased, "Native " + mode + " basic lifestyle edits survive the legacy format");
                }
                finally { editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close(); }
            }
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(books); }
    }
}
