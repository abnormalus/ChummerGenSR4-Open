#nullable enable
using System;
using System.Reflection;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Characterize both original editor methods before separating sensor mutation
// from tree labels. Fixed catalog expectations do not call a replacement policy.
internal static class VehicleSensorChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    internal static void Run(Character original, Action<bool, string> check)
    {
        string[] names = { "Microdrone Sensor", "Minidrone Sensor", "Small Drone Sensor", "Medium Drone Sensor", "Large Drone Sensor", "Vehicle Sensor", "Extra-Large Vehicle Sensor" };
        string[] capacities = { "1", "3", "5", "6", "8", "12", "30" };
        int[] signals = { 2, 3, 4, 4, 4, 5, 6 };
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = career;
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            var tree = (Forms.TreeView)editor.GetType().GetField("treVehicles", Flags)!.GetValue(editor)!;
            var change = editor.GetType().GetMethod("ChangeVehicleSensor", Flags)!;
            void Invoke(Vehicle vehicle, bool increase) => change.Invoke(editor, new object[] { vehicle, increase });
            foreach (bool increase in new[] { false, true })
            for (int index = 0; index < names.Length; index++)
            {
                var vehicle = new Vehicle(character);
                var sensor = new Gear(character) { Name = names[index], MaxRating = 20, Rating = 7, Capacity = "99", Signal = 9, Avail = "17R", Cost = "123", Source = "Synthetic", Page = "999", Notes = "Retained notes" };
                var child = new Gear(character) { Name = "Retained child" }; sensor.Children.Add(child); vehicle.Gear.Add(sensor);
                string id = sensor.InternalId;
                tree.Nodes.Clear();
                // CommonFunctions.FindNode reads Tag on every visited node.
                // Match the tagged vehicle-root/vehicle/sensor hierarchy.
                var root = new Forms.TreeNode("Synthetic root") { Tag = "Node_SelectedVehicles" };
                var vehicleNode = new Forms.TreeNode("Synthetic vehicle") { Tag = vehicle.InternalId };
                var node = new Forms.TreeNode("Original label") { Tag = id };
                vehicleNode.Nodes.Add(node); root.Nodes.Add(vehicleNode); tree.Nodes.Add(root);
                Invoke(vehicle, increase);
                int next = index + (increase ? 1 : -1); bool changed = next >= 0 && next < names.Length;
                string context = " career=" + career + " from=" + index + " increase=" + increase;
                check(changed
                    ? sensor.Name == names[next] && sensor.Rating == 0 && sensor.Capacity == capacities[next] && sensor.Signal == signals[next] && sensor.Avail == "0" && sensor.Cost == "0" && sensor.Source == (next == 6 ? "AR" : "SR4") && sensor.Page == (next == 6 ? "138" : "333")
                    : sensor.Name == names[index] && sensor.Rating == 7 && sensor.Capacity == "99" && sensor.Signal == 9 && sensor.Avail == "17R" && sensor.Cost == "123" && sensor.Source == "Synthetic" && sensor.Page == "999",
                    "Original vehicle sensor transition preserves fixed field contract" + context);
                check(ReferenceEquals(vehicle.Gear[0], sensor) && vehicle.Gear.Count == 1 && sensor.InternalId == id && sensor.Notes == "Retained notes" && sensor.MaxRating == 20 && sensor.Children.Count == 1 && ReferenceEquals(sensor.Children[0], child),
                    "Original vehicle sensor transition retains identity notes children and collection" + context);
                check(node.Text == (changed ? sensor.DisplayNameShort : "Original label"),
                    "Original vehicle sensor transition refreshes label only on change" + context);
            }
            // The first recognized endpoint stops lookup even when a later
            // sensor could move. An unrecognized sensor alone is a no-op.
            var multiple = new Vehicle(character);
            var endpoint = new Gear(character) { Name = names[0], Capacity = "99" };
            var later = new Gear(character) { Name = names[3], Capacity = "99" };
            multiple.Gear.Add(endpoint); multiple.Gear.Add(later); tree.Nodes.Clear();
            Invoke(multiple, false);
            check(endpoint.Capacity == "99" && later.Capacity == "99" && later.Name == names[3],
                "Original vehicle sensor first recognized boundary stops lookup career=" + career);
            var unknown = new Vehicle(character); var custom = new Gear(character) { Name = "Custom sensor", Capacity = "99" }; unknown.Gear.Add(custom);
            Invoke(unknown, true);
            check(custom.Name == "Custom sensor" && custom.Capacity == "99", "Original vehicle sensor unrecognized name is unchanged career=" + career);
            var missing = new Vehicle(character); var noNode = new Gear(character) { Name = names[0], Capacity = "99" }; missing.Gear.Add(noNode);
            bool rejected = false;
            try { Invoke(missing, true); }
            catch (TargetInvocationException ex) when (ex.InnerException is NullReferenceException) { rejected = true; }
            check(rejected && noNode.Name == names[1] && noNode.Capacity == "3",
                "Original vehicle sensor missing tree fails after model mutation career=" + career);
        }
    }
}
