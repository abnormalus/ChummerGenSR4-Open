#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

// A quantity is a purchase/UI concept only. Each unit remains an ordinary accessory
// with its own GUID; older readers and existing expense undo need no new fields.
internal static class WeaponAccessoryBatch
{
    internal sealed record Unit(WeaponAccessory Accessory, TreeNode Node, int PurchaseCost);
    internal sealed record Plan(IReadOnlyList<Unit> Units)
    {
        internal int PurchaseCost => checked(Units.Sum(unit => unit.PurchaseCost));
    }

    internal static bool SupportsQuantity(string name) => name is "Spare Clips" or "Speed Loader" or
        "Ammo Drum/Box" or "Spare Clips (2050)" or "Speed Loader (2050)";

    // Calculate incremental weapon costs in purchase order, including the existing
    // multiplier/rounding rules. The temporary model attachments are always removed;
    // insufficient funds or cancellation can reject the entire plan before commit.
    internal static Plan Prepare(Character character, Weapon weapon, XmlNode definition,
        string mount, int quantity, int markup, bool free)
    {
        if (quantity < 1 || quantity > 1000) throw new ArgumentOutOfRangeException(nameof(quantity));
        if (quantity != 1 && !SupportsQuantity(definition["name"]!.InnerText))
            throw new ArgumentException("This accessory does not support quantity purchases.", nameof(quantity));
        var prepared = new List<Unit>(quantity);
        var attached = new List<WeaponAccessory>(quantity);
        try
        {
            for (int index = 0; index < quantity; index++)
            {
                var node = new TreeNode();
                var accessory = new WeaponAccessory(character);
                accessory.Create(definition, node, mount); accessory.Parent = weapon;
                int priorCost = weapon.TotalCost;
                weapon.WeaponAccessories.Add(accessory); attached.Add(accessory);
                int cost = checked(weapon.TotalCost - priorCost);
                cost = Convert.ToInt32(cost * (1 + markup / 100.0));
                if (accessory.TotalAvail.EndsWith(LanguageManager.Instance.GetString("String_AvailRestricted")) && character.Options.MultiplyRestrictedCost)
                    cost = checked(cost * character.Options.RestrictedCostMultiplier);
                if (accessory.TotalAvail.EndsWith(LanguageManager.Instance.GetString("String_AvailForbidden")) && character.Options.MultiplyForbiddenCost)
                    cost = checked(cost * character.Options.ForbiddenCostMultiplier);
                prepared.Add(new Unit(accessory, node, free ? 0 : cost));
            }
            var plan = new Plan(prepared.AsReadOnly());
            _ = plan.PurchaseCost; // Reject overflow before returning a seemingly valid plan.
            return plan;
        }
        finally
        {
            foreach (var accessory in attached) weapon.WeaponAccessories.Remove(accessory);
        }
    }

    // Keep one ordinary ledger/undo record per unit. Preflight the complete price
    // before changing the model, so a batch is never a partially affordable purchase.
    internal static bool TryPurchase(Character character, Weapon weapon, XmlNode definition, string mount,
        int quantity, int markup, bool free, bool career, bool vehicle, Form owner, out Plan? plan)
    {
        plan = null;
        try
        {
            Plan prepared = Prepare(character, weapon, definition, mount, quantity, markup, free);
            if (career && !free && prepared.PurchaseCost > character.Nuyen)
            {
                AppDiagnostics.Record("accessory-batch.rejected-funds");
                MessageBox.Show(owner, LanguageManager.Instance.GetString("Message_NotEnoughNuyen"),
                    LanguageManager.Instance.GetString("MessageTitle_NotEnoughNuyen"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                return false;
            }
            var expenses = new List<ExpenseLogEntry>();
            if (career && !free)
            {
                foreach (var unit in prepared.Units)
                {
                    var expense = new ExpenseLogEntry();
                    expense.Create(-unit.PurchaseCost, LanguageManager.Instance.GetString(vehicle
                        ? "String_ExpensePurchaseVehicleWeaponAccessory" : "String_ExpensePurchaseWeaponAccessory") + " " + unit.Accessory.DisplayNameShort,
                        ExpenseType.Nuyen, DateTime.Now);
                    var undo = new ExpenseUndo();
                    undo.CreateNuyen(vehicle ? NuyenExpenseType.AddVehicleWeaponAccessory : NuyenExpenseType.AddWeaponAccessory, unit.Accessory.InternalId);
                    expense.Undo = undo; expenses.Add(expense);
                }
            }
            foreach (var unit in prepared.Units) weapon.WeaponAccessories.Add(unit.Accessory);
            if (career && !free) character.Nuyen -= prepared.PurchaseCost;
            character.ExpenseEntries.AddRange(expenses);
            plan = prepared; AppDiagnostics.Record("accessory-batch.purchased");
            return true;
        }
        catch (Exception error)
        {
            AppDiagnostics.Record("accessory-batch.prepare-failed", error);
            AppErrors.Notify(owner, LanguageManager.Instance.GetString("Message_CommandFailed"), "Chummer");
            return false;
        }
    }

    // Derive equivalence from exactly what the legacy writer persists, excluding
    // only the unit's own identity. Notes, installation, discounts and child gear
    // remain significant. Never combine records or discard GUIDs to group a view.
    internal static IReadOnlyList<IReadOnlyList<WeaponAccessory>> Groups(Weapon weapon) =>
        weapon.WeaponAccessories.GroupBy(accessory => SupportsQuantity(accessory.Name)
            ? PersistedContent(accessory) : accessory.InternalId, StringComparer.Ordinal)
        .Select(group => (IReadOnlyList<WeaponAccessory>)group.ToArray()).ToArray();

    private static string PersistedContent(WeaponAccessory accessory)
    {
        using var text = new StringWriter();
        using (var writer = new XmlTextWriter(text)) accessory.Save(writer);
        var document = new XmlDocument(); document.LoadXml(text.ToString());
        var root = document.DocumentElement!;
        root.RemoveChild(root["guid"]!);
        return root.OuterXml;
    }
}
