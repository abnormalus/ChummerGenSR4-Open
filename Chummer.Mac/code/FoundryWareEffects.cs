#nullable enable
using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Text.Json.Nodes;
using System.Xml;

namespace Chummer;

// Only ware with an established activation model gets an activation checkbox.
// Never make permanently installed ware switchable by guessing from a numeric bonus.
internal static class FoundryWareEffects
{
    private sealed record Profile(Cyberware Ware, Improvement[] Improvements, bool Active);
    private static string RuleName(Cyberware ware) => ware.Name.Split(" (", StringSplitOptions.None)[0];
    private static bool IsSwitchable(Cyberware ware) => RuleName(ware) is "Wired Reflexes" or "Move-by-Wire System";
    private static bool IsKnownPassive(Cyberware ware) => RuleName(ware) is "Muscle Replacement" or "Muscle Augmentation" or "Muscle Toner" or "Cerebral Booster" or "Reaction Enhancers" or "Synaptic Booster" or "Reflex Recorder";
    private static Improvement[] Improvements(Character character, Cyberware ware) => character.Improvements
        .Where(i => i.SourceName == ware.InternalId && i.ImproveSource == ware.SourceType).ToArray();

    // Enabled is a plain model flag. Measurements are synchronous, do not pump UI events,
    // perform I/O or notify observers, and always restore every flag before returning/throwing.
    internal static T Measure<T>(IEnumerable<Improvement> improvements, Action change, Func<T> read)
    {
        var saved = improvements.Select(i => (Item: i, i.Enabled)).ToArray();
        try { change(); return read(); }
        finally { foreach (var state in saved) state.Item.Enabled = state.Enabled; }
    }

