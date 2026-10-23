#nullable enable
using System;
using System.Linq;
using System.Xml;

namespace Chummer;

internal sealed record QualityPickerDetails(string Cost, string Source, string SourceTooltip) : ICatalogPickerDetails;

// Unattached selection state: the caller still creates, pays for and adds qualities.
internal sealed class QualityPickerModel : CatalogPickerModel<QualityPickerDetails>
{
    private static string lastCategory = "";
    private readonly XmlDocument metatypes;
    private readonly QualityEligibilityPolicy eligibility;
    private readonly string forcedCategory;
    private bool limitList = true;
    private bool metageneticOnly;
    public bool LimitList { get => limitList; set { if (limitList == value) return; limitList = value; Refresh(); } }
    public bool MetageneticOnly { get => metageneticOnly; set { if (metageneticOnly == value) return; metageneticOnly = value; Refresh(); } }
    public bool CanFilterMetagenetic { get; }
    protected override bool AllowsFreeCost => true;

    internal QualityPickerModel(Character character, string forcedCategory = "", string ignoredQuality = "")
        : base(character, character.Created, XmlManager.Instance.Load("qualities.xml"),
            "/chummer/qualities/quality", forcedCategory.Length == 0 ? lastCategory : forcedCategory, "quality-picker")
    {
        this.forcedCategory = forcedCategory;
        metatypes = XmlManager.Instance.Load("metatypes.xml");
        eligibility = new QualityEligibilityPolicy(character, Catalog, metatypes,
            XmlManager.Instance.Load("critters.xml"), ignoredQuality);
        var dummy = Catalog.SelectSingleNode("/chummer/qualities/quality[name = '360-degree Eyesight']")!.Clone();
        dummy["name"]!.InnerText = "Dummy Quality";
        CanFilterMetagenetic = eligibility.RequirementMet(dummy, false);
        Refresh();
    }

    protected override bool IncludeEntry(XmlNode definition)
    {
        if (forcedCategory.Length > 0 && definition["category"]?.InnerText != forcedCategory) return false;
        if (MetageneticOnly && definition.SelectSingleNode("required/oneof[contains(., 'Changeling (Class I SURGE)')] | metagenetic[. = 'yes']") == null) return false;
        if (!LimitList) return true;
        if (!eligibility.RequirementMet(definition, false)) return false;
        // Retain the legacy category-list restriction, including its separate search behavior.
        if (SearchText.Trim().Length == 0 && (Character.Metatype == "A.I." ||
            Character.MetatypeCategory == "Technocritters" || Character.MetatypeCategory == "Protosapients"))
        {
            var metatype = metatypes.SelectNodes("/chummer/metatypes/metatype")!.Cast<XmlNode>()
                .FirstOrDefault(node => node["name"]?.InnerText == Character.Metatype);
            return metatype?.SelectNodes("qualityrestriction/" + definition["category"]!.InnerText.ToLowerInvariant() + "/quality")!
                .Cast<XmlNode>().Any(node => node.InnerText == definition["name"]!.InnerText) == true;
        }
        return true;
    }

    protected override bool ValidateSelection(CatalogEntry entry) => eligibility.RequirementMet(entry.Definition, true);
    protected override void RememberCategory(string category) => lastCategory = category;
    protected override void NotifyAdditionalProperties()
    {
        Notify(nameof(LimitList)); Notify(nameof(MetageneticOnly)); Notify(nameof(CanFilterMetagenetic));
    }
    protected override QualityPickerDetails CalculateDetails(CatalogEntry entry)
    {
        var definition = entry.Definition;
        int cost = Convert.ToInt32(definition["bp"]!.InnerText);
        if (Character.BuildMethod == CharacterBuildMethod.Karma || Character.Created) cost *= Character.Options.KarmaQuality;
        string source = definition["source"]!.InnerText;
        string page = definition["altpage"]?.InnerText ?? definition["page"]!.InnerText;
        return new QualityPickerDetails((FreeCost ? 0 : cost).ToString(), Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page);
    }
}
