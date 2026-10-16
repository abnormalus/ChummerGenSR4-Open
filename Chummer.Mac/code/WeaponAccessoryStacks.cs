#nullable enable
namespace Chummer;

// Only discrete spare ammunition containers can be bought as a quantity.
// Installed upgrades retain one-unit rules; historical 2050 definitions remain canonical.
internal static class WeaponAccessoryStacks
{
    internal static bool SupportsQuantity(string name) => name is "Spare Clips" or "Speed Loader" or "Ammo Drum/Box" or
        "Spare Clips (2050)" or "Speed Loader (2050)";
}
