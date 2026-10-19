using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Chummer;

// Eligibility only. Source filtering, translated display and acceptance are separate
// boundaries; extraction intentionally preserves the existing optional-power policy.
internal static class CritterPowerEligibility
{
    internal static IReadOnlyList<XmlNode> Select(XmlDocument catalog, XmlNode metatype,
        string category, IEnumerable<string> ownedNames)
    {
        var powers = catalog.SelectNodes("/chummer/powers/power")!.Cast<XmlNode>().ToArray();
        if (category == "Toxic Critter Powers") return powers.Where(node => node["toxic"]?.InnerText == "yes").ToArray();
        if (category == "Weakness") return powers.Where(node => node["category"]?.InnerText == "Weakness").ToArray();
        if (metatype["optionalpowers"] == null) return powers.Where(node => node["category"]?.InnerText == category).ToArray();
        var result = new List<XmlNode>();
        void Add(string name) => result.Add(powers.First(node => node["name"]!.InnerText == name));
        foreach (XmlNode optional in metatype.SelectNodes("optionalpowers/power")!) Add(optional.InnerText);
        var owned = ownedNames.ToHashSet();
        bool presence = owned.Any(IsPhysicalPresence);
        foreach (XmlNode innate in metatype.SelectNodes("powers/power")!)
        {
            // The legacy branch suppresses every deleted innate choice when any
            // physical-presence power is owned. Retain it here, without a rules fix.
            if (owned.Contains(innate.InnerText) || presence) continue;
            Add(innate.InnerText);
            if (innate.InnerText == "Materialization")
                foreach (string alternative in new[] { "Possession", "Inhabitation" })
                    if (!result.Any(node => node["name"]!.InnerText == alternative)) Add(alternative);
        }
        return result;
    }
    private static bool IsPhysicalPresence(string name) => name is "Materialization" or "Possession" or "Inhabitation";
}
