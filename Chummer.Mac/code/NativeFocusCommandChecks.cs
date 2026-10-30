#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Run the fixed command expectations on the native dispatcher in both themes.
// Native projections/captures supplement model assertions rather than replacing them.
internal static class NativeFocusCommandChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Character source, string output, Action<bool, string> check)
    {
        var app = Avalonia.Application.Current!; var originalTheme = app.RequestedThemeVariant;
        try
        {
            foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
            {
                app.RequestedThemeVariant = theme; await Task.Delay(50);
                StackedFocusCommandChecks.Run(source, check, owner, (editor, character, career, scenario) =>
                {
                    if (scenario != 3) return;
                    Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
                    Field<Forms.TabControl>(editor, "tabStreetGearTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabGear");
                    var tree = Field<Forms.TreeView>(editor, "treGear"); var item = character.Gear.Single();
                    foreach (Forms.TreeNode root in tree.Nodes) root.Expand();
                    tree.SelectedNode = new CommonFunctions(character).FindNode(item.InternalId, tree);
                    editor.Select();
                    var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); window.UpdateLayout();
                    var name = Field<Forms.Label>(editor, "lblGearName"); var cost = Field<Forms.Label>(editor, "lblGearCost");
                    check(name.Visible && name.Text.StartsWith("Stacked Focus: Synthetic first focus") && cost.Visible && cost.Text.StartsWith("300"),
                        "Native stacked-focus selected gear shows fixed name and cost career=" + career + " theme=" + theme.Key);
                    using var image = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height));
                    image.Render(window);
                    image.Save(Path.Combine(output, "native-focus-" + (career ? "career-" : "creation-") + theme.Key.ToString()!.ToLowerInvariant() + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                });
            }
        }
        finally { app.RequestedThemeVariant = originalTheme; }
    }
}
