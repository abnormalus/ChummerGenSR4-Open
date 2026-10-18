#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;
namespace Chummer;
internal static class NativeNaturalWeaponChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot());
        var before = character.CaptureSnapshot();
        using (var prompt = new frmNaturalWeapon(character))
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (NaturalWeaponWindow)NativeDialogHost.ActiveWindow!; await Task.Delay(100);
            check(window.ModifierInput.Minimum == -10 && window.ModifierInput.Maximum == 10 && window.APInput.Minimum == -10 && window.APInput.Maximum == 10 && window.ReachInput.Minimum == -3 && window.ReachInput.Maximum == 3 && window.ModifierInput.Value == 0 && window.APInput.Value == 0 && window.ReachInput.Value == 0,
                "Native natural weapon preserves signed modifier AP and reach limits");
            window.ModifierInput.Text = "-4"; await Task.Delay(30);
            window.ModifierInput.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_IncreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(window.ModifierInput.Value == -3, "Native natural weapon supports signed typing and standard spinner increase");
            window.NameInput.Text = "Synthetic natural weapon"; window.APInput.Value = 2; window.ReachInput.Value = -1;
            var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previous = nativeOwner.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    nativeOwner.RequestedThemeVariant = theme; await Task.Delay(100);
                    check(window.ActualThemeVariant == theme && window.SkillInput.Bounds.Width > 100, "Native natural weapon follows " + (theme == ThemeVariant.Dark ? "dark" : "light") + " owner theme");
                    using var image = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192,192));
                    image.Render(window); image.Save(Path.Combine(output, "native-natural-weapon-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { nativeOwner.RequestedThemeVariant = previous; }
            window.Accept();
            check(await pending == Forms.DialogResult.OK && prompt.SelectedWeapon.Damage == "(STR/2)-3P" && prompt.SelectedWeapon.AP == "+2" && prompt.SelectedWeapon.Reach == -1 && prompt.SelectedWeapon.Cost == 0,
                "Native natural weapon acceptance retains legacy damage AP reach and zero cost");
            check(character.CaptureSnapshot().AsSpan().SequenceEqual(before), "Native natural weapon preparation leaves inventory and character unchanged");
        }
        using (var prompt = new frmNaturalWeapon(character))
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (NaturalWeaponWindow)NativeDialogHost.ActiveWindow!;
            window.SkillInput.SelectedIndex = -1; window.Accept();
            check(!pending.IsCompleted && !window.AcceptButton.IsEnabled, "Native natural weapon cannot accept a missing catalog skill");
            window.NameInput.Text = "Discarded synthetic draft"; window.Close();
            check(await pending == Forms.DialogResult.Cancel && character.CaptureSnapshot().AsSpan().SequenceEqual(before), "Native natural weapon cancellation preserves the complete character");
        }
        foreach (bool career in new[] { false, true })
        {
            var model = new Character(); model.LoadSnapshot(original.CaptureSnapshot()); model.Created = career;
            using Forms.Form editor = career ? new frmCareer(model) { Loading = true, MdiParent = owner } : new frmCreate(model) { Loading = true, MdiParent = owner }; editor.Show();
            try
            {
                var command = editor.GetType().GetMethod("tsCreateNaturalWeapon_Click", BindingFlags.Instance | BindingFlags.NonPublic)!;
                int count = model.Weapons.Count;
                void Execute(bool accept)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                        var window = (NaturalWeaponWindow)NativeDialogHost.ActiveWindow!; window.NameInput.Text = "Synthetic committed natural weapon";
                        window.DamageBaseInput.SelectedIndex = 7; window.ModifierInput.Value = 2; window.DamageTypeInput.SelectedIndex = 1;
                        if (accept) window.Accept(); else window.Close();
                    });
                    command.Invoke(editor, new object[] { editor, EventArgs.Empty });
                }
                Execute(false); check(model.Weapons.Count == count, "Native " + (career ? "career" : "creation") + " natural weapon cancellation adds no inventory");
                Execute(true); var weapon = model.Weapons.Last();
                check(model.Weapons.Count == count + 1 && weapon.Damage == "7+2S" && weapon.UseSkill.Length > 0 && weapon.Source.Length > 0,
                    "Native " + (career ? "career" : "creation") + " natural weapon command inserts one catalog-linked accepted weapon");
                var reopened = new Character(); reopened.LoadSnapshot(model.CaptureSnapshot());
                check(reopened.Weapons.Last().Damage == "7+2S" && reopened.Weapons.Last().InternalId == weapon.InternalId,
                    "Native " + (career ? "career" : "creation") + " natural weapon survives legacy snapshot round trip");
            }
            finally { editor.GetType().GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false); editor.Close(); }
        }
    }
}
