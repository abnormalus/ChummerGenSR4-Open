#nullable enable
using System;
using System.Linq;
using System.Globalization;
using System.Xml;
using System.Xml.XPath;

namespace Chummer;

internal sealed record ArmorModPickerDetails(string Ballistic, string Impact, string Capacity,
    string Availability, string Cost, string AvailabilityTest, string Source, string SourceTooltip) : ICatalogPickerDetails;

internal sealed class ArmorModPickerModel : CatalogPickerModel<ArmorModPickerDetails>, IRatedCatalogPicker
{
    private readonly string[] allowedCategories;
    private readonly int armorCost;
    private readonly CapacityStyle capacityStyle;
    private int rating = 1;
    private bool publishingRating;
    public int MinimumRating => 1;
    public int MaximumRating { get; private set; } = 1;
    public bool IsRatingEnabled => CanAccept && MaximumRating > MinimumRating;
    public int Rating
    {
        get => rating;
        set
        {
            if (publishingRating) return;
            int next = Math.Clamp(value, MinimumRating, MaximumRating);
            if (rating == next) return;
            rating = next; UpdateDetails();
        }
    }
    internal ArmorModPickerModel(Character character, bool career, string categories, int armorCost, CapacityStyle style, XmlDocument? catalog = null)
        : base(character, career, catalog ?? XmlManager.Instance.Load("armor.xml"), "/chummer/mods/mod", "", "armor-mod-picker", false)
    {
        allowedCategories = categories.Split(',', StringSplitOptions.RemoveEmptyEntries);
        this.armorCost = armorCost; capacityStyle = style; Refresh();
    }
    protected override bool IncludeEntry(XmlNode definition) => definition["category"]?.InnerText == "General" ||
        allowedCategories.Contains(definition["category"]?.InnerText, StringComparer.Ordinal);
    protected override void RememberCategory(string category) { }
    protected override void NotifyAdditionalProperties()
    {
        // Publishing new limits can cause a control to coerce its old value. The
        // model already clamped the rating; ignore that transient binding feedback.
        publishingRating = true;
        try { foreach (string property in new[] { nameof(MinimumRating), nameof(MaximumRating), nameof(Rating), nameof(IsRatingEnabled) }) Notify(property); }
        finally { publishingRating = false; }
    }
    protected override ArmorModPickerDetails CalculateDetails(CatalogEntry entry)
    {
        XmlNode definition = entry.Definition;
        MaximumRating = Math.Max(1, Convert.ToInt32(definition["maxrating"]!.InnerText, CultureInfo.InvariantCulture));
        rating = Math.Clamp(rating, MinimumRating, MaximumRating);
        XPathNavigator navigator = Catalog.CreateNavigator()!;
        string Expression(string value) => value.Replace("Rating", rating.ToString(CultureInfo.InvariantCulture))
            .Replace("Armor Cost", armorCost.ToString(CultureInfo.InvariantCulture));
        string availability = definition["avail"]!.InnerText;
        string suffix = availability.EndsWith("R", StringComparison.Ordinal) || availability.EndsWith("F", StringComparison.Ordinal) ? availability[^1..] : "";
        try { availability = Convert.ToInt32(navigator.Evaluate(Expression(suffix.Length == 0 ? availability : availability[..^1])), CultureInfo.InvariantCulture) + suffix; }
        catch (XPathException) { /* Preserve literal catalog availability when it is not a numeric expression. */ }
        catch (FormatException) { /* Same established fallback for literal availability. */ }
        availability = availability.Replace("R", LanguageManager.Instance.GetString("String_AvailRestricted"))
            .Replace("F", LanguageManager.Instance.GetString("String_AvailForbidden"));
        double price = Convert.ToDouble(navigator.Evaluate(Expression(definition["cost"]!.InnerText)), CultureInfo.InvariantCulture) * (1 + Markup / 100.0);
        string capacity = definition["armorcapacity"]!.InnerText;
        if (capacity.StartsWith("FixedValues(", StringComparison.Ordinal)) capacity = capacity[12..^1].Split(',')[rating - 1];
        capacity = capacityStyle == CapacityStyle.Zero ? "0" : capacityStyle == CapacityStyle.PerRating ? rating.ToString(CultureInfo.CurrentCulture)
            : Convert.ToString(navigator.Evaluate(Expression(capacity[1..^1])), GlobalOptions.Instance.CultureInfo)!;
        string source = definition["source"]!.InnerText, page = definition["altpage"]?.InnerText ?? definition["page"]!.InnerText;
        // As in the old picker, free cost affects the price display while the
        // availability test uses the marked-up catalog cost. Purchase owns charging.
        return new ArmorModPickerDetails(definition["b"]!.InnerText, definition["i"]!.InnerText, "[" + capacity + "]", availability,
            string.Format("{0:###,###,##0¥}", FreeCost ? 0 : price), Character.AvailTest(Convert.ToInt32(price), availability),
            Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page);
    }
}
