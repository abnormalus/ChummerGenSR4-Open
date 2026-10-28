#nullable enable
using System;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Verify actual editor selection refresh, complementing the independent model baseline.
// Synthetic models are editor-owned; finally blocks restore window/theme state.
internal static class NativeVehicleMovementChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Character source, Action<bool, string> check)
    {
        var app = Avalonia.Application.Current!; var originalTheme = app.RequestedThemeVariant;
        try
        {
            foreach (bool career in new[] { false, true })
            {
                var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career;
                using Forms.Form editor = career ? new frmCareer(character) { Loading = true, MdiParent = owner } : new frmCreate(character) { Loading = true, MdiParent = owner };
                editor.Show();
                try
                {
                    var tree = Field<Forms.TreeView>(editor, "treVehicles");
                    var vehicle = new Vehicle(character); var node = new Forms.TreeNode();
                    vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!, node, null, null, null, null, null, false);
                    vehicle.Accel = "10/20"; vehicle.Speed = 100; vehicle.Body = 4; vehicle.Armor = 1;
                    var xml = new XmlDocument(); xml.LoadXml("<bonus><accel>0.5</accel><speed>0.5</speed><armor>5</armor></bonus>");
                    var mod = new VehicleMod(character) { Name = "Synthetic movement mod", Cost = "0", Rating = 2, Bonus = xml.DocumentElement, Installed = true };
                    vehicle.Mods.Add(mod); character.Vehicles.Add(vehicle); tree.Nodes[0].Nodes.Add(node);
                    Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabVehicles");
                    tree.SelectedNode = node;
                    foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
                    foreach (bool installed in new[] { true, false })
                    {
                        app.RequestedThemeVariant = theme; mod.Installed = installed; await Task.Delay(100);
                        editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false);
                        byte[] before = character.CaptureSnapshot();
                        editor.GetType().GetMethod("RefreshSelectedVehicle", Flags)!.Invoke(editor, null);
                        var accel = Field<Forms.Label>(editor, "lblVehicleAccel"); var speed = Field<Forms.Label>(editor, "lblVehicleSpeed");
                        string context = " career=" + career + " theme=" + theme.Key + " installed=" + installed;
                        check(accel.Visible && speed.Visible && accel.Text == (installed ? "13/26" : "10/20") && speed.Text == (installed ? "130" : "100"),
                            "Native selected vehicle refresh displays fixed movement values" + context + " actual=" + accel.Text + "/" + speed.Text);
                        check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()) && !Field<bool>(editor, "_blnIsDirty"),
                            "Native selected vehicle refresh preserves character and clean editor" + context);
                    }
                }
                finally { editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close(); }
            }
        }
        finally { app.RequestedThemeVariant = originalTheme; }
    }
}
