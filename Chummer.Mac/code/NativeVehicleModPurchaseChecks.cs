#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;
namespace Chummer;

// Exercise the four vehicle-tree callers, complementing the two existing carried
// weapon grip paths. The real controllers own charging, insertion and expense undo.
internal static class NativeVehicleModPurchaseChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static void Invoke(Forms.Form editor, string method) => editor.GetType().GetMethod(method, Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
    private static Task Reply(Forms.DialogResult answer)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var limit = DateTime.UtcNow.AddSeconds(20);
                while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                { if (DateTime.UtcNow >= limit) throw new TimeoutException("Modification confirmation did not open."); await Task.Delay(10); }
                var message = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last();
                message.DialogResult = answer; message.Close(); completion.TrySetResult();
            }
            catch (Exception error) { NativeDialogHost.CancelAll(); completion.TrySetException(error); }
        });
        return completion.Task;
    }
    internal static async Task RunAsync(Forms.Form owner, Character source, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career; character.Nuyen = 100000;
            var books = character.Options.Books.ToArray(); bool capacity = character.Options.EnforceCapacity;
            character.Options.EnforceCapacity = false;
            if (!character.Options.Books.Contains("AR")) character.Options.Books.Add("AR");
            if (!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true, MdiParent = owner } : new frmCreate(character) { Loading = true, MdiParent = owner };
            editor.Show();
            try
            {
                var tree = Field<Forms.TreeView>(editor, "treVehicles");
                var vehicle = new Vehicle(character); var vehicleNode = new Forms.TreeNode();
                vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!, vehicleNode, null, null, null, null, null, false);
                character.Vehicles.Add(vehicle); tree.Nodes[0].Nodes.Add(vehicleNode);
                var weapon = new Weapon(character); var weaponNode = new Forms.TreeNode();
                weapon.Create(XmlManager.Instance.Load("weapons.xml").SelectSingleNode("/chummer/weapons/weapon[name='AK-97']")!, character, weaponNode, null, null, null);
                weapon.VehicleMounted = true; weapon.WeaponName = "Synthetic mounted rifle";
                vehicle.Weapons.Add(weapon); vehicleNode.Nodes.Add(weaponNode);
                Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabVehicles");
                foreach (bool mounted in new[] { false, true })
                {
                    string description = "Native " + (career ? "career" : "creation") + (mounted ? " mounted weapon" : " vehicle") + " modification";
                    string handler = mounted ? "tsVehicleAddWeaponModification_Click" : "tsVehicleAddMod_Click";
                    string name = mounted ? "Personalized Grip" : "Off-Road Tires";
                    Forms.TreeNode target = mounted ? weaponNode : vehicleNode;
                    int Count() => mounted ? weapon.WeaponMods.Count : vehicle.Mods.Count;
                    void Buy(bool accept, bool free = false)
                    {
                        tree.SelectedNode = target;
                        Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                        {
                            var window = (VehicleModPickerWindow)NativeDialogHost.ActiveWindow!;
                            window.SearchBox.Text = name;
                            window.Model.SelectedEntry = window.Model.Items.Single(item => item.Name == name);
                            window.Model.Rating = mounted ? 0 : 2;
                            window.Model.Markup = career ? 25 : 0; window.Model.FreeCost = free;
                            if (accept) window.Accept(false); else window.Close();
                        });
                        Invoke(editor, handler);
                    }
                    int count = Count(), money = character.Nuyen, entries = character.ExpenseEntries.Count;
                    Buy(false);
                    check(Count() == count && character.Nuyen == money && character.ExpenseEntries.Count == entries, description + " cancellation preserves inventory funds and ledger");
                    Buy(true);
                    string id = mounted ? weapon.WeaponMods.Last().InternalId : vehicle.Mods.Last().InternalId;
                    int expected = career ? (mounted ? 125 : 1000) : 0;
                    check(Count() == count + 1 && character.Nuyen == money - expected && (mounted ? weapon.WeaponMods.Last().Name == name : vehicle.Mods.Last().Rating == 2), description + " paid selection retains canonical identity quantity and markup accounting");
                    var reloaded = new Character(); reloaded.LoadSnapshot(character.CaptureSnapshot());
                    var savedVehicle = reloaded.Vehicles.Single(item => item.InternalId == vehicle.InternalId);
                    check(mounted ? savedVehicle.Weapons.Single(item => item.InternalId == weapon.InternalId).WeaponMods.Any(item => item.InternalId == id && item.Name == name)
                        : savedVehicle.Mods.Any(item => item.InternalId == id && item.Rating == 2), description + " persists through the legacy character format");
                    if (!career) continue;
                    var purchase = character.ExpenseEntries.Single(item => item.Undo?.ObjectId == id);
                    check(purchase.Amount == -expected && purchase.Undo.NuyenType == (mounted ? NuyenExpenseType.AddVehicleWeaponMod : NuyenExpenseType.AddVehicleMod), description + " records one expense with matching canonical undo identity");
                    int paid = character.Nuyen;
                    Buy(true, true);
                    check(Count() == count + 2 && character.Nuyen == paid && character.ExpenseEntries.Count == entries + 1, description + " free purchase preserves funds and expense count");
                    void SelectExpense()
                    {
                        ((frmCareer)editor).PopulateExpenseList();
                        foreach (Forms.ListViewItem row in Field<Forms.ListView>(editor, "lstNuyen").Items) row.Selected = row.SubItems[3].Text == purchase.InternalId;
                    }
                    SelectExpense(); var reply = Reply(Forms.DialogResult.Cancel); Invoke(editor, "tsUndoNuyenExpense_Click"); await reply;
                    check(Count() == count + 2 && character.Nuyen == paid && character.ExpenseEntries.Contains(purchase), description + " canceled undo preserves purchase");
                    SelectExpense(); reply = Reply(Forms.DialogResult.Yes); Invoke(editor, "tsUndoNuyenExpense_Click"); await reply;
                    check(Count() == count + 1 && character.Nuyen == money && !character.ExpenseEntries.Contains(purchase), description + " accepted undo refunds only the paid purchase");
                    character.Nuyen = 0; int remaining = Count(); entries = character.ExpenseEntries.Count;
                    reply = Reply(Forms.DialogResult.OK); Buy(true); await reply;
                    check(Count() == remaining && character.Nuyen == 0 && character.ExpenseEntries.Count == entries, description + " unaffordable purchase preserves owned inventory funds and ledger");
                    character.Nuyen = money;
                }
            }
            finally
            {
                character.Options.Books.Clear(); character.Options.Books.AddRange(books); character.Options.EnforceCapacity = capacity;
                editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close();
            }
        }
    }
    // A required price can be unwound by Quit. The production creation method
    // signals that cancellation with an empty identity; its caller must not insert
    // the unfinished modification. This regression is intentionally added before
    // changing either caller, so the platform run can establish the baseline.
    internal static async Task RunVariableCancellationAsync(Forms.Form owner, Character source, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career; character.Nuyen = 100000;
            var books = character.Options.Books.ToArray();
            if (!character.Options.Books.Contains("AR")) character.Options.Books.Add("AR");
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true, MdiParent = owner } : new frmCreate(character) { Loading = true, MdiParent = owner };
            editor.Show();
            try
            {
                var vehicle = new Vehicle(character); var node = new Forms.TreeNode();
                vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!, node, null, null, null, null, null, false);
                character.Vehicles.Add(vehicle);
                var tree = Field<Forms.TreeView>(editor, "treVehicles"); tree.Nodes[0].Nodes.Add(node); tree.SelectedNode = node;
                Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabVehicles");
                if (!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
                int initialCount = vehicle.Mods.Count, initialMoney = character.Nuyen;
                var acceptance = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        var picker = (VehicleModPickerWindow)NativeDialogHost.ActiveWindow!;
                        picker.SearchBox.Text = "Generic Mod";
                        picker.Model.SelectedEntry = picker.Model.Items.Single(item => item.Name == "Generic Mod"); picker.Accept(false);
                        var limit = DateTime.UtcNow.AddSeconds(20);
                        while (NativeDialogHost.ActiveWindow is not NumberPromptWindow)
                        { if (DateTime.UtcNow >= limit) throw new TimeoutException("Accepted variable modification price did not open."); await Task.Delay(10); }
                        var price = (NumberPromptWindow)NativeDialogHost.ActiveWindow!; price.Input.Value = 1234; price.Accept();
                        while (!Forms.Application.OpenForms.OfType<frmSelectText>().Any())
                        { if (DateTime.UtcNow >= limit) throw new TimeoutException("Variable modification detail did not open."); await Task.Delay(10); }
                        var details = Forms.Application.OpenForms.OfType<frmSelectText>().Last(); details.SelectedValue = "Synthetic compartment";
                        Field<Forms.Button>(details, "cmdOK").PerformClick(); acceptance.TrySetResult();
                    }
                    catch (Exception error) { NativeDialogHost.CancelAll(); acceptance.TrySetException(error); }
                });
                Invoke(editor, "tsVehicleAddMod_Click"); await acceptance.Task;
                var chosen = vehicle.Mods.Last();
                check(vehicle.Mods.Count == initialCount + 1 && chosen.Name == "Generic Mod" && chosen.Cost == "1234" && chosen.Extra == "Synthetic compartment" && character.Nuyen == initialMoney - (career ? 1234 : 0),
                    "Native " + (career ? "career" : "creation") + " accepted variable vehicle price and detail retain explicit cost and accounting");
                var restored = new Character(); restored.LoadSnapshot(character.CaptureSnapshot());
                var restoredMod = restored.Vehicles.Single(item => item.InternalId == vehicle.InternalId).Mods.Single(item => item.InternalId == chosen.InternalId);
                check(restoredMod.Cost == "1234" && restoredMod.Extra == "Synthetic compartment",
                    "Native accepted variable vehicle price and detail survive the existing character format");
                tree.SelectedNode = node;
                byte[] before = character.CaptureSnapshot(); var existing = vehicle.Mods.ToArray();
                var cancellation = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        var picker = (VehicleModPickerWindow)NativeDialogHost.ActiveWindow!;
                        picker.SearchBox.Text = "Smuggling Compartment, Shielded";
                        picker.Model.SelectedEntry = picker.Model.Items.Single(item => item.Name == "Smuggling Compartment, Shielded");
                        picker.Accept(false);
                        var limit = DateTime.UtcNow.AddSeconds(20);
                        while (NativeDialogHost.ActiveWindow is not NumberPromptWindow)
                        { if (DateTime.UtcNow >= limit) throw new TimeoutException("Variable modification price did not open."); await Task.Delay(10); }
                        ((NumberPromptWindow)NativeDialogHost.ActiveWindow!).CancelForShutdown();
                        cancellation.TrySetResult();
                    }
                    catch (Exception error) { NativeDialogHost.CancelAll(); cancellation.TrySetException(error); }
                });
                Exception? failure = null;
                try { Invoke(editor, "tsVehicleAddMod_Click"); }
                catch (TargetInvocationException error) { failure = error.InnerException ?? error; }
                await cancellation.Task;
                check(failure == null && character.CaptureSnapshot().AsSpan().SequenceEqual(before) && vehicle.Mods.SequenceEqual(existing),
                    "Native " + (career ? "career" : "creation") + " canceled required vehicle modification price preserves model and inventory references");
            }
            finally
            {
                character.Options.Books.Clear(); character.Options.Books.AddRange(books);
                editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close();
            }
        }
    }

}
