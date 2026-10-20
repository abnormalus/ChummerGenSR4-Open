#nullable enable
using System;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace Chummer;

internal sealed record VehicleModPickerDetails(string Category, string Limit, string Availability,
    string Cost, string Slots, string AvailabilityTest, string Source, string SourceTooltip) : ICatalogPickerDetails;

// All context is captured at opening. Electronics and weapon eligibility reproduce
// the existing catalog policy; the model only prepares a selection, never inventory.
internal sealed class VehicleModPickerModel : CatalogPickerModel<VehicleModPickerDetails>, IRatedCatalogPicker, ILabeledRatingCatalogPicker
{
    private readonly VehicleModPurchaseContext context;
    private readonly bool weapons;
    private readonly string weaponType;
    private readonly int deviceRating, maxResponse, maxSignal;
    private int rating;
    private bool publishingRating;
    public int MinimumRating { get; private set; }
    public int MaximumRating { get; private set; }
    public bool IsRatingEnabled => CanAccept && MaximumRating > 0;
    public string RatingLabel { get; private set; } = "";
    public int Rating
    {
        get => rating;
        set { if (publishingRating) return; int next = Math.Clamp(value, MinimumRating, MaximumRating); if (next == rating) return; rating = next; UpdateDetails(); }
    }
    internal VehicleModPickerModel(Character character, bool career, string inputFile, string weaponType,
        VehicleModPurchaseContext context, int? deviceRating, bool modularElectronics, XmlDocument? catalog = null)
        : base(character, career, catalog ?? XmlManager.Instance.Load(inputFile + ".xml"), "/chummer/mods/mod", "", "vehicle-mod-picker", false)
    {
        this.context = context; weapons = inputFile == "weapons"; this.weaponType = weaponType;
        this.deviceRating = deviceRating ?? 0;
        maxResponse = maxSignal = modularElectronics ? 10 : deviceRating.HasValue ? deviceRating.Value + 2 : 0;
        // The old form's unset electronics limits are zero until DeviceRating is
        // supplied. Every vehicle caller supplies it; weapons have no such fields.
        Refresh();
    }
    protected override string DisplayName(XmlNode node) => weapons
        ? WeaponModificationNames.DisplayName(node["name"]!.InnerText, node["translate"]?.InnerText) : base.DisplayName(node);
    protected override bool MatchesSearch(XmlNode node, string text) =>
        DisplayName(node).IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
        node["name"]!.InnerText.IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0;
    protected override bool IncludeEntry(XmlNode node)
    {
        if (node["category"]?.InnerText == "Special" || weapons && !WeaponModificationNames.IsAvailableFor(node["name"]!.InnerText, weaponType)) return false;
        foreach (string field in new[] { "response", "signal", "system", "firewall" })
        {
            if (node[field] == null) continue;
            int value = Convert.ToInt32(node[field]!.InnerText, CultureInfo.InvariantCulture);
            if (value <= deviceRating || field == "response" && value > maxResponse || field == "signal" && value > maxSignal) return false;
        }
        return true;
    }
    protected override void RememberCategory(string category) { }
    protected override void NotifyAdditionalProperties()
    {
        publishingRating = true;
        try { foreach (string property in new[] { nameof(MinimumRating), nameof(MaximumRating), nameof(Rating), nameof(IsRatingEnabled), nameof(RatingLabel) }) Notify(property); }
        finally { publishingRating = false; }
    }
    protected override VehicleModPickerDetails CalculateDetails(CatalogEntry entry)
    {
        XmlNode node = entry.Definition;
        bool quantity = node["rating"]!.InnerText == "qty";
        MaximumRating = quantity ? 20 : Convert.ToInt32(node["rating"]!.InnerText, CultureInfo.InvariantCulture);
        MinimumRating = MaximumRating == 0 ? 0 : 1;
        rating = Math.Clamp(rating, MinimumRating, MaximumRating);
        RatingLabel = LanguageManager.Instance.GetString(quantity ? "Label_Qty" : "Label_Rating");
        XPathNavigator navigator = Catalog.CreateNavigator()!;
        string availability = node["avail"]!.InnerText;
        string suffix = availability.EndsWith("R", StringComparison.Ordinal) || availability.EndsWith("F", StringComparison.Ordinal) ? availability[^1..] : "";
        try
        {
            string expression = (suffix.Length == 0 ? availability : availability[..^1]).Replace("Rating", rating.ToString(CultureInfo.InvariantCulture));
            // Keep the legacy string-to-int conversion: fractional availability is
            // a literal fallback, unlike the separately rounded price calculation.
            availability = Convert.ToInt32(string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(expression))) + suffix;
        }
        catch (XPathException) { }
        catch (FormatException) { }
        catch (OverflowException) { }
        availability = availability.Replace("R", LanguageManager.Instance.GetString("String_AvailRestricted"))
            .Replace("F", LanguageManager.Instance.GetString("String_AvailForbidden"));
        string rawCost = node["cost"]!.InnerText, cost;
        int itemCost;
        if (rawCost.StartsWith("Variable", StringComparison.Ordinal))
        {
            string range = rawCost.Replace("Variable(", "").Replace(")", "");
            string[] bounds = range.Split('-');
            itemCost = Convert.ToInt32(bounds[0].Replace("+", ""), CultureInfo.InvariantCulture);
            int max = bounds.Length > 1 ? Convert.ToInt32(bounds[1], CultureInfo.InvariantCulture) : 0;
            cost = max == 0 ? string.Format("{0:###,###,##0¥+}", itemCost)
                : string.Format("{0:###,###,##0}", itemCost) + "-" + string.Format("{0:###,###,##0¥}", max);
            // Existing variable-price display/test ignores free/markup; the caller
            // still owns the mandatory price prompt and final financial decision.
        }
        else
        {
            itemCost = VehicleModPurchasePreview.FixedCost(navigator, rawCost, context, rating, Markup, FreeCost);
            cost = string.Format("{0:###,###,##0¥}", itemCost);
        }
        string category = node["category"]!.InnerText;
        if (category == "Weapon Mod") category = LanguageManager.Instance.GetString("String_WeaponModification");
        else foreach (XmlNode candidate in Catalog.SelectNodes("/chummer/modcategories/category")!)
            if (candidate.InnerText == category) { category = candidate.Attributes?["translate"]?.InnerText ?? category; break; }
        string limit = node["limit"]?.InnerText ?? "";
        foreach (XmlNode candidate in Catalog.SelectNodes("/chummer/limits/limit")!)
            if (candidate.InnerText == limit) { limit = candidate.Attributes?["translate"]?.InnerText ?? limit; break; }
        string source = node["source"]!.InnerText, page = node["altpage"]?.InnerText ?? node["page"]!.InnerText;
        return new VehicleModPickerDetails(category + (limit.Length == 0 ? "" : " (" + limit + ")"), limit.Length == 0 ? "" : " (" + limit + ")", availability, cost,
            string.Format(GlobalOptions.Instance.CultureInfo, "{0}", navigator.Evaluate(node["slots"]!.InnerText.Replace("Rating", rating.ToString(CultureInfo.InvariantCulture)))),
            Character.AvailTest(itemCost, availability), Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page);
    }
}

internal sealed class VehicleModPickerWindow : CatalogPickerWindow<VehicleModPickerModel, VehicleModPickerDetails>
{
    internal VehicleModPickerWindow(VehicleModPickerModel model, Character character, bool weapons)
        : base(model, character, weapons ? "Title_SelectVehicleMod_Weapon" : "Title_SelectVehicleMod", "vehicleMod", new[] {
            ("Label_Category", "Category"), ("Label_Avail", "Availability"),
            ("Label_Cost", "Cost"), ("Label_Slots", "Slots"), ("Label_Test", "AvailabilityTest") })
    { }
}
