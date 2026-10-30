#nullable enable
using System;
using System.IO;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Reuse the movement fixture's shown Dodge Scoot editor. Fixed catalog values
// and actual selection refresh cover the model-to-tree/detail projection.
internal static class NativeVehicleSensorChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Forms.Form editor, Character character,
        Vehicle vehicle, Forms.TreeNode vehicleNode, string output, Action<bool, string> check, bool career)
    {
        var tree = Field<Forms.TreeView>(editor, "treVehicles");
        // The movement fixture intentionally passes createChildren=false. Install
        // the catalog sensor explicitly, following Vehicle.Create's bundled-gear
        // cost/rating/node setup without importing unrelated vehicle mods.
        var sensor = new Gear(character); var sensorNode = new Forms.TreeNode();
        sensor.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Vehicle Sensor' and category='Sensors']")!,
            character, sensorNode, 0, new List<Weapon>(), new List<Forms.TreeNode>());
        sensor.Cost = "0"; sensor.Quantity = 1; sensor.MaxRating = sensor.Rating;
        sensorNode.Text = sensor.DisplayName;
        vehicle.Gear.Add(sensor); vehicleNode.Nodes.Add(sensorNode);
        var originalChildren = sensor.Children.ToArray(); string id = sensor.InternalId; int count = vehicle.Gear.Count;
        var change = editor.GetType().GetMethod("ChangeVehicleSensor", Flags)!;
        foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
        {
            Avalonia.Application.Current!.RequestedThemeVariant = theme;
            editor.Select(); vehicleNode.Expand(); tree.SelectedNode = vehicleNode;
            editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
            byte[] roundTrip = character.CaptureSnapshot(); int money = character.Nuyen, expenses = character.ExpenseEntries.Count;
            foreach (bool increase in new[] { true, false })
            {
                tree.SelectedNode = vehicleNode;
                change.Invoke(editor, new object[] { vehicle, increase });
                tree.SelectedNode = sensorNode; await Task.Delay(100);
                string expected = increase ? "Extra-Large Vehicle Sensor" : "Vehicle Sensor";
                string context = " career=" + career + " theme=" + theme.Key + " increase=" + increase;
                var name = Field<Forms.Label>(editor, "lblVehicleName"); var capacity = Field<Forms.Label>(editor, "lblVehicleSlots");
                check(sensor.Name == expected && sensor.Capacity == (increase ? "30" : "12") && sensor.Signal == (increase ? 6 : 5) &&
                    sensorNode.Text == expected && name.Visible && name.Text == expected && capacity.Visible && capacity.Text.StartsWith(increase ? "30 (" : "12 ("),
                    "Native sensor transition updates canonical tree and selected details" + context);
                check(sensor.InternalId == id && vehicle.Gear.Count == count && originalChildren.SequenceEqual(sensor.Children) &&
                    character.Nuyen == money && character.ExpenseEntries.Count == expenses && !Field<bool>(editor, "_blnIsDirty") &&
                    (increase || roundTrip.AsSpan().SequenceEqual(character.CaptureSnapshot())),
                    "Native sensor transition retains identity children accounting and round-trip state" + context);
                if (increase)
                {
                    var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
                    using var image = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize((int)window.ClientSize.Width, (int)window.ClientSize.Height));
                    image.Render(window);
                    image.Save(Path.Combine(output, "native-sensor-" + (career ? "career-" : "creation-") + theme.Key.ToString()!.ToLowerInvariant() + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                }
            }
        }
    }
}
