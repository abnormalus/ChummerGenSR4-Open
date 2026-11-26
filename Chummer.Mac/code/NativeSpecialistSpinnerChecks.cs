#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeSpecialistSpinnerChecks
{
    internal static async Task RunAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        byte[] before = character.CaptureSnapshot();
        await CheckDialog(new frmDiceRoller(owner), async dialog =>
        {
            Input(dialog, "nudDice").Value = 7;
            Input(dialog, "nudThreshold").Value = 4;
            Input(dialog, "nudGremlins").Value = 2;
            var host = (Border)dialog.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var roll = host.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdRollDice");
            roll.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            await Idle();
            check(Field<Forms.ListBox>(dialog, "lstResults").Items.Count == 7 &&
                Field<Forms.NumericUpDown>(dialog, "nudThreshold").Value == 4 &&
                Field<Forms.NumericUpDown>(dialog, "nudGremlins").Value == 2,
                "The Dice Roller uses the native dice pool, threshold, and Gremlins inputs");
            dialog.ClientSize = new System.Drawing.Size(280, 380);
            await Idle();
            var layout = (Grid)host.Child!;
            check(layout.Children[0].Bounds.Bottom <= layout.Children[1].Bounds.Top &&
                layout.Children[1].Bounds.Bottom <= layout.Children[2].Bounds.Top &&
                host.GetVisualDescendants().OfType<ListBox>().Single().ItemCount == 7,
                "Narrow Dice Roller keeps its settings, results, and actions separate");
            var settings = (ScrollViewer)layout.Children[0];
            var threshold = Input(dialog, "nudThreshold");
            var thresholdLabel = Field<Forms.Label>(dialog, "lblThreshold");
            double normalFont = threshold.FontSize;
            thresholdLabel.Text = string.Join(" ", Enumerable.Repeat("Translated dice threshold", 8));
            Avalonia.Controls.Documents.TextElement.SetFontSize(host, normalFont * 1.5);
            for (int attempt = 0; attempt < 20 && (settings.Viewport.Height <= 0 || settings.Extent.Height <= settings.Viewport.Height); attempt++)
                await Task.Delay(20);
            Point? thresholdPosition = null;
            for (int attempt = 0; attempt < 20; attempt++)
            {
                threshold.BringIntoView();
                await Task.Delay(20);
                thresholdPosition = threshold.TranslatePoint(default, settings);
                if (settings.Offset.Y > 0 && thresholdPosition is { Y: >= -1 } position &&
                    position.Y + threshold.Bounds.Height <= settings.Viewport.Height + 1)
                    break;
            }
            check(threshold.FontSize == normalFont * 1.5 && settings.Offset.Y > 0 &&
                thresholdPosition.HasValue && thresholdPosition.Value.Y >= -1 &&
                thresholdPosition.Value.Y + threshold.Bounds.Height <= settings.Viewport.Height + 1 &&
                layout.Children[1].Bounds.Height > 40 && roll.IsEffectivelyVisible &&
                layout.Bounds.Bottom <= host.Bounds.Height && Field<Forms.ListBox>(dialog, "lstResults").Items.Count == 7,
                $"Dice Roller preserves scrolling inputs, results and actions at 150% text [settings={settings.Viewport}/{settings.Offset}, threshold={thresholdPosition}/{threshold.Bounds}, results={layout.Children[1].Bounds}]");
        }, "nudDice", "nudThreshold", "nudGremlins");

        await CheckDialog(new frmMetatype(character), async dialog =>
        {
            dialog.ClientSize = new System.Drawing.Size(320, 340);
            await Idle();
            var surface = (Border)dialog.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var layout = (Grid)surface.Child!;
            var panes = (Grid)layout.Children[0];
            var details = (ScrollViewer)panes.Children[1];
            check(Grid.GetRow(details) == 1 && details.Extent.Height > details.Viewport.Height &&
                panes.Bounds.Bottom <= layout.Children[1].Bounds.Top,
                "The narrow Metatype picker stacks scrollable details above OK and Cancel");
        }, "nudForce");

        await CheckDialog(new frmSelectAdvancedLifestyle(new Lifestyle(character), character), async dialog =>
        {
            dialog.ClientSize = new System.Drawing.Size(350, 450);
            await Idle();
            var surface = (Border)dialog.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var layout = (Grid)surface.Child!;
            var panes = (Grid)layout.Children[0];
            var fields = (ScrollViewer)panes.Children[1];
            check(Grid.GetRow(fields) == 1 && fields.Extent.Height > fields.Viewport.Height &&
                panes.Bounds.Bottom <= layout.Children[1].Bounds.Top &&
                surface.GetVisualDescendants().OfType<TreeView>().All(tree => tree.Bounds.Height > 30),
                "The narrow Advanced Lifestyle dialog keeps both quality trees and scrollable fields above the actions");
            var comforts = surface.GetVisualDescendants().OfType<ComboBox>().Single(combo => combo.Name == "NativecboComforts");
            comforts.SelectedIndex = comforts.SelectedIndex == 1 ? 2 : 1;
            var total = Field<Forms.Label>(dialog, "lblCost");
            string beforeCost = total.Text;
            var share = Input(dialog, "nudPercentage");
            var shareCaption = Field<Forms.Label>(dialog, "lblPercentage");
            var shareRow = share.GetVisualAncestors().OfType<StackPanel>().First();
            var caption = (TextBlock)shareRow.Children[0];
            string originalCaption = shareCaption.Text;
            double enlargedText = share.FontSize * 1.5;
            Avalonia.Controls.Documents.TextElement.SetFontSize(layout, enlargedText);
            shareCaption.Text = "Translated percentage to pay for the selected lifestyle";
            await Idle();
            check(caption.Text == shareCaption.Text && caption.FontSize == enlargedText &&
                share.FontSize == enlargedText && caption.Bounds.Bottom <= share.Bounds.Top &&
                share.Bounds.Right <= fields.Viewport.Width + 1 && panes.Bounds.Bottom <= layout.Children[1].Bounds.Top,
                "Advanced Lifestyle keeps its translated percentage and actions clear at 150% text");
            shareCaption.Text = originalCaption;
            layout.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
            share.Value = share.Value == 50 ? 75 : 50;
            var displayedCost = surface.GetVisualDescendants().OfType<TextBlock>().Single(label => label.Name == "NativelblCost");
            check(Field<Forms.NumericUpDown>(dialog, "nudPercentage").Value == share.Value && total.Text != beforeCost &&
                displayedCost.Text!.EndsWith(total.Text, StringComparison.Ordinal),
                "Changing the native Advanced Lifestyle percentage recalculates the visible draft cost");
        }, "nudPercentage", "nudRoommates");

        await CheckDialog(new frmCreateSpell(character), async dialog =>
        {
            dialog.ClientSize = new System.Drawing.Size(300, 340);
            await Idle();
            var surface = (Border)dialog.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            for (int attempt = 0; attempt < 50 && (surface.Bounds.Width > 301 || surface.Bounds.Height > 341); attempt++)
                await Task.Delay(20);
            await Idle();
            var layout = (Grid)surface.Child!;
            var scroll = (ScrollViewer)layout.Children[0];
            check(scroll.Extent.Height > scroll.Viewport.Height && scroll.Bounds.Bottom <= layout.Children[1].Bounds.Top,
                "The narrow Create Spell dialog scrolls its fields above OK and Cancel");
            var actions = (WrapPanel)layout.Children[1];
            var accept = actions.Children.OfType<Button>().Single(button => button.Name == "NativecmdOK");
            var acceptController = Field<Forms.Button>(dialog, "cmdOK");
            string originalAccept = acceptController.Text;
            double enlarged = accept.FontSize * 1.5;
            Avalonia.Controls.Documents.TextElement.SetFontSize(layout, enlarged);
            acceptController.Text = "Translated confirmation for creating and saving the selected spell";
            await Idle();
            var actionText = (TextBlock)accept.Content!;
            check(actionText.Text == acceptController.Text && actionText.FontSize == enlarged &&
                actionText.TextLayout.TextLines.Count > 1 && accept.Bounds.Right <= actions.Bounds.Width + 1 &&
                Equals(ToolTip.GetTip(accept), acceptController.Text) &&
                scroll.Bounds.Height > 0 && scroll.Bounds.Bottom <= actions.Bounds.Top,
                $"Create Spell wraps its translated confirmation at 150% text and keeps the form above it [host={surface.Bounds}, text={actionText.Bounds}/{actionText.FontSize}, lines={actionText.TextLayout.TextLines.Count}, scroll={scroll.Bounds}, footer={actions.Bounds}]");
            acceptController.Text = originalAccept;
            layout.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
            await Idle();
            var categories = surface.GetVisualDescendants().OfType<ComboBox>().Single(choice => choice.Name == "NativecboCategory");
            categories.SelectedItem = "Combat";
            await Idle();
            var source = Field<Forms.NumericUpDown>(dialog, "nudNumberOfEffects");
            var effects = Input(dialog, source.Name);
            var combat = (StackPanel)effects.Parent!;
            check(effects.IsEffectivelyVisible && combat.Children.OfType<CheckBox>().Single().Name == "NativechkModifier3",
                "The spell effects spinner appears in the Combat modifier row");
            categories.SelectedItem = "Manipulation";
            await Idle();
            check(effects.IsEffectivelyVisible && effects.Parent is StackPanel manipulation &&
                manipulation.Children.OfType<CheckBox>().Single().Name == "NativechkModifier6" && manipulation != combat,
                "Changing to Manipulation moves the native spinner to the new modifier row");
            categories.SelectedItem = "Detection";
            await Idle();
            check(!Input(dialog, source.Name).IsEffectivelyVisible,
                "A spell category without variable effects hides its native spinner");
        }, "nudNumberOfEffects");

        await CheckDialog(new frmCreateImprovement(character), async dialog =>
        {
            dialog.ClientSize = new System.Drawing.Size(270, 210);
            var surface = (Border)dialog.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var layout = (Grid)surface.Child!;
            var scroll = (ScrollViewer)layout.Children[0];
            var source = Field<Forms.NumericUpDown>(dialog, "nudVal");
            var value = Input(dialog, "nudVal");
            source.Visible = true;
            Avalonia.Controls.Documents.TextElement.SetFontSize(layout, value.FontSize * 1.5);
            for (int attempt = 0; attempt < 50 && (surface.Bounds.Width > 271 || surface.Bounds.Height > 211); attempt++)
                await Task.Delay(20);
            await Idle();
            value.BringIntoView();
            await Idle();
            var position = value.TranslatePoint(default, scroll);
            check(surface.Bounds.Width is > 0 and <= 271 && surface.Bounds.Height is > 0 and <= 211 &&
                scroll.Extent.Height > scroll.Viewport.Height && scroll.Bounds.Bottom <= layout.Children[1].Bounds.Top &&
                value.IsEffectivelyVisible && position is { } point && point.Y >= -1 &&
                point.Y + value.Bounds.Height <= scroll.Viewport.Height + 1,
                $"Narrow improvement creation scrolls its fields above the confirmation row [host={surface.Bounds}, extent={scroll.Extent}, viewport={scroll.Viewport}, value={position}/{value.Bounds}]");
            layout.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
            source.Visible = false;
            await Idle();
            check(Input(dialog, "nudVal").Parent is StackPanel { IsVisible: false },
                "An unused improvement value hides the full field row");
        }, "nudVal", "nudMin", "nudMax", "nudAug");

        using (var picker = new frmSelectCritterPower(character))
        {
            var pending = picker.ShowDialogAsync(owner);
            await Idle();
            picker.ClientSize = new System.Drawing.Size(340, 380);
            await Idle();
            var surface = (Border)picker.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var layout = (Grid)surface.Child!;
            var panes = (Grid)layout.Children[0];
            var category = surface.GetVisualDescendants().OfType<ComboBox>().Single();
            var tree = surface.GetVisualDescendants().OfType<TreeView>().Single();
            category.SelectedItem = "Paranormal";
            await Idle();
            tree.SelectedItem = tree.Items.OfType<TreeViewItem>()
                .Single(row => ((Forms.TreeNode)row.Tag!).Tag?.ToString() == "Hardened Armor");
            await Idle();
            var input = surface.GetVisualDescendants().OfType<NumericUpDown>().Single();
            var rating = Field<Forms.NumericUpDown>(picker, "nudCritterPowerRating");
            var source = surface.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativeCritterPowerSource");
            check(Grid.GetRow(panes.Children[1]) == 1 && panes.Bounds.Bottom <= layout.Children[1].Bounds.Top &&
                tree.Bounds.Height > 0 && input.IsEffectivelyVisible && input.IsEnabled &&
                rating.Minimum == input.Minimum && rating.Maximum == input.Maximum && rating.Increment == input.Increment &&
                source.Content is string reference && reference.EndsWith(" 212", StringComparison.Ordinal) &&
                input.GetVisualDescendants().OfType<Button>().Count(button =>
                    button.Name is "PART_IncreaseButton" or "PART_DecreaseButton" && button.Bounds.Width >= 9) == 2,
                "The narrow Critter Power picker shows rated power details and both spinner arrows above the actions");
            input.Value = 4;
            surface.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOKAdd")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
                picker.SelectedPower == "Hardened Armor" && picker.SelectedRating == 4 && picker.AddAgain,
                "The native Critter Power picker returns the chosen category entry and rating");
        }

        check(character.CaptureSnapshot().SequenceEqual(before),
            "Closing the specialist dialogs without accepting preserves the character");

        using (var improvement = new frmCreateImprovement(character))
        {
            var pending = improvement.ShowDialogAsync(owner);
            await Idle();
            var layout = (Border)improvement.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!;
            var typeSource = Field<Forms.ComboBox>(improvement, "cboImprovemetType");
            int physicalIndex = Array.FindIndex(typeSource.Items.Cast<ListItem>().ToArray(),
                item => item.Value == "conditionmonitorphysical");
            var type = layout.GetVisualDescendants().OfType<ComboBox>()
                .Single(input => input.Name == "NativecboImprovemetType");
            type.SelectedIndex = physicalIndex;
            await Idle();
            var value = Input(improvement, "nudVal");
            check(physicalIndex >= 0 && value.IsEffectivelyVisible &&
                !Input(improvement, "nudMin").IsEffectivelyVisible && !Input(improvement, "nudMax").IsEffectivelyVisible,
                "Selecting Physical Condition Monitor reveals only its relevant numeric field");
            const string customName = "Synthetic physical condition bonus";
            layout.GetVisualDescendants().OfType<TextBox>().Single(input => input.Name == "NativeImprovementName").Text = customName;
            value.Value = 3;
            layout.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdOK")
                .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(await pending.WaitAsync(TimeSpan.FromSeconds(5)) == Forms.DialogResult.OK &&
                character.Improvements.Any(item => item.Custom && item.CustomName == customName &&
                    item.CustomId == "conditionmonitorphysical" && item.Value == 3),
                "Native confirmation creates the canonical custom improvement with the entered value");
            var reopened = new Character();
            reopened.LoadSnapshot(character.CaptureSnapshot());
            check(reopened.Improvements.Any(item => item.Custom && item.CustomName == customName &&
                item.CustomId == "conditionmonitorphysical" && item.Value == 3),
                "The native custom improvement survives the existing character save format");
        }

        async Task CheckDialog(Forms.Form dialog, Func<Forms.Form, Task>? exercise, params string[] names)
        {
            using (dialog)
            {
                dialog.Show();
                await Idle();
                foreach (string name in names)
                {
                    var source = Field<Forms.NumericUpDown>(dialog, name);
                    var input = Input(dialog, name);
                    check((source.Parent == null || dialog is frmCreateSpell) && source.Minimum == input.Minimum && source.Maximum == input.Maximum &&
                        source.Value == input.Value && source.Increment == input.Increment &&
                        source.Enabled == input.IsEnabled && source.Visible == input.IsVisible,
                        dialog.GetType().Name + "." + name + " preserves controller state");
                    bool visible = source.Visible;
                    source.Visible = true;
                    await Idle();
                    var editor = input.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
                    check(input.IsEffectivelyVisible && TopLevel.GetTopLevel(input) != null &&
                        editor is { Bounds.Width: >= 10 } &&
                        input.GetVisualDescendants().OfType<Button>().Count(button =>
                            button.Name is "PART_IncreaseButton" or "PART_DecreaseButton" && button.Bounds.Width >= 9) == 2,
                        dialog.GetType().Name + "." + name + " displays both native arrows and the value field");
                    source.Visible = visible;
                }
                if (exercise != null)
                    await exercise(dialog);
                dialog.Close();
            }
        }
    }

    private static async Task Idle() => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;

    private static NumericUpDown Input(Forms.Form owner, string name) =>
        owner is frmCreateImprovement or frmDiceRoller or frmSelectAdvancedLifestyle or frmCreateSpell or frmMetatype
            ? ((Border)owner.Controls.OfType<Forms.NativeControlHost>().Single().NativeControl!).GetVisualDescendants()
                .OfType<NumericUpDown>().Single(input => input.Name == "native" + name)
            : (NumericUpDown)((Border)Host(owner, name).NativeControl!).Child!;

    private static Forms.NativeControlHost Host(Forms.Form owner, string name) =>
        Descendants(owner.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
            .Single(host => host.Name == "native" + name);

    private static IEnumerable<Forms.Control> Descendants(IEnumerable<Forms.Control> controls) =>
        controls.SelectMany(control => Descendants(control.Controls.Cast<Forms.Control>()).Prepend(control));
}
