#nullable enable
using System;
using System.Linq;

namespace Chummer;

// Independent model contracts complement the recorded original editor baseline.
internal static class StackedFocusProposalChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var character = new Character();
        var first = new Gear(character) { Name = "First", Category = "Foci", MaxRating = 6, Rating = 4, Cost = "100" };
        var second = new Gear(character) { Name = "Second", Category = "Metamagic Foci", MaxRating = 6, Rating = 3, Cost = "200" };
        character.Gear.Add(first); character.Gear.Add(second);
        byte[] before = character.CaptureSnapshot();
        var one = new StackedFocusProposal(new[] { first });
        check(one.Validate(character, false) == StackedFocusProposal.Rejection.Minimum && before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
            "Focus proposal minimum rejection leaves a selected component unbonded and snapshot unchanged");
        var proposal = new StackedFocusProposal(new[] { second, first });
        check(proposal.Validate(character, false) == StackedFocusProposal.Rejection.ForceLimit && before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
            "Focus proposal rejects fixed combined Force7 without model writes");
        bool rejected = false; try { proposal.Commit(character, false); } catch (InvalidOperationException) { rejected = true; }
        check(rejected && before.AsSpan().SequenceEqual(character.CaptureSnapshot()), "Focus rejected commit leaves the full character unchanged");
        check(new StackedFocusProposal(new[] { first, first }).Validate(character, true) == StackedFocusProposal.Rejection.StaleSelection,
            "Focus proposal rejects duplicate object selection");
        character.Gear.Remove(second);
        check(proposal.Validate(character, true) == StackedFocusProposal.Rejection.StaleSelection && !first.Bonded && !second.Bonded,
            "Focus proposal revalidates removed owned components");
        character.Gear.Add(second); second.Bonded = true;
        check(proposal.Validate(character, true) == StackedFocusProposal.Rejection.StaleSelection && character.StackedFoci.Count == 0,
            "Focus proposal rejects a component bonded while selection was pending");
        second.Bonded = false;
        first.Cost = "invalid"; byte[] malformed = character.CaptureSnapshot(); bool costRejected = false;
        try { proposal.Commit(character, true); } catch (FormatException) { costRejected = true; }
        check(costRejected && malformed.AsSpan().SequenceEqual(character.CaptureSnapshot()) && !first.Bonded && !second.Bonded && character.StackedFoci.Count == 0,
            "Focus malformed custom cost rejects before bonding or transferring owned components");
        first.Cost = "100";
        int money = character.Nuyen, expenses = character.ExpenseEntries.Count;
        var result = proposal.Commit(character, true);
        check(result.Components.SequenceEqual(new[] { second, first }) && ReferenceEquals(result.Stack.Gear[0], second) && ReferenceEquals(result.Stack.Gear[1], first) &&
            first.Bonded && second.Bonded && !result.Stack.Bonded && character.Gear.Single() == result.Item && character.StackedFoci.Single() == result.Stack,
            "Focus allowed Force7 commit transfers original ordered components to one unbonded stack");
        check(result.Item.Cost == "300" && result.Item.Category == "Stacked Focus" && result.Item.Source == "SM" && result.Item.Page == "84" &&
            result.Stack.GearId == result.Item.InternalId && character.Nuyen == money && character.ExpenseEntries.Count == expenses,
            "Focus proposal commit retains fixed linked cost metadata and leaves accounting to the caller");
    }
}
