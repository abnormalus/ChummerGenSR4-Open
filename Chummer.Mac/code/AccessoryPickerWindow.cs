#nullable enable
namespace Chummer;

internal sealed class AccessoryPickerWindow : CatalogPickerWindow<AccessoryPickerModel, AccessoryPickerDetails>
{
    internal AccessoryPickerWindow(AccessoryPickerModel model, Character character)
        : base(model, character, "Title_SelectWeaponAccessory", "accessory", new[] {
            ("Label_RC", "Recoil"), ("Label_Mount", "Mount"), ("Label_Avail", "Availability"),
            ("Label_Cost", "Cost"), ("Label_Test", "AvailabilityTest") }) { }
}
