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
                await StackedFocusCommandChecks.RunAsync(source, check, owner, async (editor, character, career, scenario) =>
                {
                    if (scenario != 3) return;
                    Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
                    Field<Forms.TabControl>(editor, "tabStreetGearTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabGear");
                    var tree = Field<Forms.TreeView>(editor, "treGear"); var item = character.Gear.Single();
                    foreach (Forms.TreeNode root in tree.Nodes) root.Expand();
                    tree.SelectedNode = new CommonFunctions(character).FindNode(item.InternalId, tree);
                    editor.Select();
                    // Let the native host apply tab visibility before validating/capturing.
                    // Legacy label.Visible alone also passes for hidden tab contents.
                    await Task.Delay(100);
                    var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); window.UpdateLayout();
                    var name = Field<Forms.Label>(editor, "lblGearName"); var cost = Field<Forms.Label>(editor, "lblGearCost");
                    var mainTabs = Field<Forms.TabControl>(editor, "tabCharacterTabs");
                    var gearTabs = Field<Forms.TabControl>(editor, "tabStreetGearTabs");
                    var streetPage = Field<Forms.TabPage>(editor, "tabStreetGear");
                    var gearPage = Field<Forms.TabPage>(editor, "tabGear");
                    check(mainTabs.SelectedTab == streetPage && gearTabs.SelectedTab == gearPage &&
                        streetPage.Visible && gearPage.Visible && name.Width > 0 && name.Height > 0 && cost.Width > 0 && cost.Height > 0 &&
                        name.Visible && name.Text.StartsWith("Stacked Focus: Synthetic first focus") && cost.Visible && cost.Text.StartsWith("300"),
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
