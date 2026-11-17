#nullable enable
using System;

namespace Chummer;

// Final installed movement arithmetic; no model, control, persistence or resource ownership.
internal static class VehicleMovementValue
{
    // A null base means no armor penalty. When eligible, subtract 20% of the
    // original base, not 20% of the modified total. Preserve decimal precision
    // until the legacy zero clamp and upward integer rounding are complete.
    internal static int Calculate(decimal modifiedValue, int? armorPenaltyBase)
    {
        if (armorPenaltyBase.HasValue) modifiedValue -= armorPenaltyBase.Value * 0.2m;
        if (modifiedValue < 0m) modifiedValue = 0m;
        return Convert.ToInt32(Math.Ceiling(modifiedValue));
    }
}
