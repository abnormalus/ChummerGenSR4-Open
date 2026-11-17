using System;

namespace Chummer;

// Pure budget arithmetic. Callers supply the already-calculated power costs:
// discounts and attribute-dependent double costs remain owned by Power.
// This value neither mutates a character nor owns presentation/lifecycle state.
internal readonly record struct AdeptPowerPointBudget(int Available, decimal Remaining)
{
    internal static AdeptPowerPointBudget Calculate(bool mysticAdept, int fullMagic,
        int adeptMagic, int bonusPoints, decimal spent)
    {
        int available = (mysticAdept ? adeptMagic : fullMagic) + bonusPoints;
        return new AdeptPowerPointBudget(available, available - spent);
    }
}

// Culture and localization belong to the summary presentation boundary, not
// budget arithmetic. Preserve the legacy length-limited decimal trimming,
// including -1.0 and 4.00, until a separate behavior change is approved/tested.
internal static class AdeptPowerPointSummary
{
    internal static string Format(AdeptPowerPointBudget budget, string remainingLabel)
    {
        string remaining = budget.Remaining.ToString();
        while (remaining.EndsWith("0") && remaining.Length > 4)
            remaining = remaining.Substring(0, remaining.Length - 1);
        return String.Format("{1} ({0} " + remainingLabel + ")", remaining, budget.Available);
    }
}
