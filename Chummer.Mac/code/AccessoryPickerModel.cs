#nullable enable
using System;
using System.Linq;
using System.Xml;
using System.Xml.XPath;

namespace Chummer;

internal sealed record AccessoryPickerDetails(string Recoil, string Mount, string Availability,
    string Cost, string AvailabilityTest, string Source, string SourceTooltip) : ICatalogPickerDetails;

// Accessories have mounts rather than catalog categories. Keep selection filtering
// separate from the window and preserve the existing preview rounding/cost policy.
internal sealed class AccessoryPickerModel : CatalogPickerModel<AccessoryPickerDetails>, IQuantityCatalogPicker
{
    private int quantity = 1;
    public bool IsQuantityEnabled => SelectedEntry != null && WeaponAccessoryBatch.SupportsQuantity(SelectedEntry.Name);
    public int Quantity
    {
        get => IsQuantityEnabled ? quantity : 1;
        set { int next = Math.Clamp(value, 1, 1000); if (quantity == next) return; quantity = next; UpdateDetails(); }
    }
    protected override void NotifyAdditionalProperties()
    {
        Notify(nameof(Quantity)); Notify(nameof(IsQuantityEnabled));
    }
    private readonly string[] allowedMounts;
    private readonly int weaponCost;
    internal AccessoryPickerModel(Character character, bool career, string mounts, int weaponCost, XmlDocument? catalog = null)
        : base(character, career, catalog ?? XmlManager.Instance.Load("weapons.xml"),
            "/chummer/accessories/accessory", "", "accessory-picker", false)
    {
        allowedMounts = mounts.Split('/', StringSplitOptions.RemoveEmptyEntries);
        this.weaponCost = weaponCost;
        Refresh();
    }
    protected override void RememberCategory(string category) { }
    protected override bool IncludeEntry(XmlNode definition)
    {
        string mount = definition["mount"]?.InnerText ?? "";
        return mount.Length == 0 || mount.Split('/').Intersect(allowedMounts, StringComparer.Ordinal).Any();
    }
    protected override AccessoryPickerDetails CalculateDetails(CatalogEntry entry)
    {
        XmlNode definition = entry.Definition;
        // Weapon Cost is the legacy expression token. Do not apply the weapon's
        // accessory multiplier here: purchase/core calculations already own it.
        string expression = FreeCost ? "0" : definition["cost"]!.InnerText.Replace("Weapon Cost", weaponCost.ToString(System.Globalization.CultureInfo.InvariantCulture));
        XPathNavigator navigator = Catalog.CreateNavigator()!;
        int cost = Convert.ToInt32(Convert.ToDouble(navigator.Evaluate(expression), GlobalOptions.Instance.CultureInfo));
        cost = checked(Convert.ToInt32(cost * (1 + Markup / 100.0)) * Quantity);
        string availability = definition["avail"]!.InnerText
            .Replace("R", LanguageManager.Instance.GetString("String_AvailRestricted"))
            .Replace("F", LanguageManager.Instance.GetString("String_AvailForbidden"));
        string mounts = string.Join("/", definition["mount"]!.InnerText.Split('/', StringSplitOptions.RemoveEmptyEntries)
            .Select(mount => LanguageManager.Instance.GetString("String_Mount" + mount)));
        string source = definition["source"]!.InnerText;
        string page = definition["altpage"]?.InnerText ?? definition["page"]!.InnerText;
        return new AccessoryPickerDetails(definition["rc"]?.InnerText ?? "", mounts, availability,
            string.Format("{0:###,###,##0¥}", cost), Character.AvailTest(cost, availability),
            Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page);
    }
}
