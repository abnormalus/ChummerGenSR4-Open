#nullable enable
using System;
using System.Linq;
using System.IO;
using System.Xml;
using System.Xml.Linq;

namespace Chummer;

/// <summary>
/// Accepted creation choices, separated from prompt layout and disk persistence.
/// Preparing a selection never changes the live character. Applying it retains
/// existing inventory and skill-group instances used by editor and undo state.
/// This is not a save transaction: callers still own backup and write ordering.
/// </summary>
internal sealed class CreationFinalizationSelection
{
    private readonly Character owner;
    private readonly int? carriedKarma;
    private readonly int startingNuyen;
    private readonly Lifestyle? fallbackLifestyle;
    private readonly SkillGroup[] brokenGroups;

    internal CreationFinalizationSelection(Character character, int remainingBuildPoints,
        int karmaCarryover, int acceptedStartingNuyen, Lifestyle? fallback)
    {
        owner = character;
        carriedKarma = character.BuildMethod == CharacterBuildMethod.Karma && remainingBuildPoints > karmaCarryover
            ? karmaCarryover : null;
        startingNuyen = Math.Max(0, acceptedStartingNuyen);
        fallbackLifestyle = fallback;
        brokenGroups = character.SkillGroups.Where(group => character.Skills.Any(skill =>
            skill.SkillGroup == group.Name && skill.Rating != group.Rating)).ToArray();
    }

    // Prepare the existing serializer's complete character state on an isolated
    // model. Never load a snapshot back into the editor's live object graph.
    // Career ledger/attribute conversion and disk commit remain caller-owned.
    internal Character PrepareCharacterCopy()
    {
        var prepared = new Character { FileName = owner.FileName };
        byte[] snapshot = owner.CaptureSnapshot();
        if (!prepared.LoadSnapshot(snapshot))
            throw new InvalidOperationException("Unable to prepare creation save copy.");
        using (var before = new MemoryStream(snapshot, false))
        using (var after = new MemoryStream(prepared.CaptureSnapshot(), false))
            if (!XNode.DeepEquals(XDocument.Load(before), XDocument.Load(after)))
                throw new InvalidOperationException("Creation save copy changed serialized character state.");
        Lifestyle? copiedFallback = null;
        if (fallbackLifestyle != null)
        {
            using var text = new StringWriter(System.Globalization.CultureInfo.InvariantCulture);
            using (var writer = new XmlTextWriter(text)) fallbackLifestyle.Save(writer);
            var document = new XmlDocument();
            document.LoadXml(text.ToString());
            copiedFallback = new Lifestyle(prepared);
            copiedFallback.Load(document.DocumentElement!);
        }
        var preparedGroups = prepared.SkillGroups;
        foreach (var group in brokenGroups)
        {
            var matches = preparedGroups.Where(item => item.Name == group.Name).ToArray();
            if (matches.Length != 1)
                throw new InvalidOperationException("Creation skill groups changed during preparation.");
            matches[0].Broken = true;
        }
        if (carriedKarma.HasValue) prepared.Karma = carriedKarma.Value;
        prepared.Nuyen = startingNuyen;
        if (copiedFallback != null && !prepared.Lifestyles.Any(item => item.InternalId == copiedFallback.InternalId))
            prepared.Lifestyles.Add(copiedFallback);
        return prepared;
    }

    internal void ApplyTo(Character character)
    {
        if (!ReferenceEquals(character, owner))
            throw new InvalidOperationException("Creation selection belongs to a different character.");
        if (carriedKarma.HasValue) character.Karma = carriedKarma.Value;
        if (fallbackLifestyle != null && !character.Lifestyles.Contains(fallbackLifestyle))
            character.Lifestyles.Add(fallbackLifestyle);
        character.Nuyen = startingNuyen;
        foreach (var group in brokenGroups) group.Broken = true;
    }
}
