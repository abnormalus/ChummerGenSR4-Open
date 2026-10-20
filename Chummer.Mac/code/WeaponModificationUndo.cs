#nullable enable
using System.Linq;

namespace Chummer;

// Ledger identity belongs to the purchased modification, regardless of whether
// its weapon is directly mounted, carried by a vehicle mod, or underbarrel.
internal static class WeaponModificationUndo
{
    internal static bool RemoveMounted(Character character, string modificationId)
    {
        foreach (var weapon in AccessoryGroupViews.Weapons(character, true))
        {
            var modification = weapon.WeaponMods.FirstOrDefault(item => item.InternalId == modificationId);
            if (modification == null) continue;
            weapon.WeaponMods.Remove(modification);
            return true;
        }
        return false;
    }
}
