#nullable enable
using Avalonia.Controls;
namespace Chummer;

internal sealed class WeaponPickerWindow : CatalogPickerWindow<WeaponPickerModel, WeaponPickerDetails>
{
    internal ListBox WeaponList => ItemList;
    internal WeaponPickerWindow(WeaponPickerModel model, Character character)
        : base(model, character, "Title_SelectWeapon", "weapon", new[] {
            ("Label_Damage", "Damage"), ("Label_AP", "ArmorPenetration"), ("Label_Reach", "Reach"),
            ("Label_Mode", "Mode"), ("Label_RC", "RecoilCompensation"), ("Label_Ammo", "Ammo"),
            ("Label_Avail", "Availability"), ("Label_Cost", "Cost"), ("Label_Test", "AvailabilityTest") },
            "Label_SelectWeapon_IncludedItems") { }
}
