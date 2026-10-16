#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeInventoryEnhancementChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static void Invoke(object owner, string name) => owner.GetType()
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(owner, new object[] { owner, EventArgs.Empty });
    internal static async Task RunAsync(Forms.Form main, Character originalCharacter, Action<bool, string> check)
    {
        byte[] original = originalCharacter.CaptureSnapshot();
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original);
            character.Created = career; character.Nuyen = 10000;
            if (!character.Options.Books.Contains("AR")) character.Options.Books.Add("AR");
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            editor.MdiParent = main; editor.Show();
            string mode = career ? "career" : "creation";
            try
            {
                var gearTree = Field<Forms.TreeView>(editor, "treGear");
                var weaponTree = Field<Forms.TreeView>(editor, "treWeapons");
                var topTabs = Field<Forms.TabControl>(editor, "tabCharacterTabs");
                var gearTabs = Field<Forms.TabControl>(editor, "tabStreetGearTabs");
                topTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
                gearTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabGear");
                var link = new Commlink(character); var linkNode = new Forms.TreeNode();
                XmlNode linkDefinition = XmlManager.Instance.Load("gear.xml").SelectNodes("/chummer/gears/gear")!.Cast<XmlNode>()
                    .First(node => node["category"]?.InnerText == "Commlink");
                link.Create(linkDefinition, character, linkNode, 0); character.Gear.Add(link); gearTree.Nodes[0].Nodes.Add(linkNode);
                gearTree.SelectedNode = linkNode;
                bool operatingSystemsSelected = false;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = Forms.Application.OpenForms.OfType<frmSelectGear>().Last();
                    operatingSystemsSelected = Field<Forms.ComboBox>(picker, "cboCategory").SelectedValue?.ToString() == "Commlink Operating System";
                    picker.Close();
                });
                Invoke(editor, "tsGearAddAsPlugin_Click");
                check(operatingSystemsSelected && link.Children.Count == 0, "Native " + mode + " commlink add-on defaults to operating systems and cancels cleanly");

                XmlDocument catalog = XmlManager.Instance.Load("weapons.xml");
                Weapon AddWeapon(string name)
                {
                    var weapon = new Weapon(character); var node = new Forms.TreeNode();
                    var definition = catalog.SelectNodes("/chummer/weapons/weapon")!.Cast<XmlNode>().First(item => item["name"]?.InnerText == name);
                    weapon.Create(definition, character, node, null, null, null);
                    character.Weapons.Add(weapon); weaponTree.Nodes[0].Nodes.Add(node); weaponTree.SelectedNode = node;
                    return weapon;
                }
                gearTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabWeapons");
                var axe = AddWeapon("Combat Axe");
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = (AccessoryPickerWindow)NativeDialogHost.ActiveWindow!;
                    picker.SearchBox.Text = "Weapon Commlink";
                    picker.Model.SelectedEntry = picker.Model.Items.Single(item => item.Name == "Weapon Commlink");
                    picker.Accept(false);
                });
                Invoke(editor, "tsWeaponAddAccessory_Click");
                check(axe.WeaponAccessories.Any(item => item.Name == "Weapon Commlink"), "Native " + mode + " melee accessory purchase reaches the existing inventory path");

                void AddGrip()
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var picker = Forms.Application.OpenForms.OfType<frmSelectVehicleMod>().Last();
                        Field<Forms.TextBox>(picker, "txtSearch").Text = "Personalized Grip";
                        Field<Forms.ListBox>(picker, "lstMod").SelectedIndex = 0;
                        Field<Forms.Button>(picker, "cmdOK").PerformClick();
                    });
                    Invoke(editor, "tsWeaponAddModification_Click");
                }
                AddGrip();
                check(axe.WeaponMods.Any(item => item.Name == WeaponModificationNames.MeleePersonalizedGrip && item.DicePool == 1),
                    "Native " + mode + " unified grip purchase applies the melee definition");
                var rifle = AddWeapon("AK-97");
                AddGrip();
                check(rifle.WeaponMods.Any(item => item.Name == WeaponModificationNames.PersonalizedGrip && item.RC == "1" && item.DicePool == 0),
                    "Native " + mode + " unified grip purchase applies the ranged definition");
                var menu = Field<Forms.ContextMenuStrip>(editor, "cmsWeapon");
                var command = menu.Items.OfType<Forms.ToolStripMenuItem>().Single(item => item.Name == "buyWeaponAmmunition");
                menu.Show(weaponTree, new System.Drawing.Point(8, 8)); menu.Hide();
                if (!command.Visible) throw new InvalidOperationException("Ranged weapon ammunition command is unavailable.");
                gearTree.SelectedNode = linkNode;
                int beforeCount = character.Gear.Count, beforeMoney = character.Nuyen;
                bool literalSearchRecovered = false;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = Forms.Application.OpenForms.OfType<frmSelectGear>().Last();
                    Field<Forms.TextBox>(picker, "txtSearch").Text = "missing \"quoted\" ammunition";
                    literalSearchRecovered = Field<Forms.ListBox>(picker, "lstGear").Items.Count == 0 &&
                        !Field<Forms.Button>(picker, "cmdOK").Enabled && Field<Forms.Label>(picker, "lblCost").Text == "";
                    Field<Forms.TextBox>(picker, "txtSearch").Text = "";
                    Field<Forms.ListBox>(picker, "lstGear").SelectedValue = "Ammo: Regular Ammo";
                    Field<Forms.NumericUpDown>(picker, "nudGearQty").Value = 10;
                    Field<Forms.Button>(picker, "cmdOK").PerformClick();
                });
                command.PerformClick();
                check(literalSearchRecovered, "Native " + mode + " quoted gear search clears stale purchase state and recovers");
                var ammo = character.Gear.Last(item => item.Name == "Ammo: Regular Ammo");
                check(character.Gear.Count == beforeCount + 1 && ammo.Extra == rifle.AmmoCategory && ammo.Quantity == 10 &&
                    ammo.Parent == null && link.Children.Count == 0 && gearTree.SelectedNode == linkNode &&
                    (!career || character.Nuyen == beforeMoney - ammo.TotalCost),
                    "Native " + mode + " right-click ammo uses the weapon class, root inventory and existing purchase cost");
                beforeCount = character.Gear.Count; beforeMoney = character.Nuyen;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => Forms.Application.OpenForms.OfType<frmSelectGear>().Last().Close());
                command.PerformClick();
                weaponTree.SelectedNode = weaponTree.Nodes[0].Nodes.Cast<Forms.TreeNode>().Single(node => node.Tag?.ToString() == axe.InternalId);
                menu.Show(weaponTree, new System.Drawing.Point(8, 8)); menu.Hide();
                check(character.Gear.Count == beforeCount && character.Nuyen == beforeMoney && !command.Visible,
                    "Native " + mode + " cancelled ammo purchase changes nothing and melee weapons hide the command");

                // Use a decorated label to reproduce the vehicle path's former lookup
                // bug. The purchase must use the weapon's canonical name/category.
                var vehicleTree = Field<Forms.TreeView>(editor, "treVehicles");
                var vehicle = new Vehicle(character); var vehicleNode = new Forms.TreeNode();
                vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!,
                    vehicleNode, null, null, null, null, null, false);
                character.Vehicles.Add(vehicle); vehicleTree.Nodes[0].Nodes.Add(vehicleNode);
                var mounted = new Weapon(character); var mountedNode = new Forms.TreeNode();
                mounted.Create(catalog.SelectSingleNode("/chummer/weapons/weapon[name='AK-97']")!, character, mountedNode, null, null, null);
                mounted.VehicleMounted = true; mounted.WeaponName = "Custom vehicle rifle";
                mountedNode.Text = mounted.DisplayName;
                vehicle.Weapons.Add(mounted); vehicleNode.Nodes.Add(mountedNode);
                topTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabVehicles");
                vehicleTree.SelectedNode = mountedNode;
                int vehicleMoney = character.Nuyen, weaponCost = mounted.TotalCost;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = (AccessoryPickerWindow)NativeDialogHost.ActiveWindow!;
                    picker.SearchBox.Text = "Weapon Commlink";
                    picker.Model.SelectedEntry = picker.Model.Items.Single(item => item.Name == "Weapon Commlink");
                    picker.Accept(false);
                });
                Invoke(editor, "tsVehicleAddWeaponAccessory_Click");
                check(mounted.DisplayName != mounted.Name && mounted.WeaponAccessories.Any(item => item.Name == "Weapon Commlink") &&
                    character.Nuyen == vehicleMoney - (career ? mounted.TotalCost - weaponCost : 0),
                    "Native " + mode + " decorated vehicle weapon buys an accessory with its existing cost rules");
                int accessoryCount = mounted.WeaponAccessories.Count;
                vehicleMoney = character.Nuyen; vehicleTree.SelectedNode = mountedNode;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => NativeDialogHost.ActiveWindow!.Close());
                Invoke(editor, "tsVehicleAddWeaponAccessory_Click");
                check(mounted.WeaponAccessories.Count == accessoryCount && character.Nuyen == vehicleMoney,
                    "Native " + mode + " cancelled vehicle accessory purchase changes neither inventory nor funds");
            }
            finally
            {
                editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
                editor.Close();
            }
            await Task.Delay(20);
        }
        check(originalCharacter.CaptureSnapshot().SequenceEqual(original), "Native inventory enhancements leave the source character unchanged");
    }
}
