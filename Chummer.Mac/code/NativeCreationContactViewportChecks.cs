#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Avalonia.Styling;
using Avalonia.Media.Imaging;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Drive standard inputs and the original modal commands on synthetic owned rows.
internal static class NativeCreationContactViewportChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static void Clean(frmCreate editor) => typeof(frmCreate).GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
    private static Task Reply<T>(Action<T> respond) where T : Forms.Form
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (!Forms.Application.OpenForms.OfType<T>().Any())
                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Contact modal did not open."); await Task.Delay(10); }
                var dialog = Forms.Application.OpenForms.OfType<T>().Last();
                var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(dialog);
                while (!window.IsVisible || window.Bounds.Width <= 0)
                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Contact modal was not visible."); await Task.Delay(10); }
                respond(dialog); completion.TrySetResult();
            }
            catch (Exception error)
            { Forms.Application.OpenForms.OfType<T>().LastOrDefault()?.Close(); completion.TrySetException(error); }
        });
        return completion.Task;
    }
    internal static async Task RunAsync(Forms.Form owner, Character source, string output, Action<bool, string> check)
    {
        foreach (bool enemy in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
            character.Created = false; character.IgnoreRules = true; character.Contacts.Clear();
            for (int i = 0; i < 3; i++) character.Contacts.Add(new Contact(character)
            { Name = "Synthetic viewport " + i, EntityType = enemy ? ContactType.Enemy : ContactType.Contact, Connection = 2, Loyalty = 1 });
            using var editor = new frmCreate(character) { MdiParent = owner, Loading = true, WindowState = Forms.FormWindowState.Maximized };
            editor.Show(); await Task.Delay(30); editor.Loading = false; Clean(editor);
            var section = editor.ContactPresentation(enemy)!;
            var controller = (enemy ? editor.CreationEnemyControllers : editor.CreationContactControllers).Last();
            var row = section.Rows[controller]; string label = " enemy=" + enemy;
            try
            {
                check(section.Rows.Count == 3 && section.Rows.Keys.Select(c => c.ContactObject).SequenceEqual(character.Contacts)
                    && section.Rows.Keys.All(c => c.Parent == null && ReferenceEquals(c.CreationViewportOwner, editor))
                    && row.Name.GetType() == typeof(TextBox) && row.Connection.GetType() == typeof(NumericUpDown) && !Field<bool>(editor, "_blnIsDirty"),
                    "Native contact viewport opens clean with ordered detached controllers and standard inputs" + label);
                await CheckScrollingAsync(owner, character, editor, section, row, enemy, output, check);
                await NativeContactInteractions.ClickAsync(section, row.Link); row.LinkMenu.Close();
                row.Name.Focus(); row.Name.Text = "Synthetic literal [O'Brien]";
                check(controller.ContactObject.Name == row.Name.Text && Field<bool>(editor, "_blnIsDirty"),
                    "Native contact literal name commits and marks its actual owner dirty" + label);
                row.Connection.Text = "3"; await Task.Delay(20);
                check(controller.ContactObject.Connection == 3, "Native contact rating text uses the original input boundary" + label);
                var input = row.Connection.GetVisualDescendants().OfType<TextBox>().Single();
                input.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up }); await Task.Delay(20);
                check(controller.ContactObject.Connection == 4, "Native contact keyboard stepping uses the original rating transition" + label);
                byte[] approved = character.CaptureSnapshot(); row.Connection.Value = 2.5m;
                check(row.Connection.Value == 4 && character.CaptureSnapshot().SequenceEqual(approved),
                    "Native contact fractional input preserves the complete character" + label);
                row.Connection.Value = null; input.Focus(); row.Name.Focus(); await Task.Delay(20);
                check(row.Connection.Value == 4 && character.CaptureSnapshot().SequenceEqual(approved),
                    "Native contact incomplete rating restores on focus change" + label);
                Clean(editor); controller.Enabled = false;
                row.Name.Text = "Queued disabled"; row.Connection.Value = 5;
                row.Delete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); row.Group.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                check(character.CaptureSnapshot().SequenceEqual(approved) && !Field<bool>(editor, "_blnIsDirty")
                    && !Forms.Application.OpenForms.OfType<frmSelectContactConnection>().Any(),
                    "Native disabled contact rejects queued text numeric delete and group commands" + label);
                controller.Enabled = true; section.Refresh();
                bool modalOwner = false; frmSelectContactConnection? canceledGroup = null, acceptedGroup = null;
                var cancel = Reply<frmSelectContactConnection>(dialog =>
                {
                    canceledGroup = dialog;
                    modalOwner = ReferenceEquals(dialog.Owner, editor);
                    Field<Forms.TextBox>(dialog, "txtGroupName").Text = "Canceled synthetic group";
                    Field<Forms.CheckBox>(dialog, "chkFreeContact").Checked = true;
                    Field<Forms.Button>(dialog, "cmdCancel").PerformClick();
                });
                await NativeContactInteractions.ClickAsync(section, row.Group); await cancel;
                check(modalOwner && canceledGroup!.IsDisposed && approved.SequenceEqual(character.CaptureSnapshot()) && !Field<bool>(editor, "_blnIsDirty") && editor.Enabled,
                    "Native contact group cancellation retains snapshot dirty state and explicit editor ownership" + label);
                var accept = Reply<frmSelectContactConnection>(dialog =>
                {
                    acceptedGroup = dialog;
                    Field<Forms.TextBox>(dialog, "txtGroupName").Text = "Synthetic accepted group";
                    Field<Forms.ComboBox>(dialog, "cboMembership").SelectedIndex = 1;
                    Field<Forms.CheckBox>(dialog, "chkFreeContact").Checked = true;
                    dialog.Colour = System.Drawing.Color.CornflowerBlue;
                    Field<Forms.Button>(dialog, "cmdOK").PerformClick();
                });
                await NativeContactInteractions.ClickAsync(section, row.Group); await accept;
                check(controller.ContactObject.Free && controller.ContactObject.GroupName == "Synthetic accepted group"
                    && controller.ContactObject.Membership == 1 && controller.ContactObject.Colour.ToArgb() == System.Drawing.Color.CornflowerBlue.ToArgb()
                    && acceptedGroup!.IsDisposed && row.Group.Content!.ToString()!.Contains("1") && Field<bool>(editor, "_blnIsDirty") && editor.Enabled,
                    "Native contact group acceptance retains Free modifiers color and original accounting notification" + label);
                Clean(editor); approved = character.CaptureSnapshot();
                await NativeContactInteractions.ClickAsync(section, row.Link);
                var menu = row.LinkMenu.ItemsSource!.Cast<MenuItem>().ToArray();
                check(menu[0].IsVisible && !menu[1].IsVisible && !menu[2].IsVisible && approved.SequenceEqual(character.CaptureSnapshot())
                    && !Field<bool>(editor, "_blnIsDirty"), "Native unlinked contact menu opens at its native anchor without editing character" + label);
                row.LinkMenu.Close();
                controller.ContactObject.FileName = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "synthetic-contact-link.chum");
                controller.ContactObject.RelativeFileName = "synthetic-contact-link.chum";
                await NativeContactInteractions.ClickAsync(section, row.Link);
                check(!menu[0].IsVisible && menu[1].IsVisible && menu[2].IsVisible,
                    "Native linked contact menu exposes original open and remove choices" + label);
                row.LinkMenu.Close();
                string linkedPath = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "chummer-native-contact-link-" + Guid.NewGuid().ToString("N") + ".chum");
                var priorDocuments = owner.MdiChildren.ToArray();
                try
                {
                    var linked = new Character(); linked.LoadSnapshot(source.CaptureSnapshot()); linked.Created = false; linked.Name = "Synthetic linked document";
                    System.IO.File.WriteAllBytes(linkedPath, linked.CaptureSnapshot());
                    controller.ContactObject.FileName = linkedPath; controller.ContactObject.RelativeFileName = "";
                    var beforeOpen = character.CaptureSnapshot();
                    menu[1].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); await Task.Delay(30);
                    var opened = owner.MdiChildren.Except(priorDocuments).OfType<frmCreate>().Single();
                    check(ReferenceEquals(owner.ActiveMdiChild, opened) && Field<Character>(opened, "_objCharacter").Name == "Synthetic linked document"
                        && beforeOpen.SequenceEqual(character.CaptureSnapshot()) && !Field<bool>(editor, "_blnIsDirty"),
                        "Native contact open command loads the associated synthetic character without changing its source" + label);
                    Clean(opened); opened.Close(); await Task.Delay(30); section.Refresh();
                    check(opened.IsDisposed && ReferenceEquals(owner.ActiveMdiChild, editor) && editor.Enabled,
                        "Native linked character close returns to the original contact editor" + label);
                }
                finally
                {
                    foreach (var opened in owner.MdiChildren.Except(priorDocuments).OfType<frmCreate>().ToArray()) { Clean(opened); opened.Close(); }
                    System.IO.File.Delete(linkedPath);
                }
                approved = character.CaptureSnapshot();
                var refuse = Reply<Forms.MessageBoxForm>(dialog => { dialog.DialogResult = Forms.DialogResult.No; dialog.Close(); });
                menu[2].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); await refuse;
                check(approved.SequenceEqual(character.CaptureSnapshot()) && !Field<bool>(editor, "_blnIsDirty") && editor.Enabled,
                    "Native contact link removal refusal retains association and clean owner" + label);
                var consent = Reply<Forms.MessageBoxForm>(dialog => { dialog.DialogResult = Forms.DialogResult.Yes; dialog.Close(); });
                menu[2].RaiseEvent(new RoutedEventArgs(MenuItem.ClickEvent)); await consent;
                check(controller.ContactObject.FileName == "" && controller.ContactObject.RelativeFileName == ""
                    && Field<bool>(editor, "_blnIsDirty") && editor.Enabled,
                    "Native contact accepted link removal clears both paths through original handler" + label);
                Clean(editor); approved = character.CaptureSnapshot();
                var tabs = Field<Forms.TabControl>(editor, "tabCharacterTabs"); tabs.SelectedTab = Field<Forms.TabPage>(editor, "tabSkills"); await Task.Delay(30);
                row.Name.Text = "Queued inactive tab"; row.Connection.Value = 5; row.Delete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                check(approved.SequenceEqual(character.CaptureSnapshot()) && !Field<bool>(editor, "_blnIsDirty"),
                    "Native contact hidden tab rejects queued edits and commands" + label);
                tabs.SelectedIndex = 0; await Task.Delay(30); section.Refresh();
                await NativeContactInteractions.ClickAsync(section, row.Link); row.LinkMenu.Close();
                check(row.Name.IsEffectivelyEnabled && row.Connection.IsEffectivelyEnabled && editor.Enabled,
                    "Native contact return from another tab restores accessible original inputs" + label);
            }
            finally { Clean(editor); editor.Close(); }
            var closed = character.CaptureSnapshot(); row.Name.Text = "Queued disposed"; row.Connection.Value = 6;
            row.Delete.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(editor.IsDisposed && controller.IsDisposed && controller.CreationViewportOwner == null
                && closed.SequenceEqual(character.CaptureSnapshot()) && owner.Enabled,
                "Native contact document close releases bindings modal owner and queued callbacks" + label);
        }
        await RunLimitsAsync(owner, source, check);
    }
    private static async Task CheckScrollingAsync(Forms.Form owner, Character character, frmCreate editor,
        CreationContactSection section, CreationContactRow row, bool enemy, string output, Action<bool, string> check)
    {
        var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
        var previousState = window.WindowState; var previousSize = owner.Size;
        var theme = Application.Current!.RequestedThemeVariant; byte[] before = character.CaptureSnapshot();
        bool dirty = Field<bool>(editor, "_blnIsDirty");
        bool Fits() => new Control[] { row.Name, row.Connection, row.Loyalty, row.Group, row.Notes, row.Link, row.Delete }.All(input =>
            input.IsEffectivelyVisible && input.Bounds.Height >= 20 && input.TranslatePoint(default, section.Viewport) is { } p
            && p.X >= -1 && p.Y >= -1 && p.X + input.Bounds.Width <= section.Viewport.Viewport.Width + 1
            && p.Y + input.Bounds.Height <= section.Viewport.Viewport.Height + 1);
        void Capture(string kind, ThemeVariant variant)
        {
            using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width * 2),
                (int)Math.Ceiling(window.ClientSize.Height * 2)), new Vector(192, 192));
            image.Render(window); image.Save(System.IO.Path.Combine(output, "native-creation-" + (enemy ? "enemies" : "contacts")
                + "-" + kind + "-" + variant.Key.ToString()!.ToLowerInvariant() + ".png"), PngBitmapEncoderOptions.Default);
        }
        var geometryTrace = new System.Collections.Generic.List<string>();
        void TraceGeometry(string reason)
        {
            if (geometryTrace.Count < 32)
                geometryTrace.Add(FormattableString.Invariant($"{reason}:state={window.WindowState}:client={window.ClientSize}:requested={window.Width}x{window.Height}:frame={window.FrameSize}:position={window.Position}:active={window.IsActive}"));
        }
        EventHandler<WindowResizedEventArgs> traceResize = (_, _) => TraceGeometry("resize");
        EventHandler<AvaloniaPropertyChangedEventArgs> traceProperty = (_, change) =>
        {
            if (change.Property == Window.WidthProperty || change.Property == Window.HeightProperty ||
                change.Property == Window.WindowStateProperty || change.Property == TopLevel.ClientSizeProperty)
            {
                TraceGeometry(change.Property.Name);
                if (geometryTrace.Count < 32 && (change.Property == Window.WidthProperty || change.Property == Window.HeightProperty))
                    geometryTrace.Add("via=" + string.Join(">", new System.Diagnostics.StackTrace().GetFrames().Take(12)
                        .Select(frame => frame.GetMethod()).Where(method => method != null)
                        .Select(method => method!.DeclaringType?.FullName + "." + method.Name)));
            }
        };
        window.Resized += traceResize; window.PropertyChanged += traceProperty;
        TraceGeometry("before-contact-captures");
        try
        {
            foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                Application.Current.RequestedThemeVariant = variant; await Task.Delay(80);
                row.Frame.BringIntoView(); await Task.Delay(30); Capture("scroll", variant);
                check(Fits(), "Native creation contact scrolling exposes every final-row field and command enemy=" + enemy + " theme=" + variant.Key
                    + FormattableString.Invariant($" [row={row.Frame.Bounds}; viewport={section.Viewport.Viewport}; offset={section.Viewport.Offset}; actual={window.ClientSize}]"));
            }
            var screen = window.Screens.ScreenFromWindow(window);
            Size expected = screen != null ? screen.Bounds.Size.ToSize(screen.Scaling) : default;
            TraceGeometry("before-fullscreen");
            var firstFullscreenDeadline = await NativeFullscreenTransition.RequestAsync(window);
            bool firstFullscreenTheme = true;
            TraceGeometry("requested-fullscreen");
            foreach (var variant in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                Application.Current.RequestedThemeVariant = variant;
                var deadline = firstFullscreenTheme ? firstFullscreenDeadline : DateTime.UtcNow.AddSeconds(20);
                firstFullscreenTheme = false; DateTime? matched = null; bool ready = false;
                while (DateTime.UtcNow < deadline)
                {
                    bool matches = window.WindowState == WindowState.FullScreen && Math.Abs(window.ClientSize.Width - expected.Width) <= 1
                        && Math.Abs(window.ClientSize.Height - expected.Height) <= 1;
                    if (matches)
                    { matched ??= DateTime.UtcNow; if (DateTime.UtcNow - matched.Value >= TimeSpan.FromMilliseconds(120)) { ready = true; break; } }
                    else matched = null;
                    await Task.Delay(10);
                }
                row.Frame.BringIntoView(); await Task.Delay(30); Capture("fullscreen", variant);
                check(screen != null && ready && window.WindowState == WindowState.FullScreen
                    && Math.Abs(window.ClientSize.Width - expected.Width) <= 1 && Math.Abs(window.ClientSize.Height - expected.Height) <= 1 && Fits(),
                    "Native fullscreen contact final row fits actual screen geometry enemy=" + enemy + " theme=" + variant.Key
                    + FormattableString.Invariant($" [expected={expected}; actual={window.ClientSize}; state={window.WindowState}; screen={screen?.Bounds}; workarea={screen?.WorkingArea}; scaling={screen?.Scaling}; requested={window.Width}x{window.Height}; frame={window.FrameSize}; min={window.MinWidth}x{window.MinHeight}; max={window.MaxWidth}x{window.MaxHeight}; sizing={window.SizeToContent}; owner={owner.Size}; fits={Fits()}; row={row.Frame.Bounds}; viewport={section.Viewport.Viewport}; offset={section.Viewport.Offset}; trace={string.Join("|", geometryTrace)}]"));
            }
        }
        finally
        {
            window.Resized -= traceResize; window.PropertyChanged -= traceProperty;
            Application.Current.RequestedThemeVariant = theme;
            window.WindowState = WindowState.Normal; owner.Size = previousSize; window.UpdateLayout(); window.WindowState = previousState;
            await Task.Delay(120);
        }
        check(before.SequenceEqual(character.CaptureSnapshot()) && dirty == Field<bool>(editor, "_blnIsDirty"),
            "Native contact scrolling fullscreen themes and restoration retain snapshot and dirty state enemy=" + enemy);
    }
    private static async Task RunLimitsAsync(Forms.Form owner, Character source, Action<bool, string> check)
    {
        foreach (var method in new[] { CharacterBuildMethod.BP, CharacterBuildMethod.Karma })
        foreach (bool qualityLimit in new[] { false, true })
        {
            var bytes = CreationContactRuleFixture.Create(source, method, qualityLimit);
            var originalModel = new Character(); originalModel.LoadSnapshot(bytes);
            var nativeModel = new Character(); nativeModel.LoadSnapshot(bytes);
            bool exceed = nativeModel.Options.ExceedNegativeQualities;
            using var original = new frmCreate(originalModel) { MdiParent = owner, Loading = true };
            using var editor = new frmCreate(nativeModel) { MdiParent = owner, Loading = true, WindowState = Forms.FormWindowState.Maximized };
            try
            {
                nativeModel.Options.ExceedNegativeQualities = false;
                original.Show(); editor.Show(); await Task.Delay(30); original.Loading = editor.Loading = false;
                var canonical = original.CreationEnemyControllers.Last();
                var adapted = editor.CreationEnemyControllers.Last();
                var row = editor.ContactPresentation(true)!.Rows[adapted];
                int notices = 0;
                var legacyNotice = Reply<Forms.MessageBoxForm>(dialog => { notices++; dialog.DialogResult = Forms.DialogResult.OK; dialog.Close(); });
                canonical.CreationConnectionInput.Value = 5; await legacyNotice;
                var nativeNotice = Reply<Forms.MessageBoxForm>(dialog => { notices++; dialog.DialogResult = Forms.DialogResult.OK; dialog.Close(); });
                row.Connection.Value = 5; await nativeNotice;
                check(notices == 2 && adapted.ContactObject.Connection == canonical.ContactObject.Connection
                    && row.Connection.Value == adapted.CreationConnectionInput.Value && editor.Enabled,
                    "Native enemy boundary restores original corrected rating after the actual rule notice method=" + method + " qualityLimit=" + qualityLimit);
                check(nativeModel.CaptureSnapshot().SequenceEqual(originalModel.CaptureSnapshot())
                    && Field<Forms.Label>(editor, "lblEnemiesBP").Text == Field<Forms.Label>(original, "lblEnemiesBP").Text,
                    "Native enemy cap and negative quality accounting match the canonical complete snapshot method=" + method + " qualityLimit=" + qualityLimit);
            }
            finally
            {
                nativeModel.Options.ExceedNegativeQualities = exceed;
                Clean(original); Clean(editor); original.Close(); editor.Close();
            }
        }
    }
}
