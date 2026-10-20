using System;
using System.Linq;

namespace Chummer;

internal static class WeaponModificationUndoChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var character = new Character();
        var vehicle = new Vehicle(character); character.Vehicles.Add(vehicle);
        var vehicleMod = new VehicleMod(character); vehicle.Mods.Add(vehicleMod);
        var direct = new Weapon(character); vehicle.Weapons.Add(direct);
        var nested = new Weapon(character); vehicleMod.Weapons.Add(nested);
        var directUnderbarrel = new Weapon(character); direct.UnderbarrelWeapons.Add(directUnderbarrel);
        var nestedUnderbarrel = new Weapon(character); nested.UnderbarrelWeapons.Add(nestedUnderbarrel);
        foreach (var weapon in new[] { direct, nested, directUnderbarrel, nestedUnderbarrel })
        {
            var paid = new WeaponMod(character); var free = new WeaponMod(character);
            weapon.WeaponMods.Add(paid); weapon.WeaponMods.Add(free);
            int money = character.Nuyen, entries = character.ExpenseEntries.Count;
            check(WeaponModificationUndo.RemoveMounted(character, paid.InternalId) &&
                weapon.WeaponMods.Count == 1 && ReferenceEquals(weapon.WeaponMods.Single(), free) &&
                character.Nuyen == money && character.ExpenseEntries.Count == entries,
                "Mounted modification removal keeps the separate free unit and leaves ledger accounting to its owner: " +
                (ReferenceEquals(weapon, direct) ? "direct" : ReferenceEquals(weapon, nested) ? "vehicle mod" :
                ReferenceEquals(weapon, directUnderbarrel) ? "direct underbarrel" : "vehicle mod underbarrel"));
        }
        check(!WeaponModificationUndo.RemoveMounted(character, Guid.NewGuid().ToString()) &&
            AccessoryGroupViews.Weapons(character, true).All(weapon => weapon.WeaponMods.Count == 1),
            "Missing mounted modification identity cannot remove another inventory unit");
    }
}
