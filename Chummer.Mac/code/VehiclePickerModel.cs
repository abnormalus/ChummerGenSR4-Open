#nullable enable
using System;
using System.Globalization;
using System.Xml;
namespace Chummer;

// UsedCost is the saved vehicle price BEFORE career markup/restriction multipliers.
// Keep the legacy ceiling boundary separate from the later midpoint-to-even markup.
internal static class VehiclePurchasePreview
{
    internal static int UsedCost(int cost, int discount) => Convert.ToInt32(Math.Ceiling((double)cost * (double)(1 - (decimal)discount / 100)));
    internal static int MarkedUpCost(int cost, int markup) => Convert.ToInt32((double)cost * (1 + markup / 100.0));
    internal static string UsedAvailability(string availability)
    {
        string suffix = availability.EndsWith("R", StringComparison.Ordinal) || availability.EndsWith("F", StringComparison.Ordinal) ? availability[^1..] : "";
        return (Convert.ToInt32(suffix.Length == 0 ? availability : availability[..^1], CultureInfo.InvariantCulture) + 4).ToString(CultureInfo.InvariantCulture) + suffix;
    }
}
internal sealed record VehiclePickerDetails(string Handling, string Acceleration, string Speed, string Pilot, string Body, string Armor, string Sensor,
    string Availability, string Cost, string AvailabilityTest, string Source, string SourceTooltip) : ICatalogPickerDetails;

internal sealed class VehiclePickerModel : CatalogPickerModel<VehiclePickerDetails>
{
    internal static string LastCategory { get; set; } = "";
    private bool usedVehicle;
    private int usedDiscount = 20;
    public bool UsedVehicle { get => usedVehicle; set { if (usedVehicle == value) return; usedVehicle = value; UpdateDetails(); } }
    public int UsedDiscount { get => usedDiscount; set { value = Math.Clamp(value, 20, 50); if (usedDiscount == value) return; usedDiscount = value; UpdateDetails(); } }
    internal int UsedCost => SelectedEntry == null ? 0 : VehiclePurchasePreview.UsedCost(Convert.ToInt32(SelectedEntry.Definition["cost"]!.InnerText, CultureInfo.InvariantCulture), usedDiscount);
    internal string UsedAvailability => SelectedEntry == null ? "" : VehiclePurchasePreview.UsedAvailability(SelectedEntry.Definition["avail"]!.InnerText);
    internal VehiclePickerModel(Character character, bool career, XmlDocument? catalog = null)
        : base(character, career, catalog ?? XmlManager.Instance.Load("vehicles.xml"), "/chummer/vehicles/vehicle", LastCategory, "vehicle-picker") { Refresh(); }
    protected override void RememberCategory(string category) => LastCategory = category;
    protected override void NotifyAdditionalProperties() { Notify(nameof(UsedVehicle)); Notify(nameof(UsedDiscount)); }
    protected override VehiclePickerDetails CalculateDetails(CatalogEntry entry)
    {
        XmlNode definition = entry.Definition;
        int baseCost = Convert.ToInt32(definition["cost"]!.InnerText, CultureInfo.InvariantCulture);
        // Preview historically ceilings even new base prices; integer prices are unchanged.
        int cost = usedVehicle ? VehiclePurchasePreview.UsedCost(baseCost, usedDiscount) : baseCost;
        cost = FreeCost ? 0 : VehiclePurchasePreview.MarkedUpCost(cost, Markup);
        string availability = usedVehicle ? VehiclePurchasePreview.UsedAvailability(definition["avail"]!.InnerText) : definition["avail"]!.InnerText;
        availability = availability.Replace("R", LanguageManager.Instance.GetString("String_AvailRestricted")).Replace("F", LanguageManager.Instance.GetString("String_AvailForbidden"));
        string source = definition["source"]!.InnerText, page = definition["altpage"]?.InnerText ?? definition["page"]!.InnerText;
        return new VehiclePickerDetails(definition["handling"]!.InnerText, definition["accel"]!.InnerText, definition["speed"]!.InnerText,
            definition["pilot"]!.InnerText, definition["body"]!.InnerText, definition["armor"]!.InnerText, definition["sensor"]!.InnerText,
            availability, string.Format("{0:###,###,##0¥}", cost), Character.AvailTest(cost, availability),
            Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page);
    }
}
