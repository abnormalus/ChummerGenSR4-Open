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
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCareerSkillSpecializationChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static void Clean(frmCareer editor) => typeof(frmCareer).GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
    internal static async Task RunAsync(Forms.Form owner, Character source, string output, Action<bool, string> check)
    {
        foreach (bool knowledge in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = true; character.Karma = 100;
            var skill = knowledge ? new Skill(character) { Name = "Synthetic career knowledge", KnowledgeSkill = true,
                SkillCategory = "Academic", Attribute = "LOG", RatingMaximum = 6 }
                : character.Skills.First(s => !s.KnowledgeSkill && !s.IsMeta && !s.ExoticSkill);
            if (knowledge) character.Skills.Add(skill);
            skill.Rating = 2; skill.Specialization = "Synthetic retained specialty"; skill.SkillGroup = ""; skill.IsGrouped = false;
            using var editor = new frmCareer(character) { MdiParent = owner, Loading = true, WindowState = Forms.FormWindowState.Maximized };
            editor.Show(); Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabSkills");
            await Task.Delay(80); editor.Loading = false; Clean(editor);
            var row = Field<Forms.Panel>(editor, knowledge ? "panKnowledgeSkills" : "panActiveSkills").Controls.OfType<SkillControl>()
                .Single(c => ReferenceEquals(c.SkillObject, skill));
            var input = Field<Forms.ComboBox>(row, "cboSpec");

            var theme = Application.Current!.RequestedThemeVariant; string label = " knowledge=" + knowledge;
            try
            {
                string hostName = knowledge ? "NativeCareerKnowledgeSkillRows" : "NativeCareerActiveSkillRows";
                var scroll = (ScrollViewer)owner.Controls.OfType<Forms.NativeControlHost>().Single(host => host.Name == hostName).NativeControl!;
                var nativeRow = ((StackPanel)scroll.Content!).Children.OfType<StackPanel>().Single(view => ReferenceEquals(view.Tag, row));
                var actions = nativeRow.Children.OfType<WrapPanel>().Last();
                var plus = actions.Children.OfType<Button>().Single(control => control.Name == "NativeCareerSpecialization");
                if (knowledge)
                {
                    scroll.MaxWidth = 250;
                    nativeRow.BringIntoView();
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var choices = nativeRow.Children.OfType<ComboBox>().ToArray();
                    check(choices.Length == 2 && choices[1].Text == skill.Name &&
                        choices[0].Bounds.Bottom <= choices[1].Bounds.Top &&
                        choices[1].Bounds.Bottom <= actions.Bounds.Top &&
                        actions.Bounds.Right <= nativeRow.Bounds.Width + 1,
                        "Narrow Career Knowledge Skills stacks its editable type and name above the commands");
                }
                double originalFontSize = plus.FontSize;
                double originalButtonHeight = plus.Bounds.Height;
                double originalMaxWidth = scroll.MaxWidth;
                scroll.MaxWidth = 230;
                Avalonia.Controls.Documents.TextElement.SetFontSize(scroll, originalFontSize * 1.5);
                nativeRow.BringIntoView();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(plus.FontSize >= originalFontSize * 1.5 && plus.Bounds.Height >= originalButtonHeight &&
                    actions.Children.Where(control => control.IsVisible).All(control =>
                        control.Bounds.Right <= actions.Bounds.Width + 1 && control.Bounds.Bottom <= actions.Bounds.Height + 1) &&
                    nativeRow.Bounds.Width <= scroll.Viewport.Width + 1,
                    $"Career skill commands inherit larger text and stay in the narrow viewport [font={originalFontSize}/{plus.FontSize}, row={nativeRow.Bounds}, viewport={scroll.Viewport}]" + label);
                if (knowledge)
                {
                    var pickers = nativeRow.Children.OfType<ComboBox>().ToArray();
                    check(pickers.All(picker => picker.FontSize >= originalFontSize * 1.5) &&
                        pickers[0].Bounds.Bottom <= pickers[1].Bounds.Top && pickers[1].Bounds.Bottom <= actions.Bounds.Top,
                        "Career Knowledge Skills pickers inherit larger text and clear the action row");
                    plus.BringIntoView();
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var position = plus.TranslatePoint(default, scroll);
                    check(position.HasValue && position.Value.Y >= -1 &&
                        position.Value.Y + plus.Bounds.Height <= scroll.Viewport.Height + 1,
                        "At larger text, the last Knowledge Skill action scrolls fully into view");
                }
                var originalCaption = plus.Content;
                plus.Content = string.Join(" ", Enumerable.Repeat("Translated specialization action", 6));
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(plus.Bounds.Right <= actions.Bounds.Width + 1 &&
                    plus.Bounds.Height > originalButtonHeight &&
                    plus.GetVisualDescendants().OfType<TextBlock>().Any(text => Equals(text.Text, plus.Content) &&
                        text.Bounds.Height > plus.FontSize * 2),
                    "Long Career skill commands wrap within the available action row" + label);
                plus.Content = originalCaption;
                scroll.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                scroll.MaxWidth = originalMaxWidth;
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(!input.Visible && skill.Specialization == "Synthetic retained specialty" && plus.IsVisible && plus.IsEnabled,
                    "Native career specialty starts hidden with a usable plus and retained saved text" + label);
                foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    Application.Current.RequestedThemeVariant = variant; await Task.Delay(80);
                    var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
                    using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width * 2),
                        (int)Math.Ceiling(window.ClientSize.Height * 2)), new Vector(192, 192));
                    image.Render(window); image.Save(Path.Combine(output, "native-career-" + (knowledge ? "knowledge" : "active")
                        + "-specialty-collapsed-" + variant.Key.ToString()!.ToLowerInvariant() + ".png"), PngBitmapEncoderOptions.Default);
                    check(!input.Visible && plus.IsVisible && plus.Bounds.Right <= actions.Bounds.Width + 1 &&
                        Field<Forms.Panel>(editor, knowledge ? "panKnowledgeSkills" : "panActiveSkills").Controls.OfType<SkillControl>()
                            .Where(c => !c.SkillIsMeta).All(c => !Field<Forms.ComboBox>(c, "cboSpec").Visible),
                        "Native career skill rows omit specialty boxes until their own plus is clicked in " + variant.Key + label);
                }
                byte[] before = character.CaptureSnapshot(); int karma = character.Karma, expenses = character.ExpenseEntries.Count;
                Task reply = Task.CompletedTask;
                if (knowledge)
                {
                    var response = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); reply = response.Task;
                    Dispatcher.UIThread.Post(async () =>
                    {
                        try
                        {
                            var deadline = DateTime.UtcNow.AddSeconds(20);
                            while (!Forms.Application.OpenForms.OfType<frmSelectText>().Any())
                            { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Career specialty prompt did not open."); await Task.Delay(10); }
                            var dialog = Forms.Application.OpenForms.OfType<frmSelectText>().Last();
                            dialog.DialogResult = Forms.DialogResult.Cancel; response.TrySetResult();
                        }
                        catch (Exception error) { Forms.Application.OpenForms.OfType<frmSelectText>().LastOrDefault()?.Close(); response.TrySetException(error); }
                    });
                }
                plus.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await reply; await Task.Delay(20);
                check(input.Visible && before.SequenceEqual(character.CaptureSnapshot()) && !Field<bool>(editor, "_blnIsDirty") && editor.Enabled,
                    "Native career plus reveals its specialty editor without changing character or Karma" + label);
                check(input.Parent == null,
                    "Native career specialty replaces the revealed compatibility input" + label);
                if (knowledge)
                    check(row.CreationKnowledgeTypeInput.Parent == null && row.CreationKnowledgeNameInput.Parent == null,
                        "Native career knowledge row uses native type and name controls");
                if (!knowledge)
                    actions.Children.OfType<Button>().Single(button => button.Name == "NativeCareerImproveSkill").Focus();
                await Task.Delay(20);
                check(character.Karma == karma && character.ExpenseEntries.Count == expenses && before.SequenceEqual(character.CaptureSnapshot())
                    && !Field<bool>(editor, "_blnIsDirty"), "Native career unchanged specialty focus leave has no expense or saved edit" + label);
                row.RefreshControl();
                check(input.Visible && skill.Specialization == "Synthetic retained specialty",
                    "Native career refresh preserves explicit specialty disclosure and retained text" + label);
            }
            finally { Application.Current.RequestedThemeVariant = theme; Clean(editor); editor.Close(); }
            check(editor.IsDisposed && row.IsDisposed && owner.Enabled, "Native career specialty editor closes without retained controls or modal" + label);
        }
    }
}
