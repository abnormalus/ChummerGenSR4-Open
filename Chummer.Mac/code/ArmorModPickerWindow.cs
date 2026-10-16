#nullable enable
namespace Chummer;

internal sealed class ArmorModPickerWindow : CatalogPickerWindow<ArmorModPickerModel, ArmorModPickerDetails>
{
    internal ArmorModPickerWindow(ArmorModPickerModel model, Character character)
        : base(model, character, "Title_SelectArmorMod", "armorMod", new[] {
            ("Label_BallisticArmorLong", "Ballistic"), ("Label_ImpactArmorLong", "Impact"),
            ("Label_Capacity", "Capacity"), ("Label_Avail", "Availability"),
            ("Label_Cost", "Cost"), ("Label_Test", "AvailabilityTest") })
    { }
}
