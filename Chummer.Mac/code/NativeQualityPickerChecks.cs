#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeQualityPickerChecks
{
    private static T Find<T>(Window window, string name) where T : Control =>
        window.GetLogicalDescendants().OfType<T>().Single(control => control.Name == name);
    private static async Task WaitForAsync(Func<bool> condition)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        while (!condition())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Native quality picker transition did not complete.");
            await Task.Delay(20);
        }
    }
    internal static async Task CheckEditorTreeAsync(Forms.Form owner, Character fixture, bool career, Action<bool, string> check, string? snapshots = null)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = career;
        using Forms.Form editor = career ? new frmCareer(character) : new frmCreate(character);
        editor.MdiParent = owner;
        editor.Show();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        T Field<T>(string name) => (T)editor.GetType().GetField(name, flags)!.GetValue(editor)!;

        Field<Forms.TabControl>("tabCharacterTabs").SelectedTab = Field<Forms.TabPage>("tabCommon");
        var source = Field<Forms.TreeView>("treQualities");
        string editorName = career ? "Career" : "Creation";
        var host = owner.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == $"Native{editorName}Qualities");
        var tree = (TreeView)host.NativeControl!;
        await WaitForAsync(() => host.Visible && TopLevel.GetTopLevel(tree) != null && tree.Bounds.Height > 0);
        if (!career)
        {
            var attributes = Field<Forms.Panel>("panAttributes");
            var anchor = Field<Forms.TabPage>("tabCommon");
            check(tree.Bounds.Width + source.Left < attributes.Left && attributes.Parent == anchor,
                "Creation Qualities fit in the column before the Attributes viewport");
        }
        check(source.Parent == null && tree.Items.Count == source.Nodes.Count, $"{editorName} qualities use a visible native tree");

        byte[] before = character.CaptureSnapshot();
        var root = (TreeViewItem)tree.Items[0]!;
        var menu = Field<Forms.ContextMenuStrip>("cmsQuality");
        var node = new Forms.TreeNode
        {
            Text = "Synthetic quality",
            ToolTipText = "Synthetic notes",
            ContextMenuStrip = menu
        };
        source.Nodes[0].Nodes.Add(node);
        source.Nodes[0].Expand();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        var row = root.Items.OfType<TreeViewItem>().Single(item => ReferenceEquals(item.Tag, node));
        check(root.IsExpanded && (string)row.Header! == node.Text && (string)ToolTip.GetTip(row)! == node.ToolTipText,
            $"{editorName} qualities display an inserted item with its caption and notes");

        tree.SelectedItem = row;
        check(source.SelectedNode == node, "Native quality selection sets the command target by node identity");
        bool opening = false;
        void ObserveMenu(object? sender, System.ComponentModel.CancelEventArgs args)
        {
            opening = source.SelectedNode == node;
            args.Cancel = true;
        }
        menu.Opening += ObserveMenu;
        try
        {
            row.RaiseEvent(new ContextRequestedEventArgs { RoutedEvent = InputElement.ContextRequestedEvent });
            check(opening, "Keyboard context request routes to the existing menu for the selected quality");
        }
        finally { menu.Opening -= ObserveMenu; }

        if (snapshots != null)
        {
            var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
            var app = Application.Current!;
            var previousTheme = app.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    app.RequestedThemeVariant = theme;
                    await WaitForAsync(() => tree.ActualThemeVariant == theme);
                    await Task.Delay(120);
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2,
                        (int)window.ClientSize.Height * 2), new Vector(192, 192));
                    bitmap.Render(window);
                    bitmap.Save(Path.Combine(snapshots, $"qualities-{editorName.ToLowerInvariant()}-{theme.Key}.png"),
                        PngBitmapEncoderOptions.Default);
                }
            }
            finally { app.RequestedThemeVariant = previousTheme; }
        }

        node.Text = "Renamed synthetic quality";
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        check(ReferenceEquals(tree.SelectedItem, row) && (string)row.Header! == node.Text,
            "A quality caption refresh preserves the selected native row");
        root.IsExpanded = false;
        check(!source.Nodes[0].IsExpanded, "Native quality collapse is reflected in the command tree");
        node.Remove();
        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        check(!root.Items.Contains(row) && !ReferenceEquals(tree.SelectedItem, row) &&
            character.CaptureSnapshot().SequenceEqual(before),
            "Removing the selected presentation row clears its identity without altering the character");
        editor.Close();
        check(!owner.Controls.Contains(host), $"Closing {editorName} disposes the native quality tree host");
    }

    internal static async Task RunAsync(Forms.Form owner, Character source, string output, Action<bool, string> check)
    {
        var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
        var previousTheme = nativeOwner.RequestedThemeVariant;
        try
        {
            foreach (bool career in new[] { false, true })
            foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
            {
                var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
                character.Created = career; character.IgnoreRules = true;
                byte[] before = character.CaptureSnapshot();
                nativeOwner.RequestedThemeVariant = theme;
                using var picker = new frmSelectQuality(character);
                var pending = picker.ShowDialogAsync(owner);
                var window = (QualityPickerWindow)NativeDialogHost.ActiveWindow!;
                await WaitForAsync(() => window.AcceptButton.IsEnabled && window.ItemList.Bounds.Height > 0);
                check(!nativeOwner.IsEnabled && window.ActualThemeVariant == theme,
                    "Native quality picker owns input and follows its actual owner theme");
                var free = Find<CheckBox>(window, "qualityPickerFreeCost");
                check(free.IsEffectivelyVisible && free.IsEnabled && !window.GetLogicalDescendants().OfType<NumericUpDown>().Any(),
                    "Native quality exposes Free in creation and career without unrelated markup");
                free.IsChecked = true;
                await WaitForAsync(() => window.Model.FreeCost && window.Model.Details!.Cost == "0");
                check(window.Model.Details!.Cost == "0", "Native quality Free updates the displayed cost");
                window.SearchBox.Text = "a";
                await WaitForAsync(() => window.Model.Items.Count > 1);
                var first = window.Model.SelectedEntry;
                window.SearchBox.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
                await WaitForAsync(() => window.Model.SelectedEntry != first);
                check(Equals(window.Model.SelectedEntry, window.ItemList.SelectedItem), "Native quality search keyboard navigation updates actual selection");
                Find<CheckBox>(window, "qualityPickerMetagenetic").IsChecked = true;
                await WaitForAsync(() => window.Model.MetageneticOnly);
                check(window.Model.Items.Count > 0 && window.Model.Items.All(item => item.Definition.SelectSingleNode(
                    "required/oneof[contains(., 'Changeling (Class I SURGE)')] | metagenetic[.='yes']") != null),
                    "Native quality metagenetic checkbox filters the actual choices");
                Find<CheckBox>(window, "qualityPickerMetagenetic").IsChecked = false;
                window.SearchBox.Text = "unmatched \"synthetic\" quality";
                await WaitForAsync(() => !window.AcceptButton.IsEnabled && !window.AddMoreButton.IsEnabled);
                check(window.Model.Items.Count == 0, "Native quality literal quoted search disables both acceptance buttons");
                window.SearchBox.Text = "a";
                await WaitForAsync(() => window.AcceptButton.IsEnabled && window.ItemList.ContainerFromIndex(0) is { IsEffectivelyVisible: true });
                await Task.Delay(80);
                using (var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192)))
                {
                    bitmap.Render(window);
                    bitmap.Save(Path.Combine(output, "native-quality-" + (career ? "career-" : "creation-") + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                }
                check(character.CaptureSnapshot().SequenceEqual(before), "Native quality browsing and theme changes preserve the entire character");
                window.Close();
                check(await pending == Forms.DialogResult.Cancel && nativeOwner.IsEnabled && NativeDialogHost.Count == 0 && character.CaptureSnapshot().SequenceEqual(before),
                    "Native quality window dismissal cancels and restores its owner without accounting changes");
            }
            foreach (bool again in new[] { false, true })
            {
                var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.IgnoreRules = true;
                byte[] before = character.CaptureSnapshot();
                using var picker = new frmSelectQuality(character) { ForceCategory = "Negative" };
                var pending = picker.ShowDialogAsync(owner); var window = (QualityPickerWindow)NativeDialogHost.ActiveWindow!;
                window.SearchBox.Text = "a";
                await WaitForAsync(() => window.AcceptButton.IsEnabled);
                string selected = window.Model.SelectedEntry!.Name;
                Find<CheckBox>(window, "qualityPickerFreeCost").IsChecked = true;
                (again ? window.AddMoreButton : window.AcceptButton).RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                check(await pending == Forms.DialogResult.OK && picker.SelectedQuality == selected && picker.FreeCost && picker.AddAgain == again,
                    "Native quality actual acceptance buttons preserve selection, Free and Add Again intent");
                check(nativeOwner.IsEnabled && NativeDialogHost.Count == 0 && character.CaptureSnapshot().SequenceEqual(before),
                    "Native quality acceptance leaves purchase accounting with its caller and restores owner input");
            }
            var rejectedCharacter = new Character(); rejectedCharacter.LoadSnapshot(source.CaptureSnapshot());
            rejectedCharacter.IgnoreRules = true;
            using var rejectedPicker = new frmSelectQuality(rejectedCharacter);
            var rejectedPending = rejectedPicker.ShowDialogAsync(owner);
            var rejectedWindow = (QualityPickerWindow)NativeDialogHost.ActiveWindow!;
            await WaitForAsync(() => rejectedWindow.AcceptButton.IsEnabled);
            rejectedWindow.Model.SelectedEntry = rejectedWindow.Model.Items.First(item => item.Definition["limit"]?.InnerText != "no");
            string duplicateName = rejectedWindow.Model.SelectedEntry!.Name;
            rejectedCharacter.Qualities.Add(new Quality(rejectedCharacter) { Name = duplicateName });
            rejectedCharacter.IgnoreRules = false;
            Find<CheckBox>(rejectedWindow, "qualityPickerLimitList").IsChecked = false;
            byte[] rejectionBefore = rejectedCharacter.CaptureSnapshot();
            var replied = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
            {
                try
                {
                    await WaitForAsync(() => Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any());
                    var notice = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last();
                    notice.DialogResult = Forms.DialogResult.OK; notice.Close(); replied.TrySetResult();
                }
                catch (Exception error) { NativeDialogHost.CancelAll(); replied.TrySetException(error); }
            });
            rejectedWindow.AcceptButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await replied.Task;
            check(!rejectedPending.IsCompleted && rejectedPicker.DialogResult == Forms.DialogResult.Cancel,
                "Native quality acceptance rechecks duplicate eligibility after disabling the list filter");
            check(rejectedCharacter.CaptureSnapshot().SequenceEqual(rejectionBefore),
                "Native quality rejected purchase preserves all character accounting");
            rejectedWindow.Close(); await rejectedPending;
            check(nativeOwner.IsEnabled && NativeDialogHost.Count == 0,
                "Native quality rejection followed by cancellation restores owner input");
        }
        finally { nativeOwner.RequestedThemeVariant = previousTheme; }
    }
}
