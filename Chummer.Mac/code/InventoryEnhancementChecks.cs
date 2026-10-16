#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class InventoryEnhancementChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    internal static void Run(Character character, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        using (var picker = new frmSelectGear(character) { AllowedCategories = "Commlink Operating System,Commlink Upgrade" })
        {
            picker.PreferCategoryForParent(new Commlink(character)); picker.Show();
            check(Field<ComboBox>(picker, "cboCategory").SelectedValue?.ToString() == "Commlink Operating System",
                "commlink plug-in selection starts in operating systems without changing allowed categories");
            picker.Close();
        }
        using (var picker = new frmSelectGear(character) { AllowedCategories = "Ammunition", InitialCategory = "Commlink Operating System", AmmunitionCategory = "Assault Rifles" })
        {
            picker.Show();
            var choices = Field<ListBox>(picker, "lstGear").Items.Cast<ListItem>().ToArray();
            check(Field<ComboBox>(picker, "cboCategory").SelectedValue?.ToString() == "Ammunition" && choices.Any(item => item.Value == "Ammo: Regular Ammo") &&
                !choices.Any(item => item.Value.Contains("Arrow") || item.Value.Contains("Rocket") || item.Value.Contains("Bolt")),
                "contextual ammo buying filters incompatible families and respects unavailable default categories");
            Field<TextBox>(picker, "txtSearch").Text = "Arrow";
            check(Field<ListBox>(picker, "lstGear").Items.Count == 0, "ammo search cannot reintroduce incompatible weapon families");
            Field<TextBox>(picker, "txtSearch").Text = "missing \"quoted\" ammunition";
            check(Field<ListBox>(picker, "lstGear").Items.Count == 0 && Field<Label>(picker, "lblCost").Text == "" && !Field<Button>(picker, "cmdOK").Enabled,
                "gear search handles quoted input, clears stale details and disables purchase");
            Field<TextBox>(picker, "txtSearch").Text = "Regular Ammo";
            Field<ListBox>(picker, "lstGear").SelectedIndex = 0;
            check(Field<Button>(picker, "cmdOK").Enabled && Field<Label>(picker, "lblCost").Text != "",
                "gear purchase recovers after an empty quoted search");
            picker.Close();
        }
        var copy = new Character(); copy.LoadSnapshot(original);
        XmlDocument weapons = XmlManager.Instance.Load("weapons.xml");
        XmlNode Definition(string name) => weapons.SelectNodes("/chummer/weapons/weapon")!.Cast<XmlNode>().First(node => node["name"]?.InnerText == name);
        var axe = new Weapon(copy); axe.Create(Definition("Combat Axe"), copy, new TreeNode(), null, null, null);
        check(WeaponAccessoryPolicy.CanAdd(axe, Definition("Combat Axe")), "ordinary melee weapons allow mount-free accessories despite legacy catalog flag");
        axe.WeaponName = "Custom axe";
        check(axe.DisplayName != axe.Name && WeaponAccessoryPolicy.FindDefinition(axe, weapons) == Definition("Combat Axe"),
            "accessory lookup uses canonical weapon identity instead of its decorated display label");
        var accessory = new WeaponAccessory(copy);
        accessory.Create(weapons.SelectSingleNode("/chummer/accessories/accessory[name='Weapon Commlink']")!, new TreeNode(), "");
        accessory.Parent = axe; axe.WeaponAccessories.Add(accessory); copy.Weapons.Add(axe);
        var ammo = new Gear(copy);
        ammo.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Ammo: Regular Ammo']")!, copy,
            new TreeNode(), 0, new List<Weapon>(), new List<TreeNode>(), "Assault Rifles");
        ammo.Quantity = 30; copy.Gear.Add(ammo);
        check(ammo.Extra == "Assault Rifles" && ammo.Quantity == 30, "weapon-aware ammunition creation assigns its known class without a category dialog");
        var pistolAmmo = new Gear(copy);
        pistolAmmo.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Ammo: Regular Ammo']")!, copy,
            new TreeNode(), 0, new List<Weapon>(), new List<TreeNode>(), "Heavy Pistols");
        check(pistolAmmo.Extra == "Heavy Pistols" && ammo.Extra == "Assault Rifles",
            "contextual rifle and pistol ammunition retain distinct canonical weapon classes");
        var reloaded = new Character(); reloaded.LoadSnapshot(copy.CaptureSnapshot());
        check(reloaded.Weapons.Single(weapon => weapon.InternalId == axe.InternalId).WeaponAccessories.Any(item => item.Name == "Weapon Commlink") &&
            reloaded.Gear.Single(item => item.InternalId == ammo.InternalId).Extra == "Assault Rifles",
            "melee accessories and ammunition class survive the existing character format");
        check(!AmmunitionSelectionPolicy.CanBuyFor(axe) && AmmunitionSelectionPolicy.IsCompatible("Ammo: Regular Ammo", "Assault Rifles", copy.Options) &&
            !AmmunitionSelectionPolicy.IsCompatible("Arrow", "Assault Rifles", copy.Options), "ammunition actions exclude melee weapons and retain family compatibility");
        using (var owner = new Form())
        using (var dialog = new Form())
        {
            owner.Show(); var pending = dialog.ShowDialogAsync(owner); dialog.Close();
            check(pending.IsCompleted && pending.GetAwaiter().GetResult() == DialogResult.Cancel && dialog.DialogResult == DialogResult.Cancel,
                "closing a modal dialog keeps its returned result and DialogResult property consistently cancelled");
            owner.Close();
        }
        check(character.CaptureSnapshot().SequenceEqual(original), "inventory enhancement checks leave the original character unchanged");
    }
}
