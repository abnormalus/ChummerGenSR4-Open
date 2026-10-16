#nullable enable
using System;
using System.Xml;
using System.Linq;

namespace Chummer;

// Shared by creation/career and carried/vehicle weapons. Melee weapons may use
// mount-free accessories; their empty mount list still excludes rail-mounted parts.
internal static class WeaponAccessoryPolicy
{
    // Tree labels may include aliases, translations or custom weapon names. Resolve
    // catalog identity from the model and keep same-name categories distinct.
    internal static XmlNode? FindDefinition(Weapon weapon, XmlDocument catalog) =>
        catalog.SelectNodes("/chummer/weapons/weapon")!.Cast<XmlNode>().FirstOrDefault(node =>
            node["name"]?.InnerText == weapon.Name && node["category"]?.InnerText == weapon.Category);

    internal static bool CanAdd(Weapon weapon, XmlNode definition) =>
        !weapon.Category.StartsWith("Cyberware", StringComparison.Ordinal) &&
        (weapon.WeaponType == "Melee" || bool.TryParse(definition["allowaccessory"]?.InnerText, out bool allowed) && allowed);
}
