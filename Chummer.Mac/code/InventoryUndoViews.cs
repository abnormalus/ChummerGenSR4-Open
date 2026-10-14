using System;
using System.Collections.Generic;
using System.Linq;
using Majorsilence.Forms;

namespace Chummer
{
    internal static class InventoryUndoViews
    {
        internal static Action Refresh(Character character, CommonFunctions functions, MainController controller,
            Action gear, Action cyberware, Dictionary<string, TreeView> trees, Dictionary<string, ContextMenuStrip> menus)
        {
            var selected = trees.ToDictionary(p => p.Key, p => p.Value.SelectedNode?.Tag?.ToString());
            IEnumerable<TreeNode> Walk(TreeNodeCollection nodes) { foreach (TreeNode node in nodes) { yield return node;
                foreach (var child in Walk(node.Nodes)) yield return child; } }
            var expanded = trees.ToDictionary(p => p.Key, p => Walk(p.Value.Nodes).Where(n => n.IsExpanded).Select(n => n.Tag?.ToString()).ToHashSet());
            ContextMenuStrip M(string name) => menus[name];
            TreeView T(string name) => trees[name];
            void Clear(TreeView tree, int roots = 1)
            {
                tree.SelectedNode = null;
                while (tree.Nodes.Count > roots) tree.Nodes.RemoveAt(tree.Nodes.Count - 1);
                foreach (TreeNode node in tree.Nodes) node.Nodes.Clear();
            }
            void Locations(TreeView tree, IEnumerable<string> locations, string menu)
            { foreach (string location in locations) tree.Nodes.Add(new TreeNode(location) { Tag = location, ContextMenuStrip = M(menu) }); }
            Clear(T("armor")); Clear(T("weapons")); Clear(T("cyberware"), 2); Clear(T("vehicles")); Clear(T("lifestyles"));
            Locations(T("armor"), character.ArmorBundles, "cmsArmorLocation");
            Locations(T("weapons"), character.WeaponLocations, "cmsWeaponLocation");
            foreach (var item in character.Armor) functions.CreateArmorTreeNode(item, T("armor"), M("cmsArmor"), M("cmsArmorMod"), M("cmsArmorGear"));
            foreach (var item in character.Weapons) functions.CreateWeaponTreeNode(item, T("weapons").Nodes[0], M("cmsWeapon"), M("cmsWeaponMod"), M("cmsWeaponAccessory"), M("cmsWeaponAccessoryGear"));
            cyberware(); gear();
            foreach (var item in character.Vehicles) functions.CreateVehicleTreeNode(item, T("vehicles"), M("cmsVehicle"), M("cmsVehicleLocation"), M("cmsVehicleWeapon"), M("cmsVehicleWeaponMod"), M("cmsVehicleWeaponAccessory"), M("cmsVehicleWeaponAccessoryGear"), M("cmsVehicleGear"));
            foreach (var item in character.Lifestyles) T("lifestyles").Nodes[0].Nodes.Add(new TreeNode(item.DisplayName) { Tag = item.InternalId,
                ContextMenuStrip = M(item.Comforts != "" ? "cmsAdvancedLifestyle" : "cmsLifestyleNotes"), ToolTipText = item.Notes });
            controller.PopulateFocusList(T("foci"));
            return () => {
                foreach (var pair in trees)
                {
                    foreach (var node in Walk(pair.Value.Nodes))
                    {
                        if (expanded[pair.Key].Contains(node.Tag?.ToString())) node.Expand(); else node.Collapse();
                    }
                    pair.Value.SelectedNode = Walk(pair.Value.Nodes).FirstOrDefault(n => selected[pair.Key] != null && n.Tag?.ToString() == selected[pair.Key])
                        ?? (pair.Value.Nodes.Count > 0 ? pair.Value.Nodes[0] : null);
                }
            };
        }
    }
}
