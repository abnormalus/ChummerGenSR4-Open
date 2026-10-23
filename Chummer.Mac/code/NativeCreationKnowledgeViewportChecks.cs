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
using Avalonia.Interactivity;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeCreationKnowledgeViewportChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static void Click(Button button) => button.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    internal static async Task RunAsync(Forms.Form owner, Character source, string output, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = false; character.IgnoreRules = true;
        foreach (var skill in character.Skills.Where(skill => skill.KnowledgeSkill).ToArray()) character.Skills.Remove(skill);
        for (int index = 0; index < 12; index++) character.Skills.Add(new Skill(character) { KnowledgeSkill = true,
            SkillCategory = "Academic", Name = "Synthetic knowledge " + index, Rating = index == 0 ? 1 : 0, RatingMaximum = 6 });
        using var editor = new frmCreate(character) { MdiParent = owner, Loading = true, WindowState = Forms.FormWindowState.Maximized };
        editor.Show(); await Task.Delay(30);
        CreationKnowledgeSkillRow? held = null;
        try
        {
            check(typeof(frmCreate).GetField("creationKnowledgeSkillSection", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(editor) == null,
                "Native knowledge viewport waits for the first Skills show");
            var before = character.CaptureSnapshot(); bool dirtyBefore = Field<bool>(editor, "_blnIsDirty");
            var tabs = Field<Forms.TabControl>(editor, "tabCharacterTabs"); var skills = Field<Forms.TabPage>(editor, "tabSkills");
            tabs.SelectedTab = skills; await Task.Delay(80);
            var section = Field<CreationKnowledgeSkillSection>(editor, "creationKnowledgeSkillSection");
            var controllers = Field<List<SkillControl>>(editor, "creationKnowledgeSkills");
            check(controllers.Count == 12 && section.Rows.Keys.SequenceEqual(controllers) && controllers.All(row => row.Parent == null),
                "Native knowledge viewport preserves all ordered canonical detached controllers");
            check(character.CaptureSnapshot().SequenceEqual(before) && Field<bool>(editor, "_blnIsDirty") == dirtyBefore,
                "Opening the native knowledge viewport preserves snapshot and dirty state");
            check(section.Rows.Values.All(row => row.Type.GetType() == typeof(ComboBox) && row.Name.GetType() == typeof(AutoCompleteBox)
                && row.Rating.GetType() == typeof(NumericUpDown) && row.Specialization.GetType() == typeof(AutoCompleteBox))
                && section.Rows.Values.Select(row => row.Name.Name).Distinct().Count() == 12,
                "Native knowledge rows expose standard controls and unique identities");
            check(section.Rows.Values.All(row => !row.SpecializationDisclosure.Expanded &&
                row.SpecializationDisclosure.Toggle.VerticalContentAlignment == Avalonia.Layout.VerticalAlignment.Center),
                "Native knowledge specialization lines start collapsed for every row");
            foreach (var disclosureRow in new[] { section.Rows.Values.First(), section.Rows.Values.Last() })
                disclosureRow.SpecializationDisclosure.Toggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            await Task.Delay(30);
            check(section.Rows.Values.First().Specialization.IsEffectivelyVisible && section.Rows.Values.Last().Specialization.IsEffectivelyVisible &&
                character.CaptureSnapshot().SequenceEqual(before) && Field<bool>(editor, "_blnIsDirty") == dirtyBefore,
                "Native knowledge plus opens independent specialization rows without editing character");
            var originalTheme = Application.Current!.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    Application.Current.RequestedThemeVariant = theme; await Task.Delay(80);
                    var last = section.Rows.Values.Last(); ControlExtensions.BringIntoView(last.Layout); await Task.Delay(30);
                    var point = last.Layout.TranslatePoint(default, section.Viewport);
                    // Retain the exact final-row frame even if strict fit fails.
                    var diagnosticWindow = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
                    using (var diagnostic = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(diagnosticWindow.ClientSize.Width * 2),
                        (int)Math.Ceiling(diagnosticWindow.ClientSize.Height * 2)), new Vector(192, 192)))
                    {
                        diagnostic.Render(diagnosticWindow);
                        diagnostic.Save(Path.Combine(output, "native-creation-knowledge-skills-" + theme.Key.ToString()!.ToLowerInvariant() + ".png"), PngBitmapEncoderOptions.Default);
                    }
                    check(last.Layout.IsEffectivelyVisible && point.HasValue && point.Value.Y >= -1 &&
                        point.Value.Y + last.Layout.Bounds.Height <= section.Viewport.Viewport.Height + 1 &&
                        last.Layout.Bounds.Width <= section.Viewport.Viewport.Width + 1 &&
                        new Control[] { last.Type, last.Name, last.Rating, last.Specialization, last.Delete, last.AddSpecialty }.All(control =>
                            control.TranslatePoint(default, last.Layout) is { } p && p.X >= -1 && p.X + control.Bounds.Width <= last.Layout.Bounds.Width + 1 && control.Bounds.Height >= 20),
                        "Native knowledge scrolling exposes the final complete row in " + theme.Key +
                        FormattableString.Invariant($" [row={last.Layout.Bounds}; point={point}; viewport={section.Viewport.Viewport}; extent={section.Viewport.Extent}; offset={section.Viewport.Offset}; fields={string.Join("|", new Control[] { last.Type, last.Name, last.Rating, last.Specialization, last.Delete, last.AddSpecialty }.Select(control => control.GetType().Name + ":" + control.Bounds + ":" + control.TranslatePoint(default, last.Layout)))}]"));
                    section.Viewport.Offset = default; await Task.Delay(30);
                    var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
                    using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width * 2), (int)Math.Ceiling(window.ClientSize.Height * 2)), new Vector(192, 192));
                    image.Render(window); image.Save(Path.Combine(output, "native-creation-knowledge-skills-" + theme.Key.ToString()!.ToLowerInvariant() + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { Application.Current.RequestedThemeVariant = originalTheme; }
            var nativeWindow = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
            var previousState = nativeWindow.WindowState; var previousSize = owner.Size;
            var beforeWide = character.CaptureSnapshot(); bool dirtyBeforeWide = Field<bool>(editor, "_blnIsDirty");
            var screen = nativeWindow.Screens.ScreenFromWindow(nativeWindow);
            Size expectedWide = screen != null ? screen.Bounds.Size.ToSize(screen.Scaling) : default;
            var geometryTrace = new List<string>();
            void TraceGeometry(string reason)
            {
                if (geometryTrace.Count < 32)
                    geometryTrace.Add(FormattableString.Invariant($"{reason}:state={nativeWindow.WindowState}:client={nativeWindow.ClientSize}:requested={nativeWindow.Width}x{nativeWindow.Height}:frame={nativeWindow.FrameSize}:position={nativeWindow.Position}:active={nativeWindow.IsActive}"));
            }
            EventHandler<WindowResizedEventArgs> traceResize = (_, _) => TraceGeometry("resize");
            EventHandler<AvaloniaPropertyChangedEventArgs> traceProperty = (_, change) =>
            {
                if (change.Property == Window.WidthProperty || change.Property == Window.HeightProperty ||
                    change.Property == Window.WindowStateProperty || change.Property == TopLevel.ClientSizeProperty)
                    TraceGeometry(change.Property.Name);
            };
            nativeWindow.Resized += traceResize;
            nativeWindow.PropertyChanged += traceProperty;
            TraceGeometry("before-fullscreen");
            try
            {
                nativeWindow.WindowState = WindowState.FullScreen;
                TraceGeometry("requested-fullscreen");
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    Application.Current.RequestedThemeVariant = theme;
                    var deadline = DateTime.UtcNow.AddSeconds(20); DateTime? matchedSince = null; bool ready = false;
                    while (DateTime.UtcNow < deadline)
                    {
                        bool matches = nativeWindow.WindowState == WindowState.FullScreen &&
                            Math.Abs(nativeWindow.ClientSize.Width - expectedWide.Width) <= 1 && Math.Abs(nativeWindow.ClientSize.Height - expectedWide.Height) <= 1;
                        if (matches)
                        {
                            matchedSince ??= DateTime.UtcNow;
                            if (DateTime.UtcNow - matchedSince.Value >= TimeSpan.FromMilliseconds(120)) { ready = true; break; }
                        }
                        else matchedSince = null;
                        await Task.Delay(10);
                    }
                    var last = section.Rows.Values.Last(); ControlExtensions.BringIntoView(last.Layout); await Task.Delay(30);
                    var point = last.Layout.TranslatePoint(default, section.Viewport);
                    bool complete = last.Layout.IsEffectivelyVisible && point.HasValue && point.Value.Y >= -1 &&
                        point.Value.Y + last.Layout.Bounds.Height <= section.Viewport.Viewport.Height + 1 &&
                        new Control[] { last.Type, last.Name, last.Rating, last.Specialization, last.Delete, last.AddSpecialty }.All(control =>
                            control.TranslatePoint(default, last.Layout) is { } p && p.X >= -1 && p.X + control.Bounds.Width <= last.Layout.Bounds.Width + 1 && control.Bounds.Height >= 20);
                    using (var frame = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(nativeWindow.ClientSize.Width * 2),
                        (int)Math.Ceiling(nativeWindow.ClientSize.Height * 2)), new Vector(192, 192)))
                    {
                        frame.Render(nativeWindow);
                        frame.Save(Path.Combine(output, "native-creation-knowledge-fullscreen-" + theme.Key.ToString()!.ToLowerInvariant() + ".png"), PngBitmapEncoderOptions.Default);
                    }
                    check(screen != null && ready && nativeWindow.WindowState == WindowState.FullScreen &&
                        Math.Abs(nativeWindow.ClientSize.Width - expectedWide.Width) <= 1 && Math.Abs(nativeWindow.ClientSize.Height - expectedWide.Height) <= 1 && complete,
                        "Native fullscreen knowledge exposes its complete final row at actual screen geometry in " + theme.Key +
                        FormattableString.Invariant($" [expected={expectedWide}; actual={nativeWindow.ClientSize}; state={nativeWindow.WindowState}; screen={screen?.Bounds}; workarea={screen?.WorkingArea}; scaling={screen?.Scaling}; requested={nativeWindow.Width}x{nativeWindow.Height}; frame={nativeWindow.FrameSize}; min={nativeWindow.MinWidth}x{nativeWindow.MinHeight}; max={nativeWindow.MaxWidth}x{nativeWindow.MaxHeight}; sizing={nativeWindow.SizeToContent}; owner={owner.Size}; complete={complete}; row={last.Layout.Bounds}; point={point}; viewport={section.Viewport.Viewport}; trace={string.Join("|", geometryTrace)}]"));
                }
            }
            finally
            {
                nativeWindow.Resized -= traceResize;
                nativeWindow.PropertyChanged -= traceProperty;
                Application.Current.RequestedThemeVariant = originalTheme;
                nativeWindow.WindowState = WindowState.Normal; owner.Size = previousSize; nativeWindow.UpdateLayout();
                nativeWindow.WindowState = previousState; await Task.Delay(120);
            }
            check(character.CaptureSnapshot().SequenceEqual(beforeWide) && Field<bool>(editor, "_blnIsDirty") == dirtyBeforeWide,
                "Native knowledge fullscreen themes and restoration preserve snapshot and dirty state");
            editor.Loading = false;
            var controller = controllers.First(); var row = section.Rows[controller]; held = row;
            ControlExtensions.BringIntoView(row.Layout); await Task.Delay(20);
            var choice = controller.CreationKnowledgeTypeInput.Items.Cast<ListItem>().Single(item => item.Value == "Street");
            row.Type.SelectedItem = choice;
            check(ReferenceEquals(controller.CreationKnowledgeTypeInput.SelectedItem, choice) && controller.SkillCategory == "Street" && controller.SkillObject.Attribute == "INT",
                "Native knowledge type selection preserves canonical choices and linked-attribute rules");
            var suggested = controller.CreationKnowledgeNameInput.Items.Cast<object>().FirstOrDefault();
            check(suggested != null && row.Name.ItemsSource!.Cast<object>().Any(item => ReferenceEquals(item, suggested)),
                "Native knowledge suggestions retain source category choice objects");
            typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
            row.Name.SelectedItem = suggested; await Task.Delay(20);
            check(ReferenceEquals(controller.CreationKnowledgeNameInput.SelectedItem, suggested) && controller.SkillObject.Name == controller.CreationKnowledgeNameInput.Text && Field<bool>(editor, "_blnIsDirty"),
                "Native knowledge catalog selection commits through its source choice and marks the editor dirty");
            row.Name.Focus(); row.Name.Text = "Synthetic literal [O'Brien]"; await Task.Delay(20);
            check(controller.SkillObject.Name == row.Name.Text && controller.SkillCategory == "Street" && Field<bool>(editor, "_blnIsDirty"),
                "Native knowledge custom name preserves literal text selected type and dirty state");
            row.Rating.Text = "2"; await Task.Delay(20);
            check(controller.SkillRating == 2, "Native knowledge rating text commits through the original controller");
            var input = row.Rating.GetVisualDescendants().OfType<TextBox>().Single();
            input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up }); await Task.Delay(20);
            check(controller.SkillRating == 3, "Native knowledge keyboard stepping uses the existing rating transition");
            var approved = character.CaptureSnapshot(); row.Rating.Value = 2.5m;
            check(row.Rating.Value == 3 && character.CaptureSnapshot().SequenceEqual(approved), "Native knowledge fractional rating preserves the character");
            row.Rating.Value = null; input.Focus(); row.Name.Focus(); await Task.Delay(20);
            check(row.Rating.Value == 3 && character.CaptureSnapshot().SequenceEqual(approved), "Native knowledge incomplete rating restores on focus change");
            row.Specialization.Text = "Synthetic specialty [O'Brien]";
            check(controller.SkillObject.Specialization == row.Specialization.Text, "Native knowledge specialization preserves literal custom text");
            var beforeSpecializationCollapse = character.CaptureSnapshot();
            row.SpecializationDisclosure.Toggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); await Task.Delay(20);
            check(!row.Specialization.IsEffectivelyVisible && character.CaptureSnapshot().SequenceEqual(beforeSpecializationCollapse),
                "Native knowledge collapsing specialization retains literal text and complete snapshot");
            row.SpecializationDisclosure.Toggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent)); await Task.Delay(20);
            approved = character.CaptureSnapshot();
            controller.CreationKnowledgeTypeInput.Enabled = false; controller.CreationKnowledgeNameInput.Enabled = false;
            controller.CreationRatingInput.Enabled = false; controller.CreationSpecializationInput.Enabled = false;
            row.Type.SelectedItem = controller.CreationKnowledgeTypeInput.Items.Cast<ListItem>().Single(item => item.Value == "Academic");
            row.Name.Text = "Queued disabled name"; row.Rating.Value = 1; row.Specialization.Text = "Queued disabled specialty";
            check(character.CaptureSnapshot().SequenceEqual(approved) && row.Name.Text == controller.SkillObject.Name && row.Rating.Value == 3,
                "Disabled native knowledge fields reject queued edits without changing the character");
            controller.CreationKnowledgeTypeInput.Enabled = true; controller.CreationKnowledgeNameInput.Enabled = true;
            controller.CreationRatingInput.Enabled = true; controller.CreationSpecializationInput.Enabled = true;
            async Task<bool> Specialty(bool accept)
            {
                var done = new TaskCompletionSource<bool>(TaskCreationOptions.RunContinuationsAsynchronously);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        var deadline = DateTime.UtcNow.AddSeconds(20);
                        while (!Forms.Application.OpenForms.OfType<frmSelectText>().Any(dialog => dialog.Visible))
                        { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Knowledge specialty did not open."); await Task.Delay(10); }
                        var dialog = Forms.Application.OpenForms.OfType<frmSelectText>().Last(); await Task.Delay(30);
                        bool blocked = !row.Name.IsEffectivelyEnabled;
                        Field<Forms.TextBox>(dialog, "txtValue").Text = "Synthetic accepted specialty";
                        Field<Forms.Button>(dialog, accept ? "cmdOK" : "cmdCancel").PerformClick();
                        done.TrySetResult(blocked);
                    }
                    catch (Exception error) { done.TrySetException(error); }
                });
                Click(row.AddSpecialty); return await done.Task;
            }
            approved = character.CaptureSnapshot(); dirtyBefore = Field<bool>(editor, "_blnIsDirty");
            bool cancelBlocked = await Specialty(false);
            check(cancelBlocked && row.Name.IsEffectivelyEnabled && character.CaptureSnapshot().SequenceEqual(approved) && Field<bool>(editor, "_blnIsDirty") == dirtyBefore,
                "Native knowledge specialty cancellation blocks the actual editor and preserves its character");
            bool acceptBlocked = await Specialty(true);
            check(acceptBlocked && row.Name.IsEffectivelyEnabled && controller.SkillObject.Specialization == "Synthetic accepted specialty" && Field<bool>(editor, "_blnIsDirty"),
                "Native knowledge specialty acceptance returns to its actual owner and commits through existing handlers");
            var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
            check(reopened.Skills.Any(skill => skill.KnowledgeSkill && skill.Name == controller.SkillObject.Name && skill.SkillCategory == "Street" && skill.Rating == 3 && skill.Specialization == row.Specialization.Text),
                "Native knowledge type name rating and specialization retain legacy serialization");
            using (var prompt = new frmSelectNumber { Minimum = 0, Maximum = 10, Description = "Synthetic knowledge owner check" })
            {
                var pending = prompt.ShowDialogAsync(editor); await Task.Delay(20);
                bool blocked = !row.Name.IsEffectivelyEnabled && !row.Type.IsEffectivelyEnabled && !row.Rating.IsEffectivelyEnabled;
                NativeDialogHost.ActiveWindow!.Close(); await pending;
                check(blocked && row.Name.IsEffectivelyEnabled && row.Type.IsEffectivelyEnabled && row.Rating.IsEffectivelyEnabled,
                    "Native knowledge fields respect modal ownership and recover after cancellation");
            }
            int count = controllers.Count; Field<Forms.Button>(editor, "cmdAddKnowledgeSkill").PerformClick(); await Task.Delay(30);
            var added = controllers.Last();
            check(controllers.Count == count + 1 && section.Rows.ContainsKey(added) && added.Parent == null && character.Skills.Last() == added.SkillObject,
                "Native Add Knowledge appends the exact owned controller and model to the same viewport");
            bool confirm = character.Options.ConfirmDelete;
            try { character.Options.ConfirmDelete = false; Click(section.Rows[added].Delete); }
            finally { character.Options.ConfirmDelete = confirm; }
            check(controllers.Count == count && !section.Rows.ContainsKey(added) && !character.Skills.Contains(added.SkillObject),
                "Native knowledge Delete removes the owned model row and callbacks");
            tabs.SelectedTab = Field<Forms.TabPage>(editor, "tabCommon"); await Task.Delay(20); bool hidden = !row.Layout.IsEffectivelyVisible;
            tabs.SelectedTab = skills; await Task.Delay(20);
            check(hidden && row.Layout.IsEffectivelyVisible, "Native knowledge viewport follows Skills tab availability");
        }
        finally { typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false); editor.Close(); }
        var closed = character.CaptureSnapshot(); held!.Name.Text = "Queued closed name"; held.Rating.Value = 1; held.Specialization.Text = "Queued closed specialty";
        check(character.CaptureSnapshot().SequenceEqual(closed), "Closed native knowledge viewport releases queued field callbacks");
    }
}
