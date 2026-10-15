#nullable enable
using System;
using System.Xml;

namespace Chummer;

// Shared by creation/career and carried/vehicle weapons. Melee weapons may use
// mount-free accessories; their empty mount list still excludes rail-mounted parts.
internal static class WeaponAccessoryPolicy
{
    internal static bool CanAdd(Weapon weapon, XmlNode definition) =>
        !weapon.Category.StartsWith("Cyberware", StringComparison.Ordinal) &&
        (weapon.WeaponType == "Melee" || bool.TryParse(definition["allowaccessory"]?.InnerText, out bool allowed) && allowed);
}
