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

// Uses a synthetic character in the actual MDI editor. Headless controller
// checks remain separate; these assertions require a native window and layout.
internal static class NativeCreationActiveSkillChecks
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
        CreationActiveSkillSection? section = null;
        try
        {
            check(typeof(frmCreate).GetField("creationActiveSkillSection", BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(editor) == null,
                "Native active skills preserve deferred first-Skills-show initialization");
            byte[] before = character.CaptureSnapshot(); bool dirty = Field<bool>(editor, "_blnIsDirty");
            tabs.SelectedTab = skills; await Task.Delay(80);
            section = Field<CreationActiveSkillSection>(editor, "creationActiveSkillSection");
            var controllers = Field<List<SkillControl>>(editor, "creationActiveSkills");
            check(section.Rows.Keys.SequenceEqual(controllers) && controllers.All(row => row.Parent == null)
                && section.Rows.Values.All(row => row.Rating.GetType() == typeof(NumericUpDown)
                    && row.Specialization.GetType() == typeof(AutoCompleteBox)),
                "Native active viewport retains ordered controller identities with standard rating and suggestion controls");
            check(character.CaptureSnapshot().SequenceEqual(before) && Field<bool>(editor, "_blnIsDirty") == dirty,
                "Opening native active skills preserves the character and dirty state");
            check(section.Rows.Values.Select(row => row.Rating.Name).Distinct().Count() == controllers.Count
                && section.Rows.Values.Select(row => row.Specialization.Name).Distinct().Count() == controllers.Count
                && controllers.All(row => Avalonia.Automation.AutomationProperties.GetName(section.Rows[row].Rating) == row.CreationNameLabel.Text),
                "Native active skill fields have unique identities and localized accessible names");
            // Keep a trained row visible in the synthetic theme captures so
            // source-driven bold text and themed row emphasis can be reviewed.
            controllers.First(controller => !controller.SkillIsMeta && controller.CreationRatingInput.Visible).SkillRating = 1;
            var originalTheme = Application.Current!.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    Application.Current.RequestedThemeVariant = theme; await Task.Delay(80);
                    var last = section.Rows.Values.Last(row => row.Layout.IsVisible && row.Rating.IsVisible);
                    ControlExtensions.BringIntoView(last.Layout); await Task.Delay(30);
                    var point = last.Layout.TranslatePoint(default, section.Viewport);
                    check(last.Layout.IsEffectivelyVisible && point.HasValue && point.Value.Y >= -1
                        && point.Value.Y + last.Layout.Bounds.Height <= section.Viewport.Viewport.Height + 1
                        && section.Viewport.Offset.Y > 0 && last.Rating.GetVisualDescendants().OfType<TextBox>().Any(text => text.Bounds.Width >= 24),
                        "Native active skill scrolling exposes its final readable editable row in " + theme.Key);
                    section.Viewport.Offset = default; await Task.Delay(30);
                    var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
                    using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width * 2),
                        (int)Math.Ceiling(window.ClientSize.Height * 2)), new Vector(192, 192));
                    image.Render(window);
                    image.Save(Path.Combine(output, "native-creation-active-skills-" + theme.Key.ToString()!.ToLowerInvariant() + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { Application.Current.RequestedThemeVariant = originalTheme; }
            editor.Loading = true;
            foreach (var controller in controllers.Where(row => !row.SkillIsMeta)) controller.SkillRating = 0;
            editor.Loading = false;
            var pistols = controllers.Single(row => row.SkillName == "Pistols" && !row.SkillIsMeta && !row.SkillObject.ExoticSkill);
            var row = section.Rows[pistols];
            ControlExtensions.BringIntoView(row.Layout); await Task.Delay(30);
            row.Rating.Text = "2"; await Task.Delay(20);
            check(pistols.SkillRating == 2 && Field<bool>(editor, "_blnIsDirty"),
                "Native active skill typing updates the real owner and dirty state");
            var textEditor = row.Rating.GetVisualDescendants().OfType<TextBox>().Single();
            textEditor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up }); await Task.Delay(20);
            check(pistols.SkillRating == 3, "Native active skill keyboard stepping follows the controller");
            byte[] approved = character.CaptureSnapshot(); row.Rating.Value = 2.5m;
            check(row.Rating.Value == 3 && character.CaptureSnapshot().SequenceEqual(approved),
                "Native active fractional rating restores the full approved character snapshot");
            row.Rating.Value = null;
            check(character.CaptureSnapshot().SequenceEqual(approved), "Native active incomplete rating does not commit a fallback");
            textEditor.Focus(); row.Specialization.GetVisualDescendants().OfType<TextBox>().Single().Focus(); await Task.Delay(20);
            check(row.Rating.Value == 3, "Native active skill leaving incomplete input restores its approved rating");
            string literal = "Synthetic [literal] O'Brien";
            row.Specialization.Text = literal;
            check(pistols.SkillObject.Specialization == literal, "Native active editable suggestions retain literal custom specialization text");
            var choice = pistols.CreationSpecializationInput.Items.Cast<object>().FirstOrDefault();
            check(choice != null && row.Specialization.ItemsSource!.Cast<object>().Any(item => ReferenceEquals(item, choice)),
                "Native active suggestions preserve canonical source choice objects");
            row.Specialization.SelectedItem = choice; await Task.Delay(20);
            check(ReferenceEquals(pistols.CreationSpecializationInput.SelectedItem, choice)
                && pistols.SkillObject.Specialization == pistols.CreationSpecializationInput.Text,
                "Native active suggestion selection uses the existing source text and model transition");
            row.Specialization.Text = literal;
            var filter = Field<Forms.ComboBox>(editor, "cboSkillFilter");
            filter.SelectedValue = "3"; await Task.Delay(20);
            approved = character.CaptureSnapshot(); dirty = Field<bool>(editor, "_blnIsDirty");
            row.Specialization.Text = "Queued filtered edit"; row.Rating.Value = 1;
            check(!row.Layout.IsEffectivelyVisible && character.CaptureSnapshot().SequenceEqual(approved)
                && Field<bool>(editor, "_blnIsDirty") == dirty && row.Specialization.Text == literal && row.Rating.Value == 3,
                "Native active zero-rating filter hides trained rows and rejects queued edits without changing the character");
            filter.SelectedValue = "0"; await Task.Delay(20);
            ControlExtensions.BringIntoView(row.Layout); await Task.Delay(20);
            check(row.Layout.IsEffectivelyVisible && row.Specialization.Text == literal,
                "Native active All filter restores the same row and custom specialization");
            var groups = Field<List<SkillGroupControl>>(editor, "creationSkillGroups");
            var firearms = groups.Single(group => group.GroupName == "Firearms"); firearms.GroupRating = 1;
            approved = character.CaptureSnapshot(); row.Specialization.Text = "Queued grouped edit"; row.Rating.Value = 2;
            check(pistols.IsGrouped && !row.Rating.IsEnabled && !row.Specialization.IsEnabled
                && character.CaptureSnapshot().SequenceEqual(approved),
                "Native active group membership disables both fields and rejects queued edits");
            firearms.GroupRating = 0; row.Rating.Value = 1; row.Specialization.Text = literal;
            var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
            check(reopened.Skills.Single(skill => skill.Name == "Pistols" && !skill.IsMeta && !skill.ExoticSkill).Specialization == literal
                && reopened.Skills.Single(skill => skill.Name == "Pistols" && !skill.IsMeta && !skill.ExoticSkill).Rating == 1,
                "Native active skill edits retain the unchanged legacy serialization contract");
            tabs.SelectedTab = Field<Forms.TabPage>(editor, "tabCommon"); await Task.Delay(20);
            bool hidden = !row.Rating.IsEffectivelyVisible; tabs.SelectedTab = skills; await Task.Delay(20);
            ControlExtensions.BringIntoView(row.Layout); await Task.Delay(20);
            check(hidden && row.Rating.IsEffectivelyVisible, "Native active viewport hides and restores with its Skills tab");
            async Task<string> PickExoticAsync(bool accept)
            {
                var reply = new TaskCompletionSource<string>(TaskCreationOptions.RunContinuationsAsynchronously);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        var deadline = DateTime.UtcNow.AddSeconds(20);
                        while (!Forms.Application.OpenForms.OfType<frmSelectExoticSkill>().Any())
                        { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Exotic skill picker did not open."); await Task.Delay(10); }
                        var picker = Forms.Application.OpenForms.OfType<frmSelectExoticSkill>().Last();
                        var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(picker);
                        while (!window.IsVisible || window.Bounds.Width <= 0)
                        { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Exotic skill picker did not become visible."); await Task.Delay(10); }
                        await Task.Delay(30);
                        string selected = picker.SelectedExoticSkill;
                        if (accept) Field<Forms.Button>(picker, "cmdOK").PerformClick(); else picker.Close();
                        reply.TrySetResult(selected);
                    }
                    catch (Exception error) { NativeDialogHost.ActiveWindow?.Close(); reply.TrySetException(error); }
                });
                Field<Forms.Button>(editor, "cmdAddExoticSkill").PerformClick();
                return await reply.Task;
            }
            approved = character.CaptureSnapshot(); dirty = Field<bool>(editor, "_blnIsDirty");
            int previousRows = controllers.Count;
            await PickExoticAsync(false);
            check(character.CaptureSnapshot().SequenceEqual(approved) && controllers.Count == previousRows
                && section.Rows.Count == previousRows && Field<bool>(editor, "_blnIsDirty") == dirty,
                "Native exotic-skill picker cancellation preserves the character and owned viewport rows");
            string selectedExotic = await PickExoticAsync(true); await Task.Delay(40);
            var exotic = controllers.Last(); var exoticRow = section.Rows[exotic];
            check(controllers.Count == previousRows + 1 && section.Rows.Count == controllers.Count && exotic.SkillObject.ExoticSkill
                && exotic.SkillName == selectedExotic && ReferenceEquals(character.Skills.Last(), exotic.SkillObject)
                && exotic.Parent == null && exoticRow.Layout.IsEffectivelyVisible && Field<bool>(editor, "_blnIsDirty"),
                "Native accepted exotic skill joins the existing viewport with the exact model and controller identities");
            exoticRow.Rating.Value = 1; exoticRow.Specialization.Text = "Synthetic exotic specialization";
            check(exotic.SkillRating == 1 && exotic.SkillObject.Specialization == "Synthetic exotic specialization",
                "Native dynamically appended exotic skill routes rating and custom text through its original owner handlers");
            using var prompt = new frmSelectNumber { Minimum = 0, Maximum = 10, Description = "Synthetic active skill ownership check" };
            var pending = prompt.ShowDialogAsync(editor); await Task.Delay(20);
            bool blocked = !row.Rating.IsEffectivelyEnabled && !row.Specialization.IsEffectivelyEnabled;
            NativeDialogHost.ActiveWindow!.Close(); await pending;
            check(blocked && row.Rating.IsEffectivelyEnabled && row.Specialization.IsEffectivelyEnabled,
                "Native active fields respect modal owner blocking and recover after cancellation");
        }
        finally
        {
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
            editor.Close();
        }
        if (section != null)
        {
            byte[] closed = character.CaptureSnapshot(); var held = section.Rows.Values.First();
            held.Rating.Value = 2; held.Specialization.Text = "Queued closed edit";
            check(character.CaptureSnapshot().SequenceEqual(closed), "Closed native active viewport disconnects queued field callbacks");
        }
    }
}
