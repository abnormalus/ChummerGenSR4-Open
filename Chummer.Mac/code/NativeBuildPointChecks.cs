#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;
namespace Chummer;
internal static class NativeBuildPointChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot());
        var before = character.CaptureSnapshot();
        using (var prompt = new frmSelectBP(character))
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (BuildPointWindow)NativeDialogHost.ActiveWindow!;
            check(character.CaptureSnapshot().SequenceEqual(before) && window.MethodInput.IsEnabled && window.PointsInput.Maximum == 5000 && window.AvailabilityInput.Maximum == 100,
                "Native build-point opening preserves character and historical numeric limits");
            window.MethodInput.SelectedIndex = 1;
            int defaultKarma = character.Options.BuildMethod == "Karma" ? character.Options.BuildPoints : 750;
            check(window.PointsInput.Value == Math.Clamp(defaultKarma, 0, 5000), "Native build-point method switch uses profile or legacy Karma default");
            window.PointsInput.Value = 725; window.AvailabilityInput.Value = 18; window.IgnoreRulesInput.IsChecked = true;
            var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previousTheme = nativeOwner.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    nativeOwner.RequestedThemeVariant = theme; await Task.Delay(100);
                    check(window.ActualThemeVariant == theme && window.PointsInput.Bounds.Width >= 110,
                        "Native build-point selection follows " + (theme == ThemeVariant.Dark ? "dark" : "light") + " owner theme");
                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192));
                    bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-build-points-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { nativeOwner.RequestedThemeVariant = previousTheme; }
            window.Accept();
            check(await pending == Forms.DialogResult.OK && character.BuildMethod == CharacterBuildMethod.Karma && character.BuildKarma == 725 && character.BuildPoints == 0 && character.MaximumAvailability == 18 && character.IgnoreRules && character.NuyenMaximumBP == 100,
                "Native accepted Karma build selection applies limits and money cap once");
        }
        before = character.CaptureSnapshot();
        using (var prompt = new frmSelectBP(character, true))
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (BuildPointWindow)NativeDialogHost.ActiveWindow!;
            check(!window.MethodInput.IsEnabled && window.PointsInput.Value == 725 && window.AvailabilityInput.Value == 18 && window.IgnoreRulesInput.IsChecked == true,
                "Native existing build limits preload Ignore Rules and lock build method");
            window.PointsInput.Value = 500; window.IgnoreRulesInput.IsChecked = false; window.Close();
            check(await pending == Forms.DialogResult.Cancel && character.CaptureSnapshot().SequenceEqual(before),
                "Native canceled build limits preserve the complete character snapshot");
        }
        using (var prompt = new frmSelectBP(character, true))
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (BuildPointWindow)NativeDialogHost.ActiveWindow!;
            window.MethodInput.SelectedIndex = 0; window.PointsInput.Value = 700; window.Accept();
            check(await pending == Forms.DialogResult.OK && character.BuildMethod == CharacterBuildMethod.Karma && character.BuildKarma == 700 && character.IgnoreRules,
                "Native existing build acceptance preserves locked method and enabled Ignore Rules");
        }
        var editing = new Character(); editing.LoadSnapshot(original.CaptureSnapshot()); editing.BuildMethod = CharacterBuildMethod.BP; editing.BuildPoints = 450; editing.IgnoreRules = true;
        using var editor = new frmCreate(editing) { Loading = true, MdiParent = owner }; editor.Show();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        var dirty = editor.GetType().GetField("_blnIsDirty", flags)!; dirty.SetValue(editor, false);
        void Edit(bool accept)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                var window = (BuildPointWindow)NativeDialogHost.ActiveWindow!; window.PointsInput.Value = 500;
                if (accept) window.Accept(); else window.Close();
            });
            editor.GetType().GetMethod("mnuSpecialBPAvailLimit_Click", flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
        }
        try
        {
            Edit(false);
            check(editing.BuildPoints == 450 && editing.IgnoreRules && !(bool)dirty.GetValue(editor)!,
                "Native creation build-limit cancellation preserves values and clean state");
            Edit(true);
            check(editing.BuildPoints == 500 && editing.IgnoreRules && (bool)dirty.GetValue(editor)!,
                "Native creation accepted build limits refresh and mark the character dirty");
        }
        finally { dirty.SetValue(editor, false); editor.Close(); }
    }
}
