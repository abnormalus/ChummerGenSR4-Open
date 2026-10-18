#nullable enable
using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;
namespace Chummer;
internal static class NativeNumberPromptChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character character, string output, Action<bool, string> check)
    {
        using (var prompt = new frmSelectNumber { Minimum = 10, Maximum = 1000000, SelectedValue = 25, Description = "Synthetic number choice" })
        {
            check(prompt.SelectedValue == 0, "Native number initial value remains an unaccepted draft");
            var pending = prompt.ShowDialogAsync(owner); var window = (NumberPromptWindow)NativeDialogHost.ActiveWindow!; await Task.Delay(100);
            check(window.Input.Value == 25 && !Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled, "Native number displays configured draft and owns modality");
            window.Input.Text = "1000000"; await Task.Delay(30);
            check(window.Input.Value == 1000000, "Native number accepts configured large integer limit");
            window.Input.Text = "20"; await Task.Delay(30);
            window.Input.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_IncreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(window.Input.Value == 21, "Native number spinner increases configured draft");
            var inputEditor = window.Input.GetVisualDescendants().OfType<TextBox>().Single();
            decimal? beforeDown = window.Input.Value;
            inputEditor.Focus();
            inputEditor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
            await Task.Delay(30);
            // Synthetic numeric-only evidence distinguishes stepping from text/focus synchronization.
            File.WriteAllText(Path.Combine(output, "native-number-input-probe.json"), System.Text.Json.JsonSerializer.Serialize(new { beforeDown, afterDown = window.Input.Value, text = window.Input.Text, editorFocused = inputEditor.IsFocused }));
            check(window.Input.Value == 20, "Native number keyboard decreases configured draft");
            window.Input.Text = "1"; await Task.Delay(30);
            check(window.Input.Value == 10, "Native number clips typed input at configured minimum");
            window.Input.Text = "10";
            var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previousTheme = nativeOwner.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    nativeOwner.RequestedThemeVariant = theme; await Task.Delay(100);
                    check(window.ActualThemeVariant == theme && window.Input.Bounds.Width >= 110, "Native shared number follows " + (theme == ThemeVariant.Dark ? "dark" : "light") + " theme at logical sizing");
                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192));
                    bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-number-prompt-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { nativeOwner.RequestedThemeVariant = previousTheme; }
            window.Accept();
            check(await pending == Forms.DialogResult.OK && prompt.DialogResult == Forms.DialogResult.OK && prompt.SelectedValue == 10, "Native number returns only explicitly accepted integer");
        }
        using (var prompt = new frmSelectNumber { Minimum = 1, Maximum = 5, SelectedValue = 3 })
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (NumberPromptWindow)NativeDialogHost.ActiveWindow!; window.Input.Value = 4; window.Close();
            check(await pending == Forms.DialogResult.Cancel && prompt.SelectedValue == 0, "Native number title-bar cancellation discards configured and edited drafts");
        }
        using (var prompt = new frmSelectNumber { Minimum = 5, Maximum = 20, AllowCancel = false })
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (NumberPromptWindow)NativeDialogHost.ActiveWindow!; window.Close();
            check(!pending.IsCompleted && window.IsVisible && !window.CancelButton.IsEnabled, "Native required number rejects ordinary close without accepting a default");
            NativeDialogHost.CancelAll();
            check(await pending == Forms.DialogResult.Cancel && prompt.SelectedValue == 0 && NativeDialogHost.Count == 0 && Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled, "Native required number shutdown unwinds without accepting or trapping owner");
        }
        using (var prompt = new frmSelectNumber { Minimum = -20, Maximum = -1, SelectedValue = -5, AllowCancel = false })
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (NumberPromptWindow)NativeDialogHost.ActiveWindow!; window.Input.Text = "-8"; await Task.Delay(30); window.Accept();
            check(await pending == Forms.DialogResult.OK && prompt.SelectedValue == -8, "Native mandatory number accepts configured negative integer ranges");
        }
        var stackCharacter = new Character(); var stackGear = new Gear(stackCharacter) { Name = "Synthetic stack", Cost = "100", Quantity = 2 };
        stackCharacter.Gear.Add(stackGear); stackCharacter.Nuyen = 1000;
        using (var tree = new Forms.TreeView())
        using (var menu = new Forms.ContextMenuStrip())
        {
            var node = new Forms.TreeNode(stackGear.DisplayName) { Tag = stackGear.InternalId }; tree.Nodes.Add(node); tree.SelectedNode = node; int changes = 0;
            GearStacks.Attach(owner, stackCharacter, tree, _ => stackGear, () => changes++, true, menu);
            var command = menu.Items.OfType<Forms.ToolStripMenuItem>().Single(item => item.Name == "createGearStack");
            Avalonia.Threading.Dispatcher.UIThread.Post(() => NativeDialogHost.ActiveWindow!.Close()); command.PerformClick();
            check(stackGear.Quantity == 2 && stackCharacter.Nuyen == 1000 && stackCharacter.ExpenseEntries.Count == 0 && changes == 0, "Native shared number stack cancellation preserves stock funds and notifications");
            Avalonia.Threading.Dispatcher.UIThread.Post(() => { var window = (NumberPromptWindow)NativeDialogHost.ActiveWindow!; window.Input.Value = 5; window.Accept(); }); command.PerformClick();
            check(stackGear.Quantity == 5 && stackCharacter.Nuyen == 700 && stackCharacter.ExpenseEntries.Single().Undo.Qty == 3 && changes == 1, "Native shared number stack purchase sets total and records additional-unit undo");
        }
        var definition = XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Medkit']")!.CloneNode(true);
        definition["cost"]!.InnerText = "Variable(100-200)";
        int money = character.Nuyen, gearCount = character.Gear.Count, expenseCount = character.ExpenseEntries.Count;
        Avalonia.Threading.Dispatcher.UIThread.Post(() => { var window = (NumberPromptWindow)NativeDialogHost.ActiveWindow!; window.Input.Value = 125; window.Accept(); });
        var gear = new Gear(character); gear.Create(definition, character, new Forms.TreeNode(), 1, new List<Weapon>(), new List<Forms.TreeNode>(), blnAddImprovements: false);
        check(gear.InternalId != Guid.Empty.ToString() && gear.TotalCost == 125, "Native required variable-price gear uses the accepted canonical cost");
        Avalonia.Threading.Dispatcher.UIThread.Post(() => NativeDialogHost.CancelAll());
        var cancelled = new Gear(character); cancelled.Create(definition, character, new Forms.TreeNode(), 1, new List<Weapon>(), new List<Forms.TreeNode>(), blnAddImprovements: false);
        check(cancelled.InternalId == Guid.Empty.ToString() && character.Nuyen == money && character.Gear.Count == gearCount && character.ExpenseEntries.Count == expenseCount, "Native variable-price shutdown does not manufacture zero-cost inventory or payment");
    }
}
