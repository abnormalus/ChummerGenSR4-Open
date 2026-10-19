#nullable enable
using System;
using System.Globalization;
using System.Xml.XPath;

namespace Chummer;

// A purchase-context snapshot keeps legacy expression evaluation outside the native
// view. Callers still own inventory creation, variable-price choices and accounting.
internal sealed record VehicleModPurchaseContext(int VehicleCost, int WeaponCost, int TotalWeaponCost,
    int Body, int Speed, int AccelRunning, int AccelWalking, int ModMultiplier = 1)
{
    internal string Expand(string expression, int rating) => expression
        .Replace("Rating", rating.ToString(CultureInfo.InvariantCulture))
        .Replace("Vehicle Cost", VehicleCost.ToString(CultureInfo.InvariantCulture))
        .Replace("Weapon Cost", WeaponCost.ToString(CultureInfo.InvariantCulture))
        .Replace("Total Cost", TotalWeaponCost.ToString(CultureInfo.InvariantCulture))
        .Replace("Body", Body.ToString(CultureInfo.InvariantCulture))
        .Replace("Speed", Speed.ToString(CultureInfo.InvariantCulture))
        .Replace("Accel(Running)", AccelRunning.ToString(CultureInfo.InvariantCulture))
        .Replace("Accel", AccelWalking.ToString(CultureInfo.InvariantCulture));
}

internal static class VehicleModPurchasePreview
{
    // Two integer conversions are intentional: fractional catalog costs round before
    // the weapon multiplier and markup, then markup rounds again (midpoint to even).
    internal static int FixedCost(XPathNavigator navigator, string expression,
        VehicleModPurchaseContext context, int rating, int markup, bool free)
    {
        string expanded = free ? "0" : context.Expand(expression, rating);
        int cost = Convert.ToInt32(Convert.ToDouble(navigator.Evaluate(expanded), CultureInfo.InvariantCulture));
        cost *= context.ModMultiplier;
        return Convert.ToInt32(cost * (1 + markup / 100.0));
    }
}
