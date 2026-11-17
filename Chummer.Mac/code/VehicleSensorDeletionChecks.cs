#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Drive the actual delete callers: a transition-only baseline cannot establish
// confirmation cancellation, removal ownership or surrounding accounting.
internal static class VehicleSensorDeletionChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static void Run(Character source, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career;
            bool confirm = character.Options.ConfirmDelete; character.Options.ConfirmDelete = true;
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            editor.Show();
            try
            {
                var tree = Field<Forms.TreeView>(editor, "treVehicles");
                var vehicle = new Vehicle(character); var node = new Forms.TreeNode();
                vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!, node, null, null, null, null, null, false);
                var sensor = new Gear(character); var sensorNode = new Forms.TreeNode();
                sensor.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Extra-Large Vehicle Sensor' and category='Sensors']")!, character,
                    sensorNode, 0, new List<Weapon>(), new List<Forms.TreeNode>());
                sensor.Cost = "0"; vehicle.Gear.Add(sensor); node.Nodes.Add(sensorNode);
                var mod = new VehicleMod(character); var modNode = new Forms.TreeNode();
                mod.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/mods/mod[name='Improved Sensor Array']")!, modNode, 0);
                vehicle.Mods.Add(mod); node.Nodes.Add(modNode); character.Vehicles.Add(vehicle); tree.Nodes[0].Nodes.Add(node);
                Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabVehicles");
                tree.SelectedNode = modNode; editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
                foreach (bool accept in new[] { false, true })
                {
                    bool finished = false, expired = false; int notices = 0;
                    var backend = Forms.Backends.Platform.Backend;
                    using var replies = new Timer(_ => backend.Post(() =>
                    {
                        if (finished) return;
                        var notice = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().LastOrDefault(f => f.Modal && f.Visible && f.DialogResult == Forms.DialogResult.None);
                        if (notice == null) return;
                        notices++; notice.DialogResult = accept ? Forms.DialogResult.Yes : Forms.DialogResult.No; notice.Close();
                    }), null, 20, 20);
                    using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
                    using var cancel = deadline.Token.Register(() => backend.Post(() =>
                    {
                        if (finished) return; expired = true;
                        foreach (var form in Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().ToArray()) form.Close();
                    }));
                    try
                    {
                        byte[] before = character.CaptureSnapshot(); int money = character.Nuyen, expenses = character.ExpenseEntries.Count;
                        editor.GetType().GetMethod("cmdDeleteVehicle_Click", Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                        if (expired) throw new TimeoutException("Sensor deletion confirmation exceeded its deadline.");
                        string context = " career=" + career + " accept=" + accept;
                        check(notices == 1 && editor.Enabled && character.Nuyen == money && character.ExpenseEntries.Count == expenses,
                            "Actual sensor modification deletion confirmation retains accounting" + context);
                        check(accept ? !vehicle.Mods.Contains(mod) && modNode.Parent == null && sensor.Name == "Vehicle Sensor" &&
                            sensor.Capacity == "12" && sensor.Signal == 5 && sensorNode.Text == "Vehicle Sensor" && ReferenceEquals(vehicle.Gear.Single(), sensor) &&
                            Field<bool>(editor, "_blnIsDirty")
                            : vehicle.Mods.Contains(mod) && ReferenceEquals(modNode.Parent, node) && before.AsSpan().SequenceEqual(character.CaptureSnapshot()) && !Field<bool>(editor, "_blnIsDirty"),
                            "Actual sensor modification deletion preserves cancelled snapshot or lowers sensor and removes canonical mod" + context);
                    }
                    finally { finished = true; }
                }
            }
            finally { character.Options.ConfirmDelete = confirm; editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close(); }
        }
    }
}
