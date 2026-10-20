#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCreationSkillGroupChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Character source, string output, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.Created = false; character.IgnoreRules = true; character.BuildMethod = CharacterBuildMethod.BP;
        foreach (var group in character.SkillGroups) { group.Rating = 0; group.Broken = false; }
        using var editor = new frmCreate(character) { MdiParent = owner, Loading = true };
        editor.Show(); await Task.Delay(30);
        var tabs = Field<Forms.TabControl>(editor, "tabCharacterTabs");
        var skills = Field<Forms.TabPage>(editor, "tabSkills");
        try
        {
            check(typeof(frmCreate).GetField("creationSkillGroupSection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor) == null,
                "Native creation group viewport preserves first-Skills-show initialization timing");
            byte[] beforeShowing = character.CaptureSnapshot();
            bool beforeDirty = Field<bool>(editor, "_blnIsDirty");
            tabs.SelectedTab = skills; await Task.Delay(80);
            var section = Field<CreationSkillGroupSection>(editor, "creationSkillGroupSection");
            var groups = Field<List<SkillGroupControl>>(editor, "creationSkillGroups");
            check(section.Inputs.Count == groups.Count && groups.Count == character.SkillGroups.Count && groups.All(group => group.Parent == null) &&
                section.Inputs.Values.All(input => input.GetType() == typeof(NumericUpDown)),
                "Native creation group viewport exposes standard inputs with explicitly owned detached controllers");
            check(section.Inputs.Values.Select(input => input.Name).Distinct().Count() == groups.Count &&
                groups.All(group => Avalonia.Automation.AutomationProperties.GetName(section.Inputs[group]) == group.CreationNameInput.Text),
                "Native creation group inputs have unique identities and localized accessible group labels");
            check(character.CaptureSnapshot().SequenceEqual(beforeShowing) && Field<bool>(editor, "_blnIsDirty") == beforeDirty,
                "Opening native creation groups preserves the existing character and clean state");
            var originalTheme = Application.Current!.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    Application.Current.RequestedThemeVariant = theme; await Task.Delay(80);
                    var last = section.Inputs.Values.Last();
                    ControlExtensions.BringIntoView(last); await Task.Delay(30);
                    var point = last.TranslatePoint(default, section.Viewport);
                    check(last.IsEffectivelyVisible && point.HasValue && point.Value.Y >= -1 &&
                        point.Value.Y + last.Bounds.Height <= section.Viewport.Viewport.Height + 1 &&
                        last.GetVisualDescendants().OfType<TextBox>().Any(text => text.Bounds.Width >= 24),
                        "Native creation group list exposes its final readable row in " + theme.Key);
                    section.Viewport.Offset = default; await Task.Delay(30);
                    var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
                    using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width * 2),
                        (int)Math.Ceiling(window.ClientSize.Height * 2)), new Vector(192, 192));
                    image.Render(window);
                    image.Save(Path.Combine(output, "native-creation-skill-groups-" + theme.Key.ToString()!.ToLowerInvariant() + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { Application.Current.RequestedThemeVariant = originalTheme; }
            var controller = groups.Single(group => group.GroupName == "Athletics");
            var input = section.Inputs[controller];
            var members = character.Skills.Where(skill => skill.SkillGroup == controller.GroupName).ToArray();
            ControlExtensions.BringIntoView(input); input.Text = "2"; await Task.Delay(30);
            check(members.Length > 0 && controller.GroupRating == 2 && members.All(skill => skill.Rating == 2 && skill.IsGrouped) && Field<bool>(editor, "_blnIsDirty"),
                "Native creation group typing applies existing canonical member grouping and dirty-state rules");
            int groupCost = groups.Sum(group => group.GroupRating * character.Options.BPSkillGroup);
            check(Field<Forms.Label>(editor, "lblSkillGroupsBP").Text.StartsWith(groupCost.ToString() + " ", StringComparison.Ordinal),
                "Native creation group edit retains existing BP summary accounting");
            var textEditor = input.GetVisualDescendants().OfType<TextBox>().Single();
            textEditor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up }); await Task.Delay(20);
            check(controller.GroupRating == 3 && members.All(skill => skill.Rating == 3),
                "Native creation group keyboard stepping updates member skills through the owner");
            byte[] approved = character.CaptureSnapshot(); input.Value = 2.5m;
            check(input.Value == 3 && character.CaptureSnapshot().SequenceEqual(approved),
                "Native creation group fractional edit preserves the full character snapshot");
            input.Value = null;
            check(controller.GroupRating == 3 && character.CaptureSnapshot().SequenceEqual(approved),
                "Native creation group incomplete edit does not commit a fallback rating");
            textEditor.Focus(); section.Inputs.Values.First(other => other != input).Focus(); await Task.Delay(20);
            check(input.Value == 3, "Native creation group leaving an incomplete edit restores its approved rating");
            input.Value = 0;
            check(members.All(skill => skill.Rating == 0 && !skill.IsGrouped),
                "Native creation zero group rating releases member skills through existing rules");
            controller.IsEnabled = false; input.Value = 1;
            check(!input.IsEnabled && input.Value == 0 && controller.GroupRating == 0,
                "Native creation disabled group rejects queued edits");
            controller.IsEnabled = true; input.Value = 1;
            check(input.IsEnabled && controller.GroupRating == 1, "Native creation group re-enabling permits a valid edit");
            character.BuildMethod = CharacterBuildMethod.Karma; input.Value = 2;
            int karma = groups.Where(group => group.GroupRating > 0).Sum(group => character.Options.KarmaNewSkillGroup +
                Enumerable.Range(2, Math.Max(0, group.GroupRating - 1)).Sum(rating => rating * character.Options.KarmaImproveSkillGroup));
            check(Field<Forms.Label>(editor, "lblSkillGroupsBP").Text.StartsWith(karma.ToString() + " ", StringComparison.Ordinal),
                "Native creation group edit retains incremental Karma summary accounting");
            var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
            check(reopened.SkillGroups.Single(group => group.Name == controller.GroupName).Rating == 2 &&
                reopened.Skills.Where(skill => skill.SkillGroup == controller.GroupName).All(skill => skill.Rating == 2 && skill.IsGrouped),
                "Native creation group changes retain unchanged legacy character serialization");
            tabs.SelectedTab = Field<Forms.TabPage>(editor, "tabCommon"); await Task.Delay(20);
            bool hidden = !input.IsEffectivelyVisible;
            tabs.SelectedTab = skills; await Task.Delay(20);
            check(hidden && input.IsEffectivelyVisible, "Native creation group viewport hides and restores with Skills");
            using var prompt = new frmSelectNumber { Minimum = 0, Maximum = 10, Description = "Synthetic group ownership check" };
            var pending = prompt.ShowDialogAsync(editor); await Task.Delay(20);
            bool blocked = !input.IsEffectivelyEnabled;
            NativeDialogHost.ActiveWindow!.Close(); await pending;
            check(blocked && input.IsEffectivelyEnabled, "Native creation group viewport respects and releases modal owner blocking");
        }
        finally
        {
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
            editor.Close();
        }
        var closed = Field<CreationSkillGroupSection>(editor, "creationSkillGroupSection");
        var held = closed.Inputs.First(); int previous = held.Key.GroupRating;
        held.Value.Value = previous == 1 ? 2 : 1;
        check(held.Key.GroupRating == previous, "Closed native group viewport rejects queued input callbacks");
    }
}
