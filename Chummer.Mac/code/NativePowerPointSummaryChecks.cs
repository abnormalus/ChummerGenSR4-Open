#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

// Show both actual editors on the native dispatcher. This supplements the fixed
// headless baseline; checking a pure calculator alone cannot exercise tab ownership.
internal static class NativePowerPointSummaryChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static async Task RunAsync(frmMain main, Character source, Action<bool, string> check)
    {
        var culture = CultureInfo.CurrentCulture;
        var app = Avalonia.Application.Current!;
        var originalTheme = app.RequestedThemeVariant;
        try
        {
            foreach (bool career in new[] { false, true })
            foreach (string cultureName in new[] { "en-US", "de-DE" })
            {
                CultureInfo.CurrentCulture = CultureInfo.GetCultureInfo(cultureName);
                var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
                character.Created = career; character.Powers.Clear(); character.Improvements.Clear();
                character.AdeptEnabled = true; character.MagicianEnabled = true; character.MAGAdept = 3;
                using Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
                editor.MdiParent = main; editor.Show();
                try
                {
                    var panel = Field<Panel>(editor, "panPowers");
                    foreach (Control row in panel.Controls.Cast<Control>().ToArray()) row.Dispose();
                    panel.Controls.Clear();
                    var rowControl = new PowerControl();
                    typeof(PowerControl).GetField("_objPower", Flags)!.SetValue(rowControl,
                        new Power(character) { PointsPerLevel = 4m, Rating = 1 });
                    panel.Controls.Add(rowControl);
                    Field<TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<TabPage>(editor, "tabAdept");
                    foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
                    {
                        app.RequestedThemeVariant = theme;
                        await Task.Delay(100);
                        // Show/Load reconciles split Magic against the source fixture.
                        // Establish this synthetic budget after native initialization.
                        character.MAGAdept = 3;
                        editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
                        byte[] before = character.CaptureSnapshot();
                        editor.GetType().GetMethod("CalculatePowerPoints", Flags)!.Invoke(editor, null);
                        var label = Field<Label>(editor, "lblPowerPoints");
                        string expected = "3 (" + (cultureName == "de-DE" ? "-1,0" : "-1.0") + " " + LanguageManager.Instance.GetString("String_Remaining") + ")";
                        string context = " career=" + career + " culture=" + cultureName + " theme=" + theme.Key;
                        check(label.Visible && label.Text == expected,
                            "Native shown adept tab retains exact overspent Power Point summary" + context + " [visible=" + label.Visible + "; actual=" + label.Text + "; expected=" + expected + "]");
                        check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()) && !(bool)editor.GetType().GetField("_blnIsDirty", Flags)!.GetValue(editor)!,
                            "Native Power Point refresh preserves character and clean editor" + context);
                    }
                }
                finally { editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close(); }
            }
        }
        finally { CultureInfo.CurrentCulture = culture; app.RequestedThemeVariant = originalTheme; }
    }
}