    internal static void Apply(Character character, JsonObject system, Dictionary<Cyberware, JsonObject> items)
    {
        var profiles = items.Keys.Where(IsSwitchable).Select(ware => {
            var improvements = Improvements(character, ware);
            return new Profile(ware, improvements, improvements.Any(i => i.Enabled));
        }).Where(p => p.Improvements.Length > 0 && (p.Improvements.All(i => i.Enabled) || p.Improvements.All(i => !i.Enabled))).ToArray();
        var deltas = new Dictionary<Cyberware, Dictionary<string, int>>();
        var rejected = new HashSet<string>(StringComparer.Ordinal);
        if (profiles.Length is > 0 and <= 4)
        {
            var affected = profiles.SelectMany(p => p.Improvements).ToArray();
            var measured = Measure(affected, () => { }, () => {
                var combinations = new Dictionary<string, int>[1 << profiles.Length];
                for (int mask = 0; mask < combinations.Length; ++mask) {
                    for (int index = 0; index < profiles.Length; ++index)
                        foreach (var improvement in profiles[index].Improvements) improvement.Enabled = (mask & (1 << index)) != 0;
                    combinations[mask] = FoundryActorExporter.Capture(character);
                }
                return combinations;
            });
            var baseline = measured[0];
            for (int index = 0; index < profiles.Length; ++index)
                deltas[profiles[index].Ware] = baseline.ToDictionary(pair => pair.Key, pair => measured[1 << index][pair.Key] - pair.Value);
            // Check every on/off combination. A cap, maximum-only rule or other interaction
            // must not be approximated by additive effects that are wrong after a toggle.
            foreach (string field in baseline.Keys) {
                for (int mask = 0; mask < measured.Length; ++mask) {
                    int predicted = baseline[field];
                    for (int index = 0; index < profiles.Length; ++index)
                        if ((mask & (1 << index)) != 0) predicted += deltas[profiles[index].Ware][field];
                    if (predicted != measured[mask][field]) { rejected.Add(field); break; }
                }
                if (!rejected.Contains(field)) FoundryActorExporter.Set(system, field, baseline[field]);
            }
        }
        foreach (var (ware, item) in items)
        {
            var notices = new List<string>();
            var improvements = Improvements(character, ware);
            var changes = new JsonArray();
            if (deltas.TryGetValue(ware, out var fields)) {
                foreach (var (field, delta) in fields.Where(pair => pair.Value != 0)) {
                    if (rejected.Contains(field)) {
                        notices.Add("Activation cannot safely toggle " + Label(field) + " because bonuses interact or reach a cap. Its current augmented value is already included in the exported actor; adjust that value manually when changing this ware's activation.");
                        continue;
                    }
                    changes.Add(FoundryActorExporter.Obj(("key", "system." + field), ("type", "add"), ("value", delta.ToString(CultureInfo.InvariantCulture)), ("priority", 20)));
                }
                if (changes.Count > 0) {
                    var profile = profiles.Single(p => p.Ware == ware);
                    ((JsonArray)item["effects"]!).Add(FoundryActorExporter.Obj(("_id", Guid.NewGuid().ToString("N")[..16]),
                        ("name", "Activate " + ware.DisplayNameShort), ("img", "icons/svg/upgrade.svg"), ("transfer", true), ("disabled", !profile.Active), ("changes", changes)));
                }
            }
            else if (IsSwitchable(ware)) notices.Add("Activation was not automated (missing, mixed or interacting improvement data). Current augmented attributes, skills and initiative already include enabled bonuses. Adjust them manually when changing activation.");

            foreach (var improvement in improvements.Where(i => i.ImproveType == Improvement.ImprovementType.Skill)) {
                var skill = character.Skills.FirstOrDefault(s => s.Name == improvement.ImprovedName);
                string name = improvement.ImprovedName;
                if (skill == null) notices.Add(name + ": listed rule bonus " + improvement.Value + " has no matching exported skill record. Add/configure that skill manually; this bonus is not automated.");
                if (IsSwitchable(ware) && skill != null) {
                    int current = FoundryActorExporter.EffectiveSkillRating(skill, character);
                    int without = Measure(improvements, () => { foreach (var i in improvements) i.Enabled = false; }, () => FoundryActorExporter.EffectiveSkillRating(skill, character));
                    int included = current - without;
                    int activated = Measure(improvements, () => { foreach (var i in improvements) i.Enabled = true; }, () => FoundryActorExporter.EffectiveSkillRating(skill, character));
                    notices.Add(name + ": exported effective rating " + current + " already includes " + included + " from this ware. Foundry does not toggle this stored skill rating: set it to " + without + " when this ware is inactive and " + activated + " when active. Listed rule bonus: " + improvement.Value + "." +
                        (name == "Dodge" ? " Apply it only to Dodge, never Gymnastics; Foundry's general Dodge modifier is broader." : ""));
                }
            }
            if (ware.Bonus != null) {
                if (!IsSwitchable(ware) && !IsKnownPassive(ware) && ware.Bonus.ChildNodes.Cast<XmlNode>().Any(node => node.Name is "specificattribute" or "specificskill" or "initiativepass" or "initiativepassadd"))
                    notices.Add("This ware has no verified activation profile. Its enabled numeric bonuses are included in the exported actor snapshot; activation/deactivation and conditional behavior require manual adjustment.");
                foreach (XmlNode bonus in ware.Bonus.ChildNodes) {
                    if (bonus.NodeType != XmlNodeType.Element || bonus.Name is "specificattribute" or "specificskill" or "initiativepass" or "initiativepassadd") continue;
                    string resolved = string.Join(", ", improvements.Where(i => i.ImproveType.ToString().Equals(bonus.Name, StringComparison.OrdinalIgnoreCase)).Select(i => i.Value.ToString(CultureInfo.InvariantCulture)).Distinct());
                    notices.Add(bonus.Name + (resolved.Length == 0 ? "" : " (" + resolved + ")") + ": this rule is not automated in Foundry. Apply its non-stat behavior manually. Current attribute/skill/initiative bonuses are already represented in the actor or activation effect; do not add those again.");
                }
            }
            if (ware.Category == "Cyberlimb") notices.Add("Cyberlimb attribute averaging is included in exported character totals. Limb-specific tests, capacity and changes to limb components require manual recalculation.");
            if (notices.Count > 0) {
                var data = (JsonObject)item["system"]!;
                data["notes"] = FoundryActorExporter.AddNotes(data["notes"]?.GetValue<string>() ?? "", notices);
            }
        }
    }
    private static string Label(string path) => path.Replace("sheetStats.", "", StringComparison.Ordinal)
        .Replace("modifiers.initiative.passes.", "initiative passes: ", StringComparison.Ordinal)
        .Replace("modifiers.initiative.bonuses.", "initiative bonus: ", StringComparison.Ordinal);
}
