#nullable enable
using System;
using System.Xml;

namespace Chummer;

// Presentation aliases never replace catalog identities. Existing files and older
// Chummer versions still receive the original ranged/melee name and numeric fields.
internal static class WeaponModificationNames
{
    internal const string PersonalizedGrip = "Personalized Grip";
    internal const string MeleePersonalizedGrip = "Personalized Grip, Melee";
    internal static bool IsPersonalizedGrip(string name) => name is PersonalizedGrip or MeleePersonalizedGrip;
    internal static bool IsAvailableFor(string name, string weaponType) => !IsPersonalizedGrip(name) ||
        name == (weaponType == "Melee" ? MeleePersonalizedGrip : PersonalizedGrip);

    internal static string DisplayName(string name, string? translatedName)
    {
        if (IsPersonalizedGrip(name))
        {
            XmlNode? ranged = XmlManager.Instance.Load("weapons.xml")
                .SelectSingleNode("/chummer/mods/mod[name='Personalized Grip']");
            return ranged?["translate"]?.InnerText ?? PersonalizedGrip;
        }
        return string.IsNullOrEmpty(translatedName) ? name : translatedName;
    }
}
