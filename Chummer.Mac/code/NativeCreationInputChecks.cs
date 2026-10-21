#nullable enable
using System;
using System.Linq;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeCreationInputChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static async Task WithRuleNotice(Action change)
    {
        var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Creation rule notice did not open."); await Task.Delay(10); }
                var notice = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last();
                notice.DialogResult = Forms.DialogResult.OK; notice.Close(); dismissed.TrySetResult();
            }
            catch (Exception error) { dismissed.TrySetException(error); }
        });
        change(); await dismissed.Task;
    }
    internal static async Task RunAsync(Forms.Form owner, Character source, string output, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = false; character.IgnoreRules = true;
        character.Contacts.Clear();
        character.Contacts.Add(new Contact(character) { Name = "Synthetic layout contact", EntityType = ContactType.Contact, Connection = 2, Loyalty = 1 });
        character.Contacts.Add(new Contact(character) { Name = "Synthetic layout enemy", EntityType = ContactType.Enemy, Connection = 2, Loyalty = 1 });
        using var editor = new frmCreate(character) { MdiParent = owner, Loading = true, WindowState = Forms.FormWindowState.Maximized };
        editor.Show(); await Task.Delay(20);
        bool previousBudgetOption = character.Options.AllowExceedAttributeBP;
        try
        {
            var section = Field<CreationInputSection>(editor, "creationInputSection");
            check(section.Inputs.Count == 12 && section.Inputs.Values.All(input => input.GetType() == typeof(Avalonia.Controls.NumericUpDown)),
                "Native creation batch exposes eleven attribute inputs and one money allocation input");
            check(section.Inputs.Keys.All(name => Field<Forms.NumericUpDown>(editor, name).Parent == null),
                "Native creation inputs retain detached controller state without drawing legacy spinners");
            var application = Avalonia.Application.Current!;
            var originalTheme = application.RequestedThemeVariant;
            var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
            var originalWindowState = window.WindowState;
            var originalOwnerSize = owner.Size;
            var geometryEvents = new System.Collections.Generic.List<string>();
            EventHandler<Avalonia.Controls.WindowResizedEventArgs> traceResize = (_, _) =>
            {
                if (geometryEvents.Count < 24)
                    geometryEvents.Add(FormattableString.Invariant($"{window.WindowState}:{window.ClientSize}:requested={window.Width}x{window.Height}"));
            };
            window.Resized += traceResize;
            var geometryPropertyChanges = new System.Collections.Generic.List<string>();
            EventHandler<AvaloniaPropertyChangedEventArgs> traceGeometryProperty = (_, change) =>
            {
                if (geometryPropertyChanges.Count >= 16 ||
                    (change.Property != Avalonia.Controls.Window.WidthProperty && change.Property != Avalonia.Controls.Window.HeightProperty)) return;
                // Method names only: no file paths, arguments, image bytes or character content.
                var methods = new System.Diagnostics.StackTrace().GetFrames().Take(14)
                    .Select(frame => frame.GetMethod()).Where(method => method != null)
                    .Select(method => method!.DeclaringType?.FullName + "." + method.Name);
                geometryPropertyChanges.Add(FormattableString.Invariant($"{change.Property.Name}:{window.Width}x{window.Height}:via={string.Join(">", methods)}"));
            };
            window.PropertyChanged += traceGeometryProperty;


            async Task RestoreNormalBeforeSizing()
            {
                if (window.WindowState == Avalonia.Controls.WindowState.Normal) return;
                window.WindowState = Avalonia.Controls.WindowState.Normal;
                // The state setter reports intermediate sizes synchronously, but
                // waiting for another resize can deadlock when no resize is pending.
                // Yield to the next actual frame before issuing the capture size.
                var frame = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                window.RequestAnimationFrame(_ => frame.TrySetResult());
                var completed = await Task.WhenAny(frame.Task, Task.Delay(TimeSpan.FromSeconds(20)));
                if (completed != frame.Task)
                    check(false, "Native normal restoration reaches a frame before capture sizing" +
                        FormattableString.Invariant($" [actual={window.ClientSize}; state={window.WindowState}; resizeEvents={string.Join("|", geometryEvents)}]"));

            }
            bool GeometryMatches(Avalonia.Controls.WindowState state, Size size) => window.WindowState == state &&
                Math.Abs(window.ClientSize.Width - size.Width) <= 1 && Math.Abs(window.ClientSize.Height - size.Height) <= 1;
            async Task<bool> WaitForGeometry(Avalonia.Controls.WindowState state, Size size)
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                DateTime? matchedSince = null;
                while (DateTime.UtcNow < deadline)
                {
                    if (GeometryMatches(state, size))
                    {
                        matchedSince ??= DateTime.UtcNow;
                        if (DateTime.UtcNow - matchedSince.Value >= TimeSpan.FromMilliseconds(120)) return true;
                    }
                    else matchedSince = null;
                    await Task.Delay(10);
                }
                return false;
            }
            var attributeInputs = section.Inputs.Where(pair => pair.Key != "nudNuyen").Select(pair => pair.Value).ToArray();
            var root = (Avalonia.Controls.Grid)attributeInputs[0].Parent!;
            var commonPage = Field<Forms.TabPage>(editor, "tabCommon");
            var attributesPanel = Field<Forms.Panel>(editor, "panAttributes");
            var contacts = Field<Forms.SplitContainer>(editor, "splitContacts");
            bool AllAttributesFit() => attributeInputs.All(input => input.IsEffectivelyVisible &&
                input.TranslatePoint(default, root) is { } point && point.Y >= -1 &&
                point.Y + input.Bounds.Height <= root.Bounds.Height + 1 && input.Bounds.Height >= 20 &&
                input.GetVisualDescendants().OfType<Avalonia.Controls.TextBox>().Any(text => text.Bounds.Width >= 24) &&
                input.GetVisualDescendants().OfType<Avalonia.Controls.Presenters.TextPresenter>().Any(text =>
                    text.TranslatePoint(default, input) is { } position && position.Y >= -1 &&
                    position.Y + text.TextLayout.Height <= input.Bounds.Height + 1));
            bool ContactsFit() => contacts.Right <= commonPage.ClientSize.Width && new[] { "panContacts", "panEnemies" }.All(name =>
            {
                var panel = Field<Forms.Panel>(editor, name);
                var rows = panel.Controls.OfType<ContactControl>().ToArray();
                return rows.Length == 1 && rows.All(row => row.Right <= panel.ClientSize.Width && row.Bottom <= panel.ClientSize.Height &&
                    row.Controls.Cast<Forms.Control>().Where(child => child.Visible).All(child => child.Left >= 0 &&
                        child.Right <= row.ClientSize.Width && child.Bottom <= row.ClientSize.Height));
            });
            void Capture(string name)
            {
                using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width * 2),
                    (int)Math.Ceiling(window.ClientSize.Height * 2)), new Vector(192, 192));
                image.Render(window); image.Save(Path.Combine(output, name), PngBitmapEncoderOptions.Default);
            }
            byte[] beforeLayout = character.CaptureSnapshot();
            bool dirtyBeforeLayout = Field<bool>(editor, "_blnIsDirty");
            try
            {
                // Capture both themes in the same measured mode, then transition
                // once. A fullscreen exit is not a prerequisite for a theme swap.
                await RestoreNormalBeforeSizing();
                owner.Size = new System.Drawing.Size(932, 545);
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    application.RequestedThemeVariant = theme;
                    bool ready = await WaitForGeometry(Avalonia.Controls.WindowState.Normal, new Size(932, 545));
                    Capture("native-creation-inputs-" + theme.Key.ToString()!.ToLowerInvariant() + ".png");
                    check(ready && GeometryMatches(Avalonia.Controls.WindowState.Normal, new Size(932, 545)),
                        "Native creation normal capture uses requested932x545 client geometry in " + theme.Key +
                        FormattableString.Invariant($" [actual={window.ClientSize}; state={window.WindowState}; requested={window.Width}x{window.Height}; min={window.MinWidth}x{window.MinHeight}; max={window.MaxWidth}x{window.MaxHeight}; sizing={window.SizeToContent}; owner={owner.Size}; resizeEvents={string.Join("|", geometryEvents)}; propertyChanges={string.Join("|", geometryPropertyChanges)}]"));
                    check(section.Inputs.Values.Any(input => input.IsVisible) && section.Inputs.Values.Where(input => input.IsVisible).All(input =>
                        input.Bounds.Width > 0 && input.Bounds.Height >= 20 && input.GetVisualDescendants().OfType<Avalonia.Controls.TextBox>().Any(text => text.Bounds.Width >= 24)),
                        "Native creation input batch has measured controls in " + theme.Key);
                    check(AllAttributesFit() && attributesPanel.Bottom <= commonPage.ClientSize.Height &&
                        attributeInputs.All(input => input.TranslatePoint(default, window) is { } point && point.Y >= 0 &&
                            point.Y + input.Bounds.Height <= window.ClientSize.Height) &&
                        !attributeInputs[0].GetVisualAncestors().OfType<Avalonia.Controls.ScrollViewer>().Any(),
                        "Native creation shows all eleven attribute rows simultaneously without scrolling in " + theme.Key +
                        FormattableString.Invariant($" [window={window.ClientSize}; common={commonPage.ClientSize}; attributes={attributesPanel.Bounds}; root={root.Bounds}]"));
                    check(ContactsFit(), "Native normal creation column exposes populated contact and enemy fields and commands in " + theme.Key);
                }
                var screen = window.Screens.ScreenFromWindow(window);
                Size fullscreenSize = screen != null ? screen.Bounds.Size.ToSize(screen.Scaling) : window.ClientSize;
                window.WindowState = Avalonia.Controls.WindowState.FullScreen;
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    application.RequestedThemeVariant = theme;
                    bool ready = await WaitForGeometry(Avalonia.Controls.WindowState.FullScreen, fullscreenSize);
                    Capture("native-creation-inputs-fullscreen-" + theme.Key.ToString()!.ToLowerInvariant() + ".png");
                    check(screen != null && ready && GeometryMatches(Avalonia.Controls.WindowState.FullScreen, fullscreenSize),
                        "Native creation full-screen capture uses actual screen client geometry in " + theme.Key +
                        FormattableString.Invariant($" [expected={fullscreenSize}; actual={window.ClientSize}; state={window.WindowState}]"));
                    check(AllAttributesFit() && contacts.Left >= attributesPanel.Right && contacts.Right <= commonPage.ClientSize.Width && contacts.Bottom <= contacts.Parent!.ClientSize.Height,
                        "Native creation full screen keeps all attributes visible and contacts beside them in " + theme.Key);
                    check(ContactsFit(), "Native full-screen creation column exposes populated contact and enemy fields and commands in " + theme.Key);
                    check(character.CaptureSnapshot().SequenceEqual(beforeLayout) && Field<bool>(editor, "_blnIsDirty") == dirtyBeforeLayout,
                        "Native creation resizing preserves character and dirty state in " + theme.Key);
                }
            }
            finally
            {
                window.Resized -= traceResize;
                window.PropertyChanged -= traceGeometryProperty;
                application.RequestedThemeVariant = originalTheme;
                await RestoreNormalBeforeSizing(); owner.Size = originalOwnerSize;
                window.WindowState = originalWindowState; await Task.Delay(120);
            }
            check(character.CaptureSnapshot().SequenceEqual(beforeLayout) && Field<bool>(editor, "_blnIsDirty") == dirtyBeforeLayout,
                "Native creation returning to the original window mode preserves character and dirty state");
            bool originalMagic = character.MAGEnabled, originalResonance = character.RESEnabled;
            try
            {
                character.MAGEnabled = true; character.RESEnabled = false;
                check(section.Inputs["nudMAG"].IsEnabled && !section.Inputs["nudRES"].IsEnabled &&
                    section.Inputs["nudMAG"].Minimum == Field<Forms.NumericUpDown>(editor, "nudMAG").Minimum &&
                    section.Inputs["nudMAG"].Maximum == Field<Forms.NumericUpDown>(editor, "nudMAG").Maximum,
                    "Native creation Magic availability and limits follow actual character enable events");
                character.MAGEnabled = false; character.RESEnabled = true;
                check(!section.Inputs["nudMAG"].IsEnabled && section.Inputs["nudRES"].IsEnabled &&
                    section.Inputs["nudRES"].Minimum == Field<Forms.NumericUpDown>(editor, "nudRES").Minimum &&
                    section.Inputs["nudRES"].Maximum == Field<Forms.NumericUpDown>(editor, "nudRES").Maximum,
                    "Native creation Resonance availability and limits follow actual character enable events");
            }
            finally { character.MAGEnabled = originalMagic; character.RESEnabled = originalResonance; }
            var body = Field<Forms.NumericUpDown>(editor, "nudBOD"); var input = section.Inputs["nudBOD"];
            editor.Loading = true; body.Minimum = 1; body.Maximum = 10; body.Value = 2;
            check(input.Minimum == 1 && input.Maximum == 10 && input.Value == 2,
                "Native creation attributes follow controller loading bounds and values");
            editor.Loading = false;
            int before = character.BOD.Value; input.Value = null;
            check(character.BOD.Value == before && body.Value == 2,
                "Cleared inline creation input does not commit a fallback value");
            var textEditor = input.GetVisualDescendants().OfType<Avalonia.Controls.TextBox>().Single();
            textEditor.Focus();
            section.Inputs["nudNuyen"].Focus(); await Task.Delay(20);
            check(input.Value == body.Value && character.BOD.Value == before,
                "Leaving an incomplete creation input restores controller state without committing a fallback");
            byte[] beforeFractionalEdit = character.CaptureSnapshot();
            decimal approvedAttribute = body.Value;
            bool dirtyBeforeFractionalEdit = Field<bool>(editor, "_blnIsDirty");
            input.Value = approvedAttribute + 0.5m;
            check(input.ParsingNumberStyle == System.Globalization.NumberStyles.Integer &&
                input.Value == approvedAttribute && body.Value == approvedAttribute &&
                character.CaptureSnapshot().SequenceEqual(beforeFractionalEdit) &&
                Field<bool>(editor, "_blnIsDirty") == dirtyBeforeFractionalEdit,
                "Native fractional creation attribute edit preserves character and dirty state");
            input.Text = "3"; await Task.Delay(30);
            check(body.Value == 3 && character.BOD.Value == 3 && Field<bool>(editor, "_blnIsDirty"),
                "Native creation attribute edit uses the existing controller and dirty-state path");
            textEditor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up }); await Task.Delay(20);
            check(input.Value == 4 && body.Value == 4 && character.BOD.Value == 4,
                "Native creation keyboard step follows existing attribute rules and model updates");
            var infoTabs = Field<Forms.TabControl>(editor, "tabCharacterTabs"); var common = Field<Forms.TabPage>(editor, "tabCommon");
            infoTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabCharacterInfo"); await Task.Delay(20);
            bool hidden = infoTabs.SelectedTab == Field<Forms.TabPage>(editor, "tabCharacterInfo") && !input.IsEffectivelyVisible;
            infoTabs.SelectedTab = common; await Task.Delay(20);
            check(common.Parent == infoTabs && infoTabs.SelectedTab == common && hidden && input.IsEffectivelyVisible,
                "Native creation input section hides and restores with its Common tab");
            using (var prompt = new frmSelectNumber { Minimum = 0, Maximum = 10, Description = "Synthetic creation ownership check" })
            {
                var pending = prompt.ShowDialogAsync(editor); await Task.Delay(20);
                check(!input.IsEffectivelyEnabled,
                    "Native creation inputs respect modal owner blocking");
                NativeDialogHost.ActiveWindow!.Close(); await pending;
                check(input.IsEffectivelyEnabled,
                    "Native creation inputs resume after modal cancellation");
            }
            var money = section.Inputs["nudNuyen"]; money.Value = 2.5m;
            check(character.NuyenBP == 2.5m && Field<Forms.NumericUpDown>(editor, "nudNuyen").Value == 2.5m,
                "Native money allocation preserves decimal model values through existing accounting");
            editor.Loading = true; body.Maximum = 2;
            check(input.Maximum == 2 && input.Value == body.Value && body.Value == 2,
                "Native attribute refresh follows controller range clipping without a stale value");
            editor.Loading = false; input.Value = 1;
            var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
            check(reopened.BOD.Value == character.BOD.Value && reopened.NuyenBP == character.NuyenBP,
                "Native creation attribute and money edits retain the unchanged character serialization contract");
            body.Enabled = false;
            decimal disabledValue = body.Value; int disabledModel = character.BOD.Value;
            input.Value = disabledValue == body.Minimum ? disabledValue + 1 : disabledValue - 1;
            check(body.Value == disabledValue && character.BOD.Value == disabledModel && input.Value == disabledValue,
                "Native disabled creation attribute rejects queued edits without changing the character");
            body.Enabled = true;
            character.BuildMethod = CharacterBuildMethod.BP;
            character.BuildPoints = 2000; character.IgnoreRules = false; character.Options.AllowExceedAttributeBP = false;
            typeof(frmCreate).GetField("_blnFreestyle", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
            void ResetPrimary()
            {
                editor.Loading = true;
                foreach (string name in new[] { "BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL" })
                { var state = Field<Forms.NumericUpDown>(editor, "nud" + name); state.Value = state.Minimum; }
                editor.Loading = false;
            }
            ResetPrimary(); editor.Loading = true;
            var agility = Field<Forms.NumericUpDown>(editor, "nudAGI"); agility.Value = agility.Maximum;
            editor.Loading = false;
            await WithRuleNotice(() => input.Value = body.Maximum);
            check(body.Value == body.Maximum - 1 && input.Value == body.Value,
                "Native creation edit retains the one-primary-attribute-at-maximum rule");
            character.IgnoreRules = true; input.Value = body.Maximum;
            check(input.Value == body.Value && body.Value == body.Maximum,
                "Native creation Ignore Rules permits an otherwise rejected maximum");
            character.IgnoreRules = false; ResetPrimary();
            character.BuildPoints = character.Options.BPAttribute * 2;
            decimal firstPaidStep = body.Minimum + 1;
            await WithRuleNotice(() => input.Value = body.Minimum + 2);
            check(body.Value == firstPaidStep && input.Value == firstPaidStep,
                "Native creation edit retains the half-BP primary attribute budget and controller correction");
            character.IgnoreRules = true; character.BuildMethod = CharacterBuildMethod.Karma; money.Value = 3.5m;
            int expectedMoney = Convert.ToInt32(3.5m) * character.Options.KarmaNuyenPer +
                Convert.ToInt32(new ImprovementManager(character).ValueOf(Improvement.ImprovementType.Nuyen));
            check(character.NuyenBP == 3.5m && Field<Forms.Label>(editor, "lblNuyenTotal").Text == string.Format("= {0:###,###,##0¥}", expectedMoney),
                "Native Karma money allocation follows existing conversion and improvement accounting");
        }
        finally { character.Options.AllowExceedAttributeBP = previousBudgetOption; typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false); editor.Close(); }
        var closedSection = Field<CreationInputSection>(editor, "creationInputSection"); decimal savedMoney = character.NuyenBP;
        closedSection.Inputs["nudNuyen"].Value = savedMoney == 1 ? 2 : 1;
        check(character.NuyenBP == savedMoney,
            "Closed creation editor rejects queued native input changes");
    }
}
