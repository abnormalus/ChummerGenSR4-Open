#nullable enable
using System;
using System.Collections.Generic;
using System.Xml;

namespace Chummer;

internal enum InventoryCopyTarget
{
    None, Lifestyle, Armor, Weapon, Gear, Vehicle
}

/// <summary>Resolves and copies a selected inventory item without depending on editor controls.</summary>
internal static class InventoryCopyCommand
{
    // Returns whether the caller should refresh Paste, including the legacy no-op
    // cases. Gear still publishes its type before serializing a linked weapon.
    internal static bool Copy(Character character, CommonFunctions lookup, InventoryCopyTarget target,
        object? selectedTag, int selectedLevel)
    {
        try
        {
            if (selectedTag?.ToString() is not { } id) return true;
            switch (target)
            {
                case InventoryCopyTarget.Lifestyle:
                    var lifestyle = lookup.FindLifestyle(id, character.Lifestyles);
                    if (lifestyle == null) return false;
                    Publish(ClipboardContentType.Lifestyle, writer => lifestyle.Save(writer));
                    break;

                case InventoryCopyTarget.Armor:
                    var armor = lookup.FindArmor(id, character.Armor);
                    if (armor != null) Publish(ClipboardContentType.Armor, writer => armor.Save(writer));
                    else CopyGear(character, lookup, lookup.FindArmorGear(id, character.Armor, out _));
                    break;

                case InventoryCopyTarget.Weapon:
                    var weapon = lookup.FindWeapon(id, character.Weapons);
                    if (weapon != null) return CopyWeapon(weapon);
                    CopyGear(character, lookup, lookup.FindWeaponGear(id, character.Weapons, out _));
                    break;

                case InventoryCopyTarget.Gear:
                    var gear = lookup.FindGear(id, character.Gear);
                    if (gear == null) return false;
                    CopyGear(character, lookup, gear);
                    break;

                case InventoryCopyTarget.Vehicle:
                    if (selectedLevel == 1)
                    {
                        var vehicle = lookup.FindVehicle(id, character.Vehicles);
                        if (vehicle != null) Publish(ClipboardContentType.Vehicle, writer => vehicle.Save(writer));
                        break;
                    }

                    var vehicleGear = lookup.FindVehicleGear(id, character.Vehicles, out _);
                    if (vehicleGear != null) CopyGear(character, lookup, vehicleGear);
                    else
                    {
                        var mounted = FindMountedWeapon(character.Vehicles, lookup, id);
                        if (mounted != null) return CopyWeapon(mounted);
                    }
                    break;
            }
        }
        catch
        {
            // Keep the existing Copy failure policy and clipboard publication order.
        }
        return true;
    }

    private static Weapon? FindMountedWeapon(IEnumerable<Vehicle> vehicles, CommonFunctions lookup, string id)
    {
        foreach (var vehicle in vehicles)
            foreach (var mod in vehicle.Mods)
                if (lookup.FindWeapon(id, mod.Weapons) is { } weapon) return weapon;
        return null;
    }

    private static bool CopyWeapon(Weapon weapon)
    {
        if (weapon.Category == "Gear" || weapon.Category.StartsWith("Cyberware")) return false;
        Publish(ClipboardContentType.Weapon, writer => weapon.Save(writer));
        return true;
    }

    private static void Publish(ClipboardContentType type, Action<XmlTextWriter> write)
    {
        var clipboard = GlobalOptions.Instance;
        clipboard.Clipboard = ClipboardDocument.Create(write);
        clipboard.ClipboardContentType = type;
    }

    private static void CopyGear(Character character, CommonFunctions lookup, Gear? gear)
    {
        if (gear == null) return;
        var clipboard = GlobalOptions.Instance;
        clipboard.Clipboard = ClipboardDocument.Create(writer =>
        {
            // Gear.Save is hidden by these two concrete types.
            if (gear.GetType() == typeof(Commlink))
            {
                ((Commlink)gear).Save(writer);
                clipboard.ClipboardContentType = ClipboardContentType.Commlink;
            }
            else if (gear.GetType() == typeof(OperatingSystem))
            {
                ((OperatingSystem)gear).Save(writer);
                clipboard.ClipboardContentType = ClipboardContentType.OperatingSystem;
            }
            else
            {
                gear.Save(writer);
                clipboard.ClipboardContentType = ClipboardContentType.Gear;
            }

            if (gear.WeaponID != Guid.Empty.ToString())
                lookup.FindWeapon(gear.WeaponID, character.Weapons).Save(writer);
        });
    }
}
