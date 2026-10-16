#nullable enable
using System.Globalization;
using System.Xml;

namespace Chummer;

// Immutable purchase context reusable by the future native gear picker. Sourcebook
// and category visibility remain catalog concerns, not capacity/upgrade rules.
internal sealed record GearSelectionContext(bool PositiveCapacityOnly, bool NegativeCapacityOnly,
    bool ArmorCapacityOnly, int MaximumResponse, int MaximumSignal, int CurrentFirewall, int CurrentSystem)
{
    internal bool Allows(XmlNode definition)
    {
        // Legacy device upgrades use their own eligibility, before capacity filters.
        // MaximumResponse/Signal already include the caller's established +2 limit.
        if (definition["category"]?.InnerText == "Commlink Upgrade")
            return MaximumResponse == 2 && MaximumSignal == 2 ||
                UpgradeInRange(definition, "response", MaximumResponse) || UpgradeInRange(definition, "signal", MaximumSignal);
        if (definition["category"]?.InnerText == "Commlink Operating System Upgrade")
            return Number(definition, "firewall") > CurrentFirewall || Number(definition, "system") > CurrentSystem;
        if (ArmorCapacityOnly) return (definition["armorcapacity"]?.InnerText ?? "").Contains('[');
        string capacity = definition["capacity"]?.InnerText ?? "";
        if (PositiveCapacityOnly) return !capacity.Contains('[');
        return !NegativeCapacityOnly || capacity.Contains('[');
    }
    private static bool UpgradeInRange(XmlNode definition, string field, int maximum)
    {
        double value = Number(definition, field);
        return value > maximum - 2 && value <= maximum;
    }
    // Missing/non-numeric nodes compare false, as in the former XPath predicates.
    private static double Number(XmlNode definition, string field) =>
        double.TryParse(definition[field]?.InnerText, NumberStyles.Float, CultureInfo.InvariantCulture, out double value) ? value : double.NaN;
}
