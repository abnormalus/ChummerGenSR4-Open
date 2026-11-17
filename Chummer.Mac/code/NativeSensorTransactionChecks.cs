#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Separate bounded native suite: actual purchase, sale and undo callers retain
// their own accounting. Fixed expected prices do not use the purchase calculator.
internal static class NativeSensorTransactionChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static void Invoke(Forms.Form editor, string method) => editor.GetType().GetMethod(method, Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
    internal static async Task RunAsync(Forms.Form owner, Character source, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career; character.Nuyen = 100000;
            var books = character.Options.Books.ToArray(); bool capacity = character.Options.EnforceCapacity;
            character.Options.EnforceCapacity = false;
            foreach (string book in new[] { "AR", "SR4" }) if (!character.Options.Books.Contains(book)) character.Options.Books.Add(book);
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true, MdiParent = owner } : new frmCreate(character) { Loading = true, MdiParent = owner };
            editor.Show();
            try
            {
                var tree = Field<Forms.TreeView>(editor, "treVehicles"); var vehicle = new Vehicle(character); var node = new Forms.TreeNode();
                vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!, node, null, null, null, null, null, false);
                var sensor = new Gear(character); var sensorNode = new Forms.TreeNode();
                sensor.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Vehicle Sensor' and category='Sensors']")!, character,
                    sensorNode, 0, new List<Weapon>(), new List<Forms.TreeNode>());
                sensor.Cost = "0"; vehicle.Gear.Add(sensor); node.Nodes.Add(sensorNode); character.Vehicles.Add(vehicle); tree.Nodes[0].Nodes.Add(node);
                Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabVehicles");
                void Clean() => editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
                bool Sensor(bool increased) => ReferenceEquals(vehicle.Gear.Single(), sensor) && sensor.Name == (increased ? "Extra-Large Vehicle Sensor" : "Vehicle Sensor") &&
                    sensor.Capacity == (increased ? "30" : "12") && sensor.Signal == (increased ? 6 : 5) && sensorNode.Text == sensor.Name;
                void Buy(bool accept)
                {
                    tree.SelectedNode = node;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var picker = (VehicleModPickerWindow)NativeDialogHost.ActiveWindow!;
                        picker.SearchBox.Text = "Improved Sensor Array";
                        picker.Model.SelectedEntry = picker.Model.Items.Single(x => x.Name == "Improved Sensor Array");
                        picker.Model.Rating = 0; picker.Model.Markup = career ? 25 : 0;
                        if (accept) picker.Accept(false); else picker.Close();
                    });
                    Invoke(editor, "tsVehicleAddMod_Click");
                }
                string context = " career=" + career; Clean(); byte[] before = character.CaptureSnapshot(); int money = character.Nuyen, entries = character.ExpenseEntries.Count;
                Buy(false);
                check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()) && !Field<bool>(editor, "_blnIsDirty") && Sensor(false), "Native sensor purchase cancellation preserves full state" + context);
                if (career)
                {
                    character.Nuyen = 0; before = character.CaptureSnapshot();
                    var insufficient = Reply(Forms.DialogResult.OK); Buy(true); await insufficient;
                    check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()) && Sensor(false) && vehicle.Mods.Count == 0 && !Field<bool>(editor, "_blnIsDirty"),
                        "Native unaffordable sensor purchase preserves full state before transition");
                    character.Nuyen = money;
                }
                Buy(true); var mod = vehicle.Mods.Single(); string id = mod.InternalId;
                check(Sensor(true) && mod.Name == "Improved Sensor Array" && Field<bool>(editor, "_blnIsDirty"), "Native sensor purchase installs canonical mod and raises sensor" + context);
                check(character.Nuyen == money - (career ? 1250 : 0) && character.ExpenseEntries.Count == entries + (career ? 1 : 0), "Native sensor purchase retains fixed markup and ledger accounting" + context);
                var restored = new Character(); restored.LoadSnapshot(character.CaptureSnapshot());
                var saved = restored.Vehicles.Single(v => v.InternalId == vehicle.InternalId);
                check(saved.Mods.Single().InternalId == id && saved.Gear.Single().InternalId == sensor.InternalId && saved.Gear.Single().Name == "Extra-Large Vehicle Sensor", "Native sensor purchase retains mod and sensor identity in existing format" + context);
                if (!career) continue;
                var purchase = character.ExpenseEntries.Single(x => x.Undo?.ObjectId == id);
                check(purchase.Amount == -1250 && purchase.Undo.NuyenType == NuyenExpenseType.AddVehicleMod, "Native sensor purchase records canonical undo identity");
                void SelectExpense()
                {
                    ((frmCareer)editor).PopulateExpenseList();
                    foreach (Forms.ListViewItem row in Field<Forms.ListView>(editor, "lstNuyen").Items) row.Selected = row.SubItems[3].Text == purchase.InternalId;
                }
                Task Reply(Forms.DialogResult answer)
                {
                    var done = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
                    {
                        try
                        {
                            var limit = DateTime.UtcNow.AddSeconds(20);
                            while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                            { if (DateTime.UtcNow >= limit) throw new TimeoutException("Sensor confirmation did not open."); await Task.Delay(10); }
                            var message = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last(); message.DialogResult = answer; message.Close(); done.SetResult();
                        }
                        catch (Exception error) { NativeDialogHost.CancelAll(); done.SetException(error); }
                    });
                    return done.Task;
                }
                Clean(); SelectExpense(); before = character.CaptureSnapshot(); var reply = Reply(Forms.DialogResult.No); Invoke(editor, "tsUndoNuyenExpense_Click"); await reply;
                check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()) && Sensor(true) && !Field<bool>(editor, "_blnIsDirty"), "Native sensor undo cancellation preserves full purchase");
                SelectExpense(); reply = Reply(Forms.DialogResult.Yes); Invoke(editor, "tsUndoNuyenExpense_Click"); await reply;
                check(Sensor(false) && vehicle.Mods.Count == 0 && character.Nuyen == money && !character.ExpenseEntries.Contains(purchase), "Native sensor undo lowers sensor removes canonical mod and refunds1250");
                Buy(true); mod = vehicle.Mods.Single(); tree.SelectedNode = new CommonFunctions(character).FindNode(mod.InternalId, tree);
                void Sell(bool accept)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var prompt = (NumberPromptWindow)NativeDialogHost.ActiveWindow!; prompt.Input.Value = 50;
                        if (accept) prompt.Accept(); else prompt.Close();
                    });
                    Invoke(editor, "tsVehicleSell_Click");
                }
                Clean(); before = character.CaptureSnapshot(); Sell(false);
                check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()) && Sensor(true) && !Field<bool>(editor, "_blnIsDirty"), "Native sensor sale cancellation preserves full purchase");
                int paid = character.Nuyen; entries = character.ExpenseEntries.Count; Sell(true);
                check(Sensor(false) && vehicle.Mods.Count == 0 && character.Nuyen == paid + 500 && character.ExpenseEntries.Count == entries + 1 && character.ExpenseEntries.Last().Amount == 500,
                    "Native sensor50percent sale lowers sensor removes mod and credits fixed500");
                check(Field<bool>(editor, "_blnIsDirty"), "Native accepted sensor sale marks the character changed");
            }
            finally
            {
                character.Options.Books.Clear(); character.Options.Books.AddRange(books); character.Options.EnforceCapacity = capacity;
                editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close();
            }
        }
    }
}
