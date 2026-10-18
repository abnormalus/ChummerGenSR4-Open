#nullable enable
using System;
using System.Linq;
using System.IO;
namespace Chummer;
internal static class CreationFinalizationChecks
{
    internal static void Run(string output, Action<bool, string> check)
    {
        var character = new Character { BuildMethod = CharacterBuildMethod.Karma, Karma = 45, Nuyen = 1234 };
        var groups = character.SkillGroups;
        // Match a live editor toggled to Ignore Rules after its group limits were built.
        character.IgnoreRules = true;
        var broken = groups.First(group => character.Skills.Any(item => item.SkillGroup == group.Name));
        var intact = groups.First(group => group != broken && character.Skills.Any(item => item.SkillGroup == group.Name));
        broken.Rating = 2; intact.Rating = 2;
        foreach (var item in character.Skills.Where(item => item.SkillGroup == broken.Name || item.SkillGroup == intact.Name)) item.Rating = 2;
        var skill = character.Skills.First(item => item.SkillGroup == broken.Name);
        skill.Rating = 3;
        var fallback = new Lifestyle(character);
        var selection = new CreationFinalizationSelection(character, 45, 20, 900, fallback);
        check(character.Karma == 45 && character.Nuyen == 1234 && !broken.Broken && !character.Lifestyles.Contains(fallback),
            "preparing accepted creation choices leaves money groups and lifestyles unchanged");
        var staged = selection.PrepareCharacterCopy();
        check(staged.Karma == 20 && staged.Nuyen == 900 && staged.SkillGroups.First(g => g.Name == broken.Name).Broken &&
              !ReferenceEquals(staged.SkillGroups.First(g => g.Name == broken.Name), broken) &&
              staged.SkillGroups.All(group => group.RatingMaximum == groups.Single(original => original.Name == group.Name).RatingMaximum) &&
              staged.Lifestyles.Any(item => item.InternalId == fallback.InternalId) &&
              character.Karma == 45 && character.Nuyen == 1234 && !broken.Broken && character.Lifestyles.Count == 0,
            "staged finalization copy preserves serialized identity without changing live references or money");
        var preparedSave = new PreparedCreationSave(character, selection, new DateTime(2072, 1, 1));
        string rejectedPath = Path.Combine(output, "synthetic-finalization-directory.chum");
        Directory.CreateDirectory(rejectedPath);
        preparedSave.Character.FileName = rejectedPath;
        bool writeRejected = false;
        try { preparedSave.Character.Save(); }
        catch (Exception error) when (error is IOException || error is UnauthorizedAccessException) { writeRejected = true; }
        check(writeRejected && !character.Created && character.Karma == 45 && character.Nuyen == 1234 &&
              character.ExpenseEntries.Count == 0 && character.Lifestyles.Count == 0 && !broken.Broken,
            "failed prepared career write leaves live creation money ledger lifestyle and groups unchanged");
        string acceptedPath = Path.Combine(output, "synthetic-finalization-retry.chum");
        preparedSave.Character.FileName = acceptedPath;
        preparedSave.Character.Save();
        var reopened = new Character { FileName = acceptedPath };
        check(reopened.Load(false) && reopened.Created && reopened.Karma == 20 && reopened.Nuyen == 900 &&
              reopened.ExpenseEntries.Count == 2 && reopened.ExpenseEntries.All(entry => entry.Undo != null) &&
              File.ReadAllBytes(acceptedPath).Take(2).SequenceEqual(new byte[] { 255, 254 }),
            "prepared career retry saves both starting ledger entries and undo in the legacy UTF16 payload");
        preparedSave.CommitToOwner(); preparedSave.CommitToOwner();
        check(character.Created && character.ExpenseEntries.Count == 2 && character.FileName == acceptedPath &&
              ReferenceEquals(character.Skills.First(item => item.Name == skill.Name), skill),
            "successful prepared career commit preserves live references and adds starting expenses once");
        selection.ApplyTo(character);
        check(character.Karma == 20 && character.Nuyen == 900 && broken.Broken && !intact.Broken &&
              ReferenceEquals(character.Skills.First(s => s.Name == skill.Name), skill) && ReferenceEquals(groups.First(group => group.Name == broken.Name), broken),
            "accepted creation choices retain skill references and existing Karma and group rules");
        selection.ApplyTo(character);
        check(character.Lifestyles.Count(l => ReferenceEquals(l, fallback)) == 1,
            "reapplying accepted creation choices does not duplicate the prepared lifestyle");
        var other = new Character { BuildMethod = CharacterBuildMethod.BP, Karma = 8, Nuyen = 100 };
        bool rejected = false;
        try { selection.ApplyTo(other); } catch (InvalidOperationException) { rejected = true; }
        check(rejected && other.Karma == 8 && other.Nuyen == 100,
            "accepted creation choices reject a different target before changing its money");
        new CreationFinalizationSelection(other, 50, 20, -1, null).ApplyTo(other);
        check(other.Karma == 8 && other.Nuyen == 0,
            "BP finalization preserves Karma and clamps negative starting money to zero");
        var ledgerOwner = new Character { BuildMethod = CharacterBuildMethod.BP, Nuyen = 25 };
        var existingEntry = new ExpenseLogEntry();
        existingEntry.Create(25, "Synthetic existing expense", ExpenseType.Nuyen, new DateTime(2071, 1, 1));
        var existingUndo = new ExpenseUndo(); existingUndo.CreateNuyen(NuyenExpenseType.ManualAdd, "");
        existingEntry.Undo = existingUndo; ledgerOwner.ExpenseEntries.Add(existingEntry);
        var ledgerSelection = new CreationFinalizationSelection(ledgerOwner, 0, 0, 50, null);
        var ledgerSave = new PreparedCreationSave(ledgerOwner, ledgerSelection, new DateTime(2072, 1, 1));
        ledgerSave.Character.FileName = Path.Combine(output, "synthetic-finalization-existing-ledger.chum");
        ledgerSave.Character.Save(); ledgerSave.CommitToOwner();
        check(ledgerOwner.ExpenseEntries.Count == 2 && ReferenceEquals(ledgerOwner.ExpenseEntries[0], existingEntry) &&
              ReferenceEquals(existingEntry.Undo, existingUndo) && ledgerOwner.Nuyen == 50,
            "prepared career commit retains existing expense and undo references alongside one new BP starting entry");

        var unsupported = new Character();
        _ = unsupported.SkillGroups.Count;
        unsupported.Skills.Add(new Skill(unsupported) { Name = "Synthetic unsupported active skill", Attribute = "AGI", SkillCategory = "Combat Active", Rating = 1 });
        var unsupportedSelection = new CreationFinalizationSelection(unsupported, 0, 0, 0, null);
        byte[] unsupportedBefore = unsupported.CaptureSnapshot();
        bool normalizedCopyRejected = false;
        try { unsupportedSelection.PrepareCharacterCopy(); }
        catch (InvalidOperationException) { normalizedCopyRejected = true; }
        check(normalizedCopyRejected && unsupportedBefore.AsSpan().SequenceEqual(unsupported.CaptureSnapshot()),
            "staged creation refuses snapshot normalization without changing unsupported live state");
    }
}
