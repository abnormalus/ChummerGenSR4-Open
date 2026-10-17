#nullable enable
using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using System.Linq;
using System.Reflection;
using System.Collections.Generic;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;
namespace Chummer;
internal static class NativeGearQuantityChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool, string> check)
    {
        using (var dialog = new frmSelectQuantity(frmSelectQuantity.Operation.Add, 8))
        {
            var pending = dialog.ShowDialogAsync(owner); var window = (GearQuantityWindow)NativeDialogHost.ActiveWindow!;
            await Task.Delay(100);
            window.QuantityInput.Text = "4"; await Task.Delay(30);
            check(window.QuantityInput.Value == 4, "Native quantity accepts typed integer input");
            window.QuantityInput.GetVisualDescendants().OfType<Button>().Single(b => b.Name == "PART_IncreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(window.QuantityInput.Value == 5, "Native quantity spinner increases quantity");
            window.QuantityInput.GetVisualDescendants().OfType<TextBox>().Single().RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
            check(window.QuantityInput.Value == 4, "Native quantity keyboard decreases quantity");
            window.QuantityInput.Text = "20"; window.MarkupInput.Text = "-120"; await Task.Delay(50);
            check(window.QuantityInput.Value == 8 && window.MarkupInput.Value == -99, "Native quantity and markup preserve domain clipping");
            var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
            var previousTheme = nativeOwner.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    nativeOwner.RequestedThemeVariant = theme; window.Width = 460; await Task.Delay(100);
                    check(window.ActualThemeVariant == theme && window.QuantityInput.Bounds.Width >= 110, "Native quantity layout follows " + (theme == ThemeVariant.Dark ? "dark" : "light") + " owner theme");
                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192));
                    bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-gear-quantity-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { nativeOwner.RequestedThemeVariant = previousTheme; }
            window.FreeInput.IsChecked = true;
            check(!window.MarkupInput.IsEnabled, "Native free quantity disables markup editing");
            window.Accept();
            check(await pending == Forms.DialogResult.OK && dialog.Quantity == 8 && dialog.FreeCost && dialog.Markup == -99, "Native quantity returns accepted purchase draft");
        }
        using (var dialog = new frmSelectQuantity(frmSelectQuantity.Operation.Remove, 3))
        {
            var pending = dialog.ShowDialogAsync(owner); var window = (GearQuantityWindow)NativeDialogHost.ActiveWindow!;
            check(!window.FreeInput.IsEnabled && !window.MarkupInput.IsEnabled && window.QuantityInput.Maximum == 3, "Native remove quantity disables purchase options and limits stock");
            window.QuantityInput.Value = 2; window.Close();
            check(await pending == Forms.DialogResult.Cancel && dialog.Quantity == 0 && !dialog.FreeCost && dialog.Markup == 0 && Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled, "Native quantity title-bar cancellation discards draft and restores owner");
        }
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = true; character.Nuyen = 10000;
        using var editor = new frmCareer(character) { Loading = true }; editor.MdiParent = owner; editor.Show();
        try
        {
            var gear = new Gear(character); var node = new Forms.TreeNode();
            gear.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Medkit']")!, character, node, 1, new List<Weapon>(), new List<Forms.TreeNode>(), blnAddImprovements: false);
            gear.Quantity = 2; character.Gear.Add(gear);
            var tree = Field<Forms.TreeView>(editor, "treGear"); tree.Nodes[0].Nodes.Add(node); tree.SelectedNode = node;
            void Invoke(string method) => editor.GetType().GetMethod(method, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
            void QuantityCommand(string method, int count, bool accept)
            {
                Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                    var dialog = (GearQuantityWindow)NativeDialogHost.ActiveWindow!; dialog.QuantityInput.Value = count;
                    if (accept) dialog.Accept(); else dialog.Close();
                });
                Invoke(method);
            }
            int money = character.Nuyen, expenses = character.ExpenseEntries.Count;
            QuantityCommand("cmdGearIncreaseQty_Click", 3, false);
            check(gear.Quantity == 2 && character.Nuyen == money && character.ExpenseEntries.Count == expenses, "Native career quantity cancellation preserves inventory funds and expenses");
            int unitCost = Convert.ToInt32(gear.TotalCost / gear.Quantity);
            QuantityCommand("cmdGearIncreaseQty_Click", 3, true);
            check(gear.Quantity == 5 && character.Nuyen == money - unitCost * 3 && character.ExpenseEntries.Count == expenses + 1, "Native career quantity purchase preserves unit accounting and expense creation");
            var purchaseExpense = character.ExpenseEntries.Last();
            // An unaffordable increase must not remove bonuses belonging to stock already owned.
            var existingImprovement = new Improvement { ImproveSource = Improvement.ImprovementSource.Gear, SourceName = gear.InternalId };
            character.Improvements.Add(existingImprovement); character.Nuyen = 0;
            int beforeRejectedExpenses = character.ExpenseEntries.Count;
            var answered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
            Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
                try
                {
                    var deadline = DateTime.UtcNow.AddSeconds(15);
                    while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                    {
                        if (DateTime.UtcNow >= deadline) throw new TimeoutException("Quantity affordability notice did not open.");
                        await Task.Delay(20);
                    }
                    var notice = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last();
                    notice.DialogResult = Forms.DialogResult.OK; notice.Close(); answered.TrySetResult();
                }
                catch (Exception error) { answered.TrySetException(error); }
            });
            QuantityCommand("cmdGearIncreaseQty_Click", 1, true); await answered.Task;
            check(gear.Quantity == 5 && character.Nuyen == 0 && character.ExpenseEntries.Count == beforeRejectedExpenses && character.Improvements.Contains(existingImprovement), "Native unaffordable quantity purchase preserves owned gear improvements and accounting");
            check(purchaseExpense.Undo != null && purchaseExpense.Undo.Qty == 3, "Native quantity expense undo records purchased units rather than previous stock");
            editor.PopulateExpenseList();
            var expenseRow = Field<Forms.ListView>(editor, "lstNuyen").Items.Cast<Forms.ListViewItem>().Single(row => row.SubItems[3].Text == purchaseExpense.InternalId);
            expenseRow.Selected = true;
            async Task UndoAsync(Forms.DialogResult answer)
            {
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
                    try
                    {
                        var deadline = DateTime.UtcNow.AddSeconds(15);
                        while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                        { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Quantity undo confirmation did not open."); await Task.Delay(20); }
                        var notice = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last(); notice.DialogResult = answer; notice.Close(); completion.TrySetResult();
                    }
                    catch (Exception error) { completion.TrySetException(error); }
                });
                Invoke("tsUndoNuyenExpense_Click"); await completion.Task;
            }
            await UndoAsync(Forms.DialogResult.Cancel);
            check(gear.Quantity == 5 && character.Nuyen == 0 && character.ExpenseEntries.Contains(purchaseExpense), "Native quantity expense undo cancellation preserves stock funds and expense");
            await UndoAsync(Forms.DialogResult.Yes);
            check(gear.Quantity == 2 && character.Nuyen == unitCost * 3 && !character.ExpenseEntries.Contains(purchaseExpense), "Native quantity expense undo removes purchased units and refunds once");
            tree.SelectedNode = node;
            QuantityCommand("cmdGearIncreaseQty_Click", 3, true);
            QuantityCommand("cmdGearReduceQty_Click", 2, false);
            check(gear.Quantity == 5, "Native career quantity removal cancellation preserves stock");
            QuantityCommand("cmdGearReduceQty_Click", 2, true);
            check(gear.Quantity == 3 && character.Gear.Contains(gear), "Native career quantity removal updates canonical stock");
            QuantityCommand("cmdGearReduceQty_Click", 3, true);
            check(!character.Gear.Contains(gear) && node.Parent == null, "Native career quantity removal to zero removes model and row");
        }
        finally { editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false); editor.Close(); }

    }
}
