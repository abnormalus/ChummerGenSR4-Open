#nullable enable
using System;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class AccessoryBatchChecks
{
    internal static void Run(Character source, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        var catalog = XmlManager.Instance.Load("weapons.xml");
        var weapon = new Weapon(character);
        weapon.Create(catalog.SelectSingleNode("/chummer/weapons/weapon[name='AK-97']")!, character, new TreeNode(), null, null, null);
        character.Weapons.Add(weapon);
        XmlNode spare = catalog.SelectSingleNode("/chummer/accessories/accessory[name='Spare Clips']")!;
        int priorCost = weapon.TotalCost, priorMoney = character.Nuyen, expenses = character.ExpenseEntries.Count;
        var plan = WeaponAccessoryBatch.Prepare(character, weapon, spare, "", 3, 7, false);
        check(weapon.WeaponAccessories.Count == 0 && weapon.TotalCost == priorCost && character.Nuyen == priorMoney &&
            character.ExpenseEntries.Count == expenses && plan.Units.Select(unit => unit.Accessory.InternalId).Distinct().Count() == 3,
            "accessory batch preparation preserves inventory, money and ledger and gives every unit a legacy identity");
        int repeatedCost = 0;
        foreach (var unit in plan.Units)
        {
            int before = weapon.TotalCost; weapon.WeaponAccessories.Add(unit.Accessory);
            repeatedCost += Convert.ToInt32((weapon.TotalCost - before) * 1.07);
        }
        check(plan.PurchaseCost == repeatedCost, "accessory batch retains incremental per-unit purchase rounding");
        byte[] beforeGrouping = character.CaptureSnapshot();
        check(WeaponAccessoryBatch.Groups(weapon).Single().Count == 3 && character.CaptureSnapshot().SequenceEqual(beforeGrouping),
            "equivalent accessory units group without changing serialized inventory");
        plan.Units[1].Accessory.Notes = "custom unit";
        plan.Units[2].Accessory.Installed = false;
        check(WeaponAccessoryBatch.Groups(weapon).Count == 3, "accessory grouping preserves notes and installed-state distinctions");
        var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
        var restored = reopened.Weapons.Single(item => item.InternalId == weapon.InternalId);
        check(restored.WeaponAccessories.Count == 3 && restored.WeaponAccessories.Select(item => item.InternalId)
            .SequenceEqual(plan.Units.Select(unit => unit.Accessory.InternalId)) && restored.TotalCost == weapon.TotalCost,
            "accessory batch round trip retains ordinary units, GUIDs and legacy weapon costs");
        var history = new InventoryUndo(character);
        byte[] beforeBatch = character.CaptureSnapshot();
        using (var owner = new Form())
        {
            int money = character.Nuyen, ledger = character.ExpenseEntries.Count;
            check(WeaponAccessoryBatch.TryPurchase(character, weapon, spare, "", 3, 0, false, false, false, owner, out _),
                "creation accessory batch commits all ordinary units through the shared purchase boundary");
            history.Observe();
            check(history.Count == 1 && history.Undo() && character.CaptureSnapshot().SequenceEqual(beforeBatch) && character.Nuyen == money && character.ExpenseEntries.Count == ledger,
                "one inventory undo restores the entire accessory batch with original identities and accounting");
        }
        // Undo deliberately recreates model instances; subsequent checks use that canonical instance.
        weapon = character.Weapons.Single(item => item.InternalId == weapon.InternalId);
        int count = weapon.WeaponAccessories.Count;
        bool rejected = false;
        try { WeaponAccessoryBatch.Prepare(character, weapon, spare, "", 1001, 0, false); }
        catch (ArgumentOutOfRangeException) { rejected = true; }
        check(rejected && weapon.WeaponAccessories.Count == count, "invalid accessory batch sizes cannot leave partial purchases");
        var broken = spare.CloneNode(true); broken["cost"]!.InnerText = "invalid expression (";
        rejected = false;
        try { WeaponAccessoryBatch.Prepare(character, weapon, broken, "", 3, 0, false); }
        catch { rejected = true; }
        check(rejected && weapon.WeaponAccessories.Count == count && character.Nuyen == priorMoney,
            "accessory batch cost failures remove temporary attachments and leave funds unchanged");
    }
}
