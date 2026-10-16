#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chummer;

// Read-only inventory selection shared by reload and weapon-tree projections. Returned Gear
// instances remain owned by their original inventory, never copied into a weapon collection.
internal static class WeaponAmmunition
{
    internal static bool Matches(Weapon weapon, Gear ammunition)
    {
        if (ammunition.Quantity <= 0 || ammunition.Category != "Ammunition") return false;
        // Preserve the legacy launchers' canonical catalog-name families and culture-sensitive
        // prefix matching. Display/localized names must not decide ammunition compatibility.
        return weapon.AmmoCategory switch {
            "Grenade Launchers" => ammunition.Name.StartsWith("Minigrenade:", StringComparison.CurrentCulture),
            "Missile Launchers" => ammunition.Name.StartsWith("Missile:", StringComparison.CurrentCulture) ||
                ammunition.Name.StartsWith("Rocket:", StringComparison.CurrentCulture),
            "Mortar Launchers" => ammunition.Name.StartsWith("Mortar Round:", StringComparison.CurrentCulture),
            _ => ammunition.Extra == weapon.AmmoCategory
        };
    }

    internal static List<Gear> Carried(Character character, Weapon weapon)
    {
        // Reload currently reaches carried roots and their direct children. Do not silently widen
        // containment/pool rules while introducing shared views; deeper traversal is separate work.
        var candidates = character.Gear.SelectMany(root => new[] { root }.Concat(root.Children));
        bool restrict = character.Options.RestrictStickNShock &&
            character.Options.StickNShockExcludedWeaponCategories.Contains(weapon.AmmoCategory);
        return candidates.Where(ammo => Matches(weapon, ammo) &&
            !(ammo.Parent is { } parent && parent.Name.StartsWith("Spare Clip", StringComparison.CurrentCulture) && parent.Quantity <= 0) &&
            (!restrict || ammo.Name != "Ammo: Stick-n-Shock")).ToList();
    }

    internal static List<Gear> Mounted(Vehicle vehicle, Weapon weapon)
    {
        // Existing mounted reload uses only this vehicle's root stock and does not apply the carried
        // Stick-n-Shock preference. Preserve that distinction until a separately reviewed rules change.
        return vehicle.Gear.Where(ammo => Matches(weapon, ammo)).ToList();
    }
}
