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
            picker.PreferCategoryForParent(new Commlink(character)); var model = picker.CreateModel();
            check(model.Category?.Code == "Commlink Operating System", "commlink plug-in starts in operating systems without changing allowed categories");
        }
        using (var picker = new frmSelectGear(character) { AllowedCategories = "Ammunition", InitialCategory = "Commlink Operating System", AmmunitionCategory = "Assault Rifles" })
        {
            var model = picker.CreateModel();
            check(model.Category?.Code == "Ammunition" && model.Items.Any(item => item.Name == "Ammo: Regular Ammo") &&
                !model.Items.Any(item => item.Name.Contains("Arrow") || item.Name.Contains("Rocket") || item.Name.Contains("Bolt")),
                "contextual ammo filters incompatible families and respects unavailable default categories");
            model.SearchText = "Arrow"; check(model.Items.Count == 0, "ammo search cannot reintroduce incompatible weapon families");
            model.SearchText = "missing \"quoted\" ammunition";
            check(model.Items.Count == 0 && model.Details == null && !model.CanAccept, "quoted gear search clears stale details and disables purchase");
            model.SearchText = "Regular Ammo";
            check(model.CanAccept && model.Details?.Cost.Length > 0, "gear purchase recovers after empty quoted search");
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
