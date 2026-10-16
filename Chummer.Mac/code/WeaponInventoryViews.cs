#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Majorsilence.Forms;

namespace Chummer;

// Compose read-only inventory projections behind one reentry boundary. Tree rows never
// become weapon-owned Gear and cannot be passed to the legacy depth-based edit handlers.
internal static class WeaponInventoryViews
{
    internal sealed record AmmunitionRow(string WeaponId, string GearId);
    private static readonly HashSet<TreeView> refreshing = new();
    internal static bool IsAmmunition(TreeNode? node) => node?.Tag is AmmunitionRow;
    internal static string AmmunitionText(Gear ammo) => ammo.DisplayName +
        (ammo.Parent == null ? "" : " (" + ammo.Parent.DisplayName + ")");

    internal static void Refresh(Character character, TreeView tree, bool mounted)
    {
        if (!refreshing.Add(tree)) return;
        try
        {
            AccessoryGroupViews.Refresh(character, tree, mounted);
            var stock = new Dictionary<string, List<Gear>>(StringComparer.Ordinal);
            void AddWeapons(IEnumerable<Weapon> weapons, Vehicle? vehicle)
            {
                foreach (var weapon in weapons)
                {
                    stock[weapon.InternalId] = weapon.RequireAmmo && weapon.WeaponType == "Ranged"
                        ? vehicle == null ? WeaponAmmunition.Carried(character, weapon) : WeaponAmmunition.Mounted(vehicle, weapon)
                        : new List<Gear>();
                    AddWeapons(weapon.UnderbarrelWeapons, vehicle);
                }
            }
            if (mounted)
                foreach (var vehicle in character.Vehicles) AddWeapons(vehicle.Weapons.Concat(vehicle.Mods.SelectMany(mod => mod.Weapons)), vehicle);
            else AddWeapons(character.Weapons, null);

            IEnumerable<TreeNode> Walk(TreeNodeCollection nodes)
            {
                foreach (TreeNode node in nodes)
                {
                    yield return node;
                    foreach (var child in Walk(node.Nodes)) yield return child;
                }
            }
            foreach (var parent in Walk(tree.Nodes).ToArray())
            {
                if (parent.Tag is not string weaponId || !stock.TryGetValue(weaponId, out var ammunition)) continue;
                var rows = parent.Nodes.Cast<TreeNode>().Where(IsAmmunition).ToArray();
                var eligible = ammunition.Select(ammo => ammo.InternalId).ToHashSet(StringComparer.Ordinal);
                foreach (var row in rows)
                    if (!eligible.Contains(((AmmunitionRow)row.Tag!).GearId))
                    {
                        // Do not promote selection to the parent weapon when stock disappears.
                        if (tree.SelectedNode == row) tree.SelectedNode = null;
                        row.Remove();
                    }
                foreach (var ammo in ammunition)
                {
                    var row = rows.FirstOrDefault(item => ((AmmunitionRow)item.Tag!).GearId == ammo.InternalId && item.TreeView == tree);
                    if (row == null)
                    {
                        row = new TreeNode { Tag = new AmmunitionRow(weaponId, ammo.InternalId) };
                        parent.Nodes.Add(row);
                    }
                    string text = AmmunitionText(ammo);
                    if (row.Text != text) row.Text = text;
                    // No inventory context menu: edit the canonical stock from Gear.
                    row.ContextMenuStrip = null;
                }
            }
        }
        finally { refreshing.Remove(tree); }
    }
}
