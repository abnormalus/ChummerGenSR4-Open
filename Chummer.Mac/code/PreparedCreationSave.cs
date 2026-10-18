#nullable enable
using System;
using System.Linq;
namespace Chummer;

/// <summary>Owns the complete career payload until its single disk write succeeds.</summary>
internal sealed class PreparedCreationSave
{
    private readonly Character owner;
    private readonly CreationFinalizationSelection selection;
    private readonly ExpenseLogEntry[] startingEntries;
    internal Character Character { get; }

    internal PreparedCreationSave(Character owner, CreationFinalizationSelection selection, DateTime timestamp)
    {
        this.owner = owner;
        this.selection = selection;
        Character = selection.PrepareCharacterCopy();
        int existingEntries = Character.ExpenseEntries.Count;
        Character.Created = true;
        if (Character.BuildMethod == CharacterBuildMethod.Karma && Character.Karma > 0)
        {
            var karma = new ExpenseLogEntry();
            karma.Create(Character.Karma, "Starting Karma", ExpenseType.Karma, timestamp);
            var undo = new ExpenseUndo(); undo.CreateKarma(KarmaExpenseType.ManualAdd, "");
            karma.Undo = undo; Character.ExpenseEntries.Add(karma);
        }
        if (Character.EssencePenalty > 0)
        {
            if (Character.MAGEnabled) Character.MAG.Value += Character.EssencePenalty;
            if (Character.RESEnabled) Character.RES.Value += Character.EssencePenalty;
        }
        var nuyen = new ExpenseLogEntry();
        nuyen.Create(Character.Nuyen, "Starting Nuyen", ExpenseType.Nuyen, timestamp);
        var nuyenUndo = new ExpenseUndo(); nuyenUndo.CreateNuyen(NuyenExpenseType.ManualAdd, "");
        nuyen.Undo = nuyenUndo; Character.ExpenseEntries.Add(nuyen);
        startingEntries = Character.ExpenseEntries.Skip(existingEntries).ToArray();
    }

    // Call only after CharacterFileDialogs confirms the prepared payload was saved.
    // Transfer only accepted changes; live inventory/skills/undo retain their identity.
    internal void CommitToOwner()
    {
        selection.ApplyTo(owner);
        owner.Created = true;
        if (owner.MAGEnabled) owner.MAG.Value = Character.MAG.Value;
        if (owner.RESEnabled) owner.RES.Value = Character.RES.Value;
        foreach (var entry in startingEntries)
            if (!owner.ExpenseEntries.Contains(entry)) owner.ExpenseEntries.Add(entry);
        owner.FileName = Character.FileName;
    }
}
