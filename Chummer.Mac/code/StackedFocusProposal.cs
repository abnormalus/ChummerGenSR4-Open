#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;

namespace Chummer;

// A modal selection is only a proposal. Validation and all value construction
// precede owned model writes; cancellation discards the proposal without edits.
// This component owns no controls, prompts, persistence or expense accounting.
internal sealed class StackedFocusProposal
{
    internal enum Rejection { None, Minimum, ForceLimit, StaleSelection }
    internal sealed record CommitResult(StackedFocus Stack, Gear Item, IReadOnlyList<Gear> Components);
    private readonly Gear[] selected;
    internal StackedFocusProposal(IEnumerable<Gear> selected) => this.selected = selected.ToArray();

    internal Rejection Validate(Character character, bool allowHigherForce)
    {
        if (selected.Length < 2) return Rejection.Minimum;
        // Identity means the actual owned object, not a name or a copied GUID.
        // Revalidate after the modal sequence, before changing any component.
        if (selected.Distinct(ReferenceEqualityComparer.Instance).Count() != selected.Length || selected.Any(g =>
            !character.Gear.Any(owned => ReferenceEquals(owned, g)) || g.Bonded || (g.Category != "Foci" && g.Category != "Metamagic Foci")))
            return Rejection.StaleSelection;
        int force = 0;
        foreach (var component in selected) force = unchecked(force + component.Rating);
        if (!allowHigherForce && force > 6) return Rejection.ForceLimit;
        return Rejection.None;
    }

    internal CommitResult Commit(Character character, bool allowHigherForce)
    {
        var rejection = Validate(character, allowHigherForce);
        if (rejection != Rejection.None) throw new InvalidOperationException("Stacked-focus proposal rejected: " + rejection);
        // Cost and display-name evaluation can fail for custom definitions. Read
        // these and construct both records before changing owned collections.
        // No rollback of unrelated global effects or allocation failures is promised.
        int cost = 0;
        foreach (var component in selected) cost = unchecked(cost + component.TotalCost);
        var stack = new StackedFocus(character) { Gear = selected.ToList() };
        var item = new Gear(character)
        {
            Category = "Stacked Focus", Name = "Stacked Focus: " + stack.Name,
            MinRating = 0, MaxRating = 0, Source = "SM", Page = "84", Cost = cost.ToString(), Avail = "0"
        };
        stack.GearId = item.InternalId;
        foreach (var component in selected) component.Bonded = true;
        character.StackedFoci.Add(stack);
        foreach (var component in selected) character.Gear.Remove(component);
        character.Gear.Add(item);
        return new CommitResult(stack, item, Array.AsReadOnly(selected));
    }
}
