#nullable enable
namespace Chummer;

internal sealed class ArmorPickerWindow : CatalogPickerWindow<ArmorPickerModel, ArmorPickerDetails>
{
    internal ArmorPickerWindow(ArmorPickerModel model, Character character)
        : base(model, character, "Title_SelectArmor", "armor", new[] {
            ("Label_BallisticArmorLong", "Ballistic"), ("Label_ImpactArmorLong", "Impact"),
            ("Label_Capacity", "Capacity"), ("Label_Avail", "Availability"),
            ("Label_Cost", "Cost"), ("Label_Test", "AvailabilityTest") }) { }
}
