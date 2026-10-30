#nullable enable
using System;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;
using Majorsilence.Forms;

namespace Chummer;

// Exercise actual legacy character labels, rather than inferring readability from the workspace.
internal static class NativeCharacterThemeChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;

    private static double Luminance(System.Drawing.Color color)
    {
        static double Linear(byte channel) { double value = channel / 255d; return value <= .04045 ? value / 12.92 : Math.Pow((value + .055) / 1.055, 2.4); }
        return .2126 * Linear(color.R) + .7152 * Linear(color.G) + .0722 * Linear(color.B);
    }
    internal static async Task RunAsync(frmMain main, Character source, string output, Action<bool, string> check)
    {
        var app = Avalonia.Application.Current!;
        var originalTheme = app.RequestedThemeVariant;
        try
        {
            foreach (var initial in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
            {
                app.RequestedThemeVariant = initial;
                await Task.Delay(100);
                var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
                using var editor = new frmCreate(character) { Loading = true, MdiParent = main };
                editor.Show();
                using var warning = new Forms.Label { ForeColor = System.Drawing.Color.Red,
                    Font = new Majorsilence.Forms.Drawing.Font("Arial", 10, Majorsilence.Forms.Drawing.FontStyle.Italic) };
                using var semantic = new Forms.Label { ForeColor = Forms.SystemColors.ControlText, BackColor = Forms.SystemColors.ButtonFace };
                try
                {
                    foreach (var theme in new[] { initial, initial == Avalonia.Styling.ThemeVariant.Light ? Avalonia.Styling.ThemeVariant.Dark : Avalonia.Styling.ThemeVariant.Light })
                    {
                        app.RequestedThemeVariant = theme; await Task.Delay(120);
                        // No quality is selected in this fixture. Initial and live
                        // theme transitions must not reveal Designer placeholders.
                        check(Field<Forms.Label>(editor, "lblQualityBP").Text == "" &&
                            Field<Forms.Label>(editor, "lblQualitySource").Text == "" &&
                            string.IsNullOrEmpty(Field<Forms.ToolTip>(editor, "tipTooltip").GetToolTip(Field<Forms.Label>(editor, "lblQualitySource"))),
                            "Native unselected quality details remain empty in " + initial.Key + " to " + theme.Key);
                        var nativeWindow = Forms.AvaloniaHostInterop.ToAvaloniaWindow(main);
                        using (var image = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize((int)nativeWindow.ClientSize.Width, (int)nativeWindow.ClientSize.Height)))
                        {
                            image.Render(nativeWindow);
                            image.Save(Path.Combine(output, "native-character-theme-" + initial.Key.ToString()!.ToLowerInvariant() + "-" + theme.Key.ToString()!.ToLowerInvariant() + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                        }
                        var qualityTree = Field<Forms.TreeView>(editor, "treQualities");
                        var qualityNode = new CommonFunctions(character).FindNode(character.Qualities[0].InternalId, qualityTree);
                        qualityTree.SelectedNode = qualityNode;
                        bool populatedDetails = Field<Forms.Label>(editor, "lblQualityBP").Text != "" &&
                            Field<Forms.Label>(editor, "lblQualitySource").Text != "";
                        qualityTree.SelectedNode = null;
                        check(populatedDetails && Field<Forms.Label>(editor, "lblQualityBP").Text == "" &&
                            Field<Forms.Label>(editor, "lblQualitySource").Text == "" &&
                            string.IsNullOrEmpty(Field<Forms.ToolTip>(editor, "tipTooltip").GetToolTip(Field<Forms.Label>(editor, "lblQualitySource"))),
                            "Native cleared quality selection clears populated details in " + initial.Key + " to " + theme.Key);
                        check(warning.ForeColor.ToArgb() == System.Drawing.Color.Red.ToArgb() && warning.Font.Italic,
                            "Native explicit review red and italic survive " + initial.Key + " to " + theme.Key);
                        check(semantic.ForeColor.ToArgb() == Forms.Theme.ForegroundColor.ToDrawingColor().ToArgb() &&
                            semantic.BackColor.ToArgb() == Forms.Theme.ControlMidColor.ToDrawingColor().ToArgb(),
                            "Native explicit system text and ButtonFace follow " + initial.Key + " to " + theme.Key);
                        foreach (string name in new[] { "lblBODLabel", "lblBODAug", "lblBODMetatype", "lblBuildPrimaryAttributes", "lblBuildFoci", "lblAttributesBP", "lblFociBP" })
                        {
                            var label = Field<Forms.Label>(editor, name);
                            double foreground = Luminance(label.ForeColor), background = Luminance(label.BackColor);
                            double contrast = (Math.Max(foreground, background) + .05) / (Math.Min(foreground, background) + .05);
                            File.AppendAllText(Path.Combine(output, "native-character-contrast.txt"), $"{initial.Key}/{theme.Key}/{name}: {label.ForeColor.ToArgb():X8}/{label.BackColor.ToArgb():X8} {contrast:F2}\n");
                            check(contrast >= 4.5, "Native character " + name + " meets text contrast in " + initial.Key + " to " + theme.Key);
                        }
                    }
                }
                finally { editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false); editor.Close(); }
            }
        }
        finally { app.RequestedThemeVariant = originalTheme; }
    }
}
