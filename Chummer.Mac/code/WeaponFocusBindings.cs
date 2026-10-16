#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Majorsilence.Forms;

namespace Chummer;

// Existing <focus>/<gearid> records link to the canonical modification GUID. No
// duplicate Gear, XML fields or Improvement entries are needed for weapon foci.
internal static class WeaponFocusBindings
{
    internal sealed record Reference(Weapon Weapon, WeaponMod Modification)
    {
        internal string Id => Modification.InternalId;
        internal int Force => Modification.Rating;
        internal string DisplayName => Modification.DisplayNameShort + " (" + Weapon.DisplayNameShort + ", " +
            LanguageManager.Instance.GetString("String_Force") + " " + Force + ")";
    }
    private sealed class State { internal readonly HashSet<string> KnownIds = new(StringComparer.Ordinal); }
    private static readonly ConditionalWeakTable<Character, State> states = new();

    private static IEnumerable<Weapon> Descendants(IEnumerable<Weapon> weapons)
    {
        foreach (var weapon in weapons)
        {
            yield return weapon;
            foreach (var child in Descendants(weapon.UnderbarrelWeapons)) yield return child;
        }
    }
    internal static Reference[] References(Character character)
    {
        var roots = character.Weapons.Concat(character.Vehicles.SelectMany(vehicle =>
            vehicle.Weapons.Concat(vehicle.Mods.SelectMany(mod => mod.Weapons))));
        return Descendants(roots).SelectMany(weapon => weapon.WeaponMods
            .Where(mod => mod.Name == "Weapon Focus").Select(mod => new Reference(weapon, mod)))
            .GroupBy(reference => reference.Id, StringComparer.Ordinal).Select(group => group.First()).ToArray();
    }
    internal static Reference? Find(Character character, string id) => References(character).FirstOrDefault(reference => reference.Id == id);

    internal static (int Count, int Force) TotalsIncluding(Character character, IEnumerable<string> checkedIds, string candidateId)
    {
        var functions = new CommonFunctions(character);
        var ids = checkedIds.Append(candidateId).Distinct(StringComparer.Ordinal).ToArray();
        int Force(string id) => Find(character, id)?.Force ?? functions.FindGear(id, character.Gear)?.Rating ??
            character.StackedFoci.FirstOrDefault(stack => stack.InternalId == id)?.TotalForce ?? 0;
        return (ids.Length, ids.Sum(Force));
    }

    internal static bool SetBound(Character character, string id, bool bound)
    {
        var reference = Find(character, id);
        if (reference == null) return false;
        states.GetOrCreateValue(character).KnownIds.Add(id);
        if (bound)
        {
            if (!character.Foci.Any(focus => focus.GearId == id))
                character.Foci.Add(new Focus { Name = "Weapon Focus", GearId = id, Rating = reference.Force });
        }
        else character.Foci.RemoveAll(focus => focus.GearId == id);
        AppDiagnostics.CaptureUiState();
        AppDiagnostics.Record(bound ? "focus.binding-applied" : "focus.binding-removed");
        return true;
    }

    // Remember only IDs observed in this session, so deleting an actual modification
    // clears its binding without guessing the meaning of unknown records in old saves.
    internal static void Synchronize(Character character, TreeView tree)
    {
        var references = References(character);
        var current = references.ToDictionary(reference => reference.Id, StringComparer.Ordinal);
        var state = states.GetOrCreateValue(character);
        character.Foci.RemoveAll(focus => state.KnownIds.Contains(focus.GearId) && !current.ContainsKey(focus.GearId));
        state.KnownIds.UnionWith(current.Keys);
        foreach (var focus in character.Foci)
            if (current.TryGetValue(focus.GearId, out var reference)) focus.Rating = reference.Force;
        foreach (var node in tree.Nodes.Cast<TreeNode>().ToArray())
            if (node.Tag is string id && state.KnownIds.Contains(id) && !current.ContainsKey(id)) node.Remove();
        foreach (var reference in references)
        {
            bool bound = character.Foci.Any(focus => focus.GearId == reference.Id);
            var node = tree.Nodes.Cast<TreeNode>().FirstOrDefault(item => item.Tag?.ToString() == reference.Id);
            if (node != null && node.Checked == bound)
            {
                if (node.Text != reference.DisplayName) node.Text = reference.DisplayName;
                continue;
            }
            node?.Remove();
            // Assign Checked before attaching, avoiding purchase handlers during refresh.
            tree.Nodes.Add(new TreeNode(reference.DisplayName) { Tag = reference.Id, Checked = bound });
        }
    }
}
