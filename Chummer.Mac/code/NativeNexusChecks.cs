#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeNexusChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static void Invoke(Forms.Form editor, string method) => editor.GetType().GetMethod(method, Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
    private static Task Reply(Forms.DialogResult answer)
    {
        var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var limit = DateTime.UtcNow.AddSeconds(20);
                while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                { if (DateTime.UtcNow >= limit) throw new TimeoutException("Nexus confirmation did not open."); await Task.Delay(10); }
                var message = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last(); message.DialogResult = answer; message.Close(); done.TrySetResult();
            }
            catch (Exception error) { NativeDialogHost.CancelAll(); done.TrySetException(error); }
        });
        return done.Task;
    }
    internal static async Task RunAsync(Forms.Form owner, Character character, string output, Action<bool, string> check)
    {
        var before = character.CaptureSnapshot();
        using (var prompt = new frmSelectNexus(character, true))
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (NexusWindow)NativeDialogHost.ActiveWindow!; await Task.Delay(100);
            check(window.ProcessorInput.Minimum == 10 && window.ProcessorInput.Maximum == 50 && window.SystemInput.Maximum == 10 && window.PersonaInput.Maximum == 50 && window.SignalInput.Value == 3,
                "Native nexus retains numeric ranges and initial signal rating");
            window.ProcessorInput.Text = "20"; await Task.Delay(30);
            check(window.Model.Processor == 20 && window.Model.Quote!.Cost == 1725, "Native nexus typed processor updates its characterized price");
            window.SystemInput.Value = 10; await Task.Delay(30);
            check(window.PersonaInput.Minimum == 30 && window.PersonaInput.Value == 30, "Native nexus system change raises and clamps the persona minimum");
            window.SystemInput.Value = 1; await Task.Delay(30);
            check(window.PersonaInput.Minimum == 3 && window.PersonaInput.Value == 30, "Native nexus lower system retains the chosen higher persona limit");
            window.ProcessorInput.Text = ""; await Task.Delay(30);
            check(!window.AcceptButton.IsEnabled && window.Model.Cost == "" && window.Model.Selection == null, "Native nexus cleared input blocks construction and clears stale price");
            window.ProcessorInput.Text = "20";
            window.ResponseInput.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_IncreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent)); await Task.Delay(30);
            check(window.Model.Response == 2, "Native nexus standard spinner changes the component rating");
            var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previous = nativeOwner.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    nativeOwner.RequestedThemeVariant = theme; await Task.Delay(100);
                    check(window.ActualThemeVariant == theme && window.ProcessorInput.Bounds.Width >= 110, "Native nexus follows " + theme + " owner theme with usable numeric layout");
                    using var image = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192));
                    image.Render(window); image.Save(Path.Combine(output, "native-nexus-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { nativeOwner.RequestedThemeVariant = previous; }
            window.Model.Free = true; window.Accept();
            check(await pending == Forms.DialogResult.OK && prompt.FreeCost && prompt.SelectedNexus.Cost == "0" && prompt.SelectedNexus.Children.Count == 5,
                "Native nexus constructs its legacy module tree only from an accepted configuration");
        }
        using (var prompt = new frmSelectNexus(character))
        {
            var pending = prompt.ShowDialogAsync(owner); ((NexusWindow)NativeDialogHost.ActiveWindow!).Close();
            check(await pending == Forms.DialogResult.Cancel && !prompt.FreeCost, "Native nexus title close returns canceled purchase intent");
        }
        check(character.CaptureSnapshot().AsSpan().SequenceEqual(before), "Native nexus browsing and accepted draft do not insert owned Gear");
        foreach (bool career in new[] { false, true })
        {
            var copy = new Character(); copy.LoadSnapshot(before); copy.Created = career; copy.Nuyen = 100000;
            using Forms.Form editor = career ? new frmCareer(copy) { Loading = true, MdiParent = owner } : new frmCreate(copy) { Loading = true, MdiParent = owner }; editor.Show();
            try
            {
                var vehicle = new Vehicle(copy); var vehicleNode = new Forms.TreeNode();
                vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!, vehicleNode, null, null, null, null, null, false);
                copy.Vehicles.Add(vehicle); var tree = Field<Forms.TreeView>(editor, "treVehicles"); tree.Nodes[0].Nodes.Add(vehicleNode);
                foreach (bool mounted in new[] { false, true })
                {
                    var inventory = mounted ? vehicle.Gear : copy.Gear;
                    string description = "Native " + (career ? "career" : "creation") + (mounted ? " vehicle" : " carried") + " nexus";
                    void Buy(bool accept, bool free = false)
                    {
                        if (mounted) tree.SelectedNode = vehicleNode;
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            var window = (NexusWindow)NativeDialogHost.ActiveWindow!; window.Model.Free = free;
                            if (accept) window.Accept(); else window.Close();
                        });
                        Invoke(editor, mounted ? "tsVehicleAddNexus_Click" : "tsGearAddNexus_Click");
                    }
                    int count = inventory.Count, money = copy.Nuyen, entries = copy.ExpenseEntries.Count;
                    Buy(false); check(inventory.Count == count && copy.Nuyen == money && copy.ExpenseEntries.Count == entries, description + " cancellation preserves stock funds and ledger");
                    Buy(true); var nexus = inventory.Last();
                    check(inventory.Count == count + 1 && nexus.Children.Count == 5 && nexus.Cost == "975" && copy.Nuyen == money - (career ? 975 : 0), description + " accepted purchase retains default cost and modules");
                    var restored = new Character(); restored.LoadSnapshot(copy.CaptureSnapshot()); var savedInventory = mounted ? restored.Vehicles.Single(item => item.InternalId == vehicle.InternalId).Gear : restored.Gear;
                    var saved = savedInventory.Single(item => item.InternalId == nexus.InternalId);
                    check(saved.Cost == nexus.Cost && saved.Name == nexus.Name && saved.Source == "UN" && saved.Page == "50" && saved.Children.Count == 5 &&
                        saved.Children.Select(item => (item.InternalId, item.Name, item.Category, item.Cost, item.Avail, item.Source, item.Page)).SequenceEqual(
                            nexus.Children.Select(item => (item.InternalId, item.Name, item.Category, item.Cost, item.Avail, item.Source, item.Page))), description + " module identities names costs and availability survive legacy persistence");
                    if (!career) continue;
                    var purchase = copy.ExpenseEntries.Single(item => item.Undo?.ObjectId == nexus.InternalId);
                    check(purchase.Amount == -975 && purchase.Undo.NuyenType == (mounted ? NuyenExpenseType.AddVehicleGear : NuyenExpenseType.AddGear), description + " records matching expense undo");
                    int paid = copy.Nuyen;
                    if (mounted) { Buy(true, true); check(inventory.Count == count + 2 && copy.Nuyen == paid && copy.ExpenseEntries.Count == entries + 1, description + " free purchase adds zero-cost inventory without a new expense"); }
                    void SelectExpense()
                    {
                        ((frmCareer)editor).PopulateExpenseList();
                        foreach (Forms.ListViewItem row in Field<Forms.ListView>(editor, "lstNuyen").Items) row.Selected = row.SubItems[3].Text == purchase.InternalId;
                    }
                    SelectExpense(); var answer = Reply(Forms.DialogResult.Cancel); Invoke(editor, "tsUndoNuyenExpense_Click"); await answer;
                    check(inventory.Contains(nexus) && copy.Nuyen == paid && copy.ExpenseEntries.Contains(purchase), description + " canceled undo preserves purchase");
                    SelectExpense(); answer = Reply(Forms.DialogResult.Yes); Invoke(editor, "tsUndoNuyenExpense_Click"); await answer;
                    check(!inventory.Contains(nexus) && copy.Nuyen == money && !copy.ExpenseEntries.Contains(purchase), description + " accepted undo refunds and removes only the paid nexus");
                    copy.Nuyen = 0; int remaining = inventory.Count; entries = copy.ExpenseEntries.Count;
                    answer = Reply(Forms.DialogResult.OK); Buy(true); await answer;
                    check(inventory.Count == remaining && copy.Nuyen == 0 && copy.ExpenseEntries.Count == entries, description + " unaffordable purchase preserves inventory and ledger"); copy.Nuyen = money;
                }
            }
            finally { editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close(); }
        }
    }
}
