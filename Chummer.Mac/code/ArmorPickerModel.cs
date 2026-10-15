#nullable enable
using System;
using System.Globalization;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal sealed record ArmorPickerDetails(string Ballistic, string Impact, string Capacity,
    string Availability, string Cost, string AvailabilityTest, string Source, string SourceTooltip) : ICatalogPickerDetails;

internal sealed class ArmorPickerModel : CatalogPickerModel<ArmorPickerDetails>
{
    internal static string LastCategory { get; set; } = "";
    internal ArmorPickerModel(Character character, bool career, XmlDocument? catalog = null)
        : base(character, career, catalog ?? XmlManager.Instance.Load("armor.xml"),
            "/chummer/armors/armor", LastCategory, "armor-picker") { Refresh(); }
    protected override void RememberCategory(string category) => LastCategory = category;

    protected override ArmorPickerDetails CalculateDetails(CatalogEntry entry)
    {
        XmlNode definition = entry.Definition;
        var armor = new Armor(Character);
        // Preview mode skips cost/detail prompts and improvements. Build included
        // items so the established core availability handling remains authoritative.
        armor.Create(definition, new TreeNode(), null, true);
        string ballistic = definition["b"]!.InnerText, impact = definition["i"]!.InnerText;
        string capacity = definition["armorcapacity"]!.InnerText;
        if (capacity is "" or "0")
            capacity = Math.Max(6, Math.Max(Math.Ceiling(Convert.ToDouble(ballistic, GlobalOptions.Instance.CultureInfo) * 1.5),
                Math.Ceiling(Convert.ToDouble(impact, GlobalOptions.Instance.CultureInfo) * 1.5))).ToString(CultureInfo.CurrentCulture);
        string rawCost = definition["cost"]!.InnerText;
        int testCost;
        string cost;
        if (rawCost.StartsWith("Variable", StringComparison.Ordinal))
        {
            string[] limits = rawCost.Replace("Variable(", "").Replace(")", "").Split('-');
            int minimum = Convert.ToInt32(limits[0].Replace("+", ""), CultureInfo.InvariantCulture);
            int maximum = limits.Length > 1 ? Convert.ToInt32(limits[1], CultureInfo.InvariantCulture) : 0;
            // The actual purchase prompts for a price later. Keep the established
            // preview range and availability test at its minimum, without applying markup.
            cost = maximum == 0 ? string.Format("{0:###,###,##0¥+}", minimum)
                : string.Format("{0:###,###,##0}-{1:###,###,##0¥}", minimum, maximum);
            testCost = minimum;
        }
        else
        {
            double markedUpCost = Convert.ToDouble(rawCost, GlobalOptions.Instance.CultureInfo) * (1 + Markup / 100.0);
            cost = string.Format("{0:###,###,##0¥}", markedUpCost); testCost = Convert.ToInt32(markedUpCost);
        }
        if (FreeCost) { cost = string.Format("{0:###,###,##0¥}", 0); testCost = 0; }
        string source = definition["source"]!.InnerText;
        string page = definition["altpage"]?.InnerText ?? definition["page"]!.InnerText;
        return new ArmorPickerDetails(ballistic, impact, capacity, armor.TotalAvail, cost,
            Character.AvailTest(testCost, armor.TotalAvail), Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page);
    }
}
