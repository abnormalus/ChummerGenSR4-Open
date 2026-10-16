#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Runtime.CompilerServices;
using Majorsilence.Forms;

namespace Chummer;

// This adapter changes tree presentation only. Group headers have no item identity
// or item context menu; expand them to edit/delete an ordinary canonical unit.
internal static class AccessoryGroupViews
{
    internal sealed record Header(string FirstUnitId)
    {
        public override string ToString() => "accessory-group:" + FirstUnitId;
    }
    private sealed class State
    {
        internal string Signature = "";
        internal HashSet<string> UnitIds = new(StringComparer.Ordinal);
    }
    private static readonly ConditionalWeakTable<TreeNode, State> states = new();
    private static readonly HashSet<TreeView> refreshing = new();
    internal static bool IsHeader(TreeNode? node) => node?.Tag is Header;

    internal static IEnumerable<Weapon> Weapons(Character character, bool vehicle)
    {
        IEnumerable<Weapon> Descendants(IEnumerable<Weapon> roots)
        {
            foreach (var weapon in roots)
            {
                yield return weapon;
                foreach (var child in Descendants(weapon.UnderbarrelWeapons)) yield return child;
            }
        }
        return Descendants(vehicle ? character.Vehicles.SelectMany(item => item.Weapons.Concat(item.Mods.SelectMany(mod => mod.Weapons))) : character.Weapons);
    }

    internal static void Refresh(Character character, TreeView tree, bool vehicle)
    {
        if (!refreshing.Add(tree)) return;
        try
        {
            IEnumerable<TreeNode> Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    yield return node;
                    foreach (var child in Walk(node.Nodes)) yield return child;
                }
            }
            var nodes = Walk(tree.Nodes).ToArray();
            foreach (var weapon in Weapons(character, vehicle))
            {
                var parent = nodes.FirstOrDefault(node => node.Tag?.ToString() == weapon.InternalId);
                if (parent == null) continue;
                var groups = WeaponAccessoryBatch.Groups(weapon);
                string signature = string.Join(";", groups.Select(group => string.Join(",", group.Select(unit => unit.InternalId))));
                var state = states.GetOrCreateValue(parent);
                if (state.Signature == signature) continue;
                var selected = tree.SelectedNode;
                string? selectedHeader = (selected?.Tag as Header)?.FirstUnitId;
                var oldHeaders = parent.Nodes.Cast<TreeNode>().Where(IsHeader).ToArray();
                var expanded = oldHeaders.Where(node => node.IsExpanded).Select(node => ((Header)node.Tag!).FirstUnitId).ToHashSet();
                var unitNodes = parent.Nodes.Cast<TreeNode>().Where(node => !IsHeader(node))
                    .Concat(oldHeaders.SelectMany(node => node.Nodes.Cast<TreeNode>()))
                    .Where(node => node.Tag is string).GroupBy(node => node.Tag!.ToString()!, StringComparer.Ordinal)
                    .ToDictionary(group => group.Key, group => group.First(), StringComparer.Ordinal);
                var currentIds = weapon.WeaponAccessories.Select(unit => unit.InternalId).ToHashSet();
                foreach (var header in oldHeaders)
                {
                    foreach (TreeNode child in header.Nodes.Cast<TreeNode>().ToArray()) child.Remove();
                    header.Remove();
                }
                foreach (var id in state.UnitIds.Concat(currentIds).Distinct())
                    if (unitNodes.TryGetValue(id, out var node)) node.Remove();
                foreach (var group in groups)
                {
                    // Missing rows belong to their normal builder/purchase path. Do not
                    // invent menus or attach duplicate backend records to repair a view.
                    var members = group.Select(unit => unitNodes.GetValueOrDefault(unit.InternalId)).Where(node => node != null).Cast<TreeNode>().ToArray();
                    if (members.Length == 0) continue;
                    if (members.Length == 1) { parent.Nodes.Add(members[0]); continue; }
                    var header = new TreeNode(group[0].DisplayName + " ×" + members.Length) { Tag = new Header(group[0].InternalId) };
                    parent.Nodes.Add(header);
                    foreach (var member in members) header.Nodes.Add(member);
                    if (expanded.Contains(group[0].InternalId) || members.Contains(selected)) header.Expand();
                    if (selectedHeader == group[0].InternalId) selected = header;
                }
                state.Signature = currentIds.All(unitNodes.ContainsKey) ? signature : ""; state.UnitIds = currentIds;
                if (selected?.TreeView == tree) tree.SelectedNode = selected;
            }
        }
        finally { refreshing.Remove(tree); }
    }
}
