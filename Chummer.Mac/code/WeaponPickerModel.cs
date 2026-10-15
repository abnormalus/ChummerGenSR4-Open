#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal sealed record WeaponPickerDetails(string Reach, string Damage, string ArmorPenetration, string Mode,
    string RecoilCompensation, string Ammo, string Availability, string Cost, string AvailabilityTest,
    string Source, string SourceTooltip, string Included) : ICatalogPickerDetails;

// Only weapon-specific preview rules live here. Shared search, selection and purchase
// state belongs to CatalogPickerModel; the caller still owns actual inventory writes.
internal sealed class WeaponPickerModel : CatalogPickerModel<WeaponPickerDetails>
{
    internal static string LastCategory { get; set; } = "";
    internal WeaponPickerModel(Character character, bool career, XmlDocument? catalog = null)
        : base(character, career, catalog ?? XmlManager.Instance.Load("weapons.xml"),
            "/chummer/weapons/weapon", LastCategory, "weapon-picker") { Refresh(); }
    protected override bool IncludeInSearch(XmlNode definition) => definition["category"]?.InnerText is not ("Cyberware" or "Gear");
    protected override void RememberCategory(string category) => LastCategory = category;
    protected override WeaponPickerDetails CalculateDetails(CatalogEntry entry)
    {
        XmlNode definition = entry.Definition;
        var weapon = new Weapon(Character);
        weapon.Create(definition, Character, new TreeNode(), null, null, null);
        double cost = Convert.ToDouble(definition["cost"]!.InnerText, GlobalOptions.Instance.CultureInfo) * (1 + Markup / 100.0);
        int testCost = Convert.ToInt32(cost);
        if (FreeCost) { cost = 0; testCost = 0; }
        string source = definition["source"]!.InnerText;
        string page = definition["altpage"]?.InnerText ?? definition["page"]!.InnerText;
        var included = definition.SelectNodes("accessories/accessory|mods/mod")!.Cast<XmlNode>()
            .Select(node =>
            {
                string family = node.Name == "accessory" ? "accessories/accessory" : "mods/mod";
                XmlNode? item = Catalog.SelectNodes("/chummer/" + family)!.Cast<XmlNode>()
                    .FirstOrDefault(candidate => candidate["name"]?.InnerText == node.InnerText);
                return item?["translate"]?.InnerText ?? item?["name"]?.InnerText ?? node.InnerText;
            }).ToArray();
        return new WeaponPickerDetails(weapon.TotalReach.ToString(CultureInfo.CurrentCulture), weapon.CalculatedDamage(),
            weapon.TotalAP, weapon.CalculatedMode, weapon.TotalRC, weapon.CalculatedAmmo(), weapon.TotalAvail,
            string.Format(CultureInfo.CurrentCulture, "{0:###,###,##0¥}", cost), Character.AvailTest(testCost, weapon.TotalAvail),
            Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page,
            included.Length == 0 ? LanguageManager.Instance.GetString("String_None") : string.Join(Environment.NewLine, included));
    }
}
