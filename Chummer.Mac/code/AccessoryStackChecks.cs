#nullable enable
using System;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class AccessoryStackChecks
{
    internal static void Run(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        var weapon = new Weapon(character); var node = new TreeNode();
        var catalog = XmlManager.Instance.Load("weapons.xml");
        weapon.Create(catalog.SelectNodes("/chummer/weapons/weapon")!.Cast<XmlNode>().First(n => n["name"]?.InnerText == "AK-97"), character, node, null, null, null);
        character.Weapons.Add(weapon);
        foreach (string name in new[] { "Spare Clips", "Speed Loader", "Ammo Drum/Box", "Spare Clips (2050)", "Speed Loader (2050)" })
        {
            var accessory = new WeaponAccessory(character);
            accessory.Create(catalog.SelectNodes("/chummer/accessories/accessory")!.Cast<XmlNode>().First(n => n["name"]?.InnerText == name), new TreeNode(), "");
            accessory.Parent = weapon; accessory.DiscountCost = true;
            int unitCost = accessory.TotalCost;
            accessory.Quantity = 5; weapon.WeaponAccessories.Add(accessory);
            check(accessory.TotalCost == unitCost * 5 && accessory.OwnCost == unitCost * 5 && accessory.DisplayName.Contains("×5"),
                "accessory stack retains per-unit discount rounding and quantity display: " + name);
        }
        var reloaded = new Character(); reloaded.LoadSnapshot(character.CaptureSnapshot());
        var restored = reloaded.Weapons.Single(w => w.InternalId == weapon.InternalId);
        check(restored.WeaponAccessories.Count == 5 && restored.WeaponAccessories.All(a => a.Quantity == 5) && restored.TotalCost == weapon.TotalCost,
            "accessory quantities and aggregate costs survive character serialization without duplicate entries");
        var model = new AccessoryPickerModel(character, true, "Top/Under", 1000);
        model.SearchText = "Spare Clips"; model.SelectedEntry = model.Items.First(e => e.Name == "Spare Clips"); model.Quantity = 3;
        check(model.Accept(false)!.Quantity == 3 && model.Details!.Cost == string.Format("{0:###,###,##0¥}", 15),
            "accessory picker accepts quantity with aggregate preview cost");
        model.SearchText = "Laser Sight";
        check(!model.IsQuantityEnabled && model.Quantity == 1 && model.Accept(false)!.Quantity == 1,
            "installed accessories cannot inherit a spare-container purchase quantity");
    }
}
