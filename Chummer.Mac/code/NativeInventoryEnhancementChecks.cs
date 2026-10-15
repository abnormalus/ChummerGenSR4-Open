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
                    var picker = Forms.Application.OpenForms.OfType<frmSelectWeaponAccessory>().Last();
                    Field<Forms.ListBox>(picker, "lstAccessory").SelectedValue = "Weapon Commlink";
                    Field<Forms.Button>(picker, "cmdOK").PerformClick();
                });
                Invoke(editor, "tsWeaponAddAccessory_Click");
                check(axe.WeaponAccessories.Any(item => item.Name == "Weapon Commlink"), "Native " + mode + " melee accessory purchase reaches the existing inventory path");

                var rifle = AddWeapon("AK-97");
                var menu = Field<Forms.ContextMenuStrip>(editor, "cmsWeapon");
                var command = menu.Items.OfType<Forms.ToolStripMenuItem>().Single(item => item.Name == "buyWeaponAmmunition");
                menu.Show(weaponTree, new System.Drawing.Point(8, 8)); menu.Hide();
                if (!command.Visible) throw new InvalidOperationException("Ranged weapon ammunition command is unavailable.");
                gearTree.SelectedNode = linkNode;
                int beforeCount = character.Gear.Count, beforeMoney = character.Nuyen;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = Forms.Application.OpenForms.OfType<frmSelectGear>().Last();
                    Field<Forms.ListBox>(picker, "lstGear").SelectedValue = "Ammo: Regular Ammo";
                    Field<Forms.NumericUpDown>(picker, "nudGearQty").Value = 10;
                    Field<Forms.Button>(picker, "cmdOK").PerformClick();
                });
                command.PerformClick();
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
