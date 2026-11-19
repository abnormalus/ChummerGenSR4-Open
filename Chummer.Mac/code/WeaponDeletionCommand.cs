using System;
using System.Collections.Generic;
using Majorsilence.Forms;

namespace Chummer;

/// <summary>Removes weapons and nested equipment from the model and its inventory tree.</summary>
internal static class WeaponDeletionCommand
{
    // Creation and Career own their details and dirty-state callbacks. Career's
    // sale and expense-undo commands continue to own their accounting.
    internal static void Delete(Character character, CommonFunctions lookup, ImprovementManager improvements,
        TreeView tree, Action updateCharacter, Action refreshSelection, Action markDirty)
    {
        if (WeaponInventoryViews.IsAmmunition(tree.SelectedNode)) return;
        if (AccessoryGroupViews.IsHeader(tree.SelectedNode)) return;
        if (AccessoryGroupViews.IsHeader(tree.SelectedNode?.Parent))
        {
            var unit = lookup.FindWeaponAccessory(tree.SelectedNode.Tag.ToString(), character.Weapons);
            if (unit == null || !lookup.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteWeapon"))) return;
            foreach (var gear in unit.Gear) lookup.DeleteGear(gear, tree, improvements);
            unit.Parent.WeaponAccessories.Remove(unit);
            tree.SelectedNode.Remove();
            WeaponInventoryViews.Refresh(character, tree, false);
            updateCharacter();
            markDirty();
            return;
        }

        // Delete the selected Weapon.
        try
        {
            if (tree.SelectedNode.Level == 0)
            {
                if (tree.SelectedNode.Text == LanguageManager.Instance.GetString("Node_SelectedWeapons"))
                    return;

                if (!lookup.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteWeaponLocation")))
                    return;

                // Move all of the child nodes in the current parent to the Selected Weapons parent node.
                foreach (TreeNode objNode in tree.SelectedNode.Nodes)
                {
                    Weapon objWeapon = lookup.FindWeapon(objNode.Tag.ToString(), character.Weapons);

                    // Change the Location for the Weapon.
                    objWeapon.Location = "";
                }

                List<TreeNode> lstMoveNodes = new List<TreeNode>();
                foreach (TreeNode objNode in tree.SelectedNode.Nodes)
                    lstMoveNodes.Add(objNode);

                foreach (TreeNode objNode in lstMoveNodes)
                {
                    tree.SelectedNode.Nodes.Remove(objNode);
                    tree.Nodes[0].Nodes.Add(objNode);
                }

                // Remove the Weapon Location from the character, then remove the selected node.
                character.WeaponLocations.Remove(tree.SelectedNode.Text);
                tree.SelectedNode.Remove();
            }

            if (tree.SelectedNode.Level > 0)
            {
                if (!lookup.ConfirmDelete(LanguageManager.Instance.GetString("Message_DeleteWeapon")))
                    return;

                if (tree.SelectedNode.Level == 1)
                {
                    // Locate the Weapon that is selected in the tree.
                    Weapon objWeapon = lookup.FindWeapon(tree.SelectedNode.Tag.ToString(), character.Weapons);

                    // Cyberweapons cannot be removed through here and must be done by removing the piece of Cyberware.
                    if (objWeapon.Category.StartsWith("Cyberware"))
                    {
                        MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotRemoveCyberweapon"), LanguageManager.Instance.GetString("MessageTitle_CannotRemoveCyberweapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (objWeapon.Category == "Gear")
                    {
                        MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotRemoveGearWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotRemoveGearWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }
                    if (objWeapon.Category.StartsWith("Quality"))
                    {
                        MessageBox.Show(LanguageManager.Instance.GetString("Message_CannotRemoveQualityWeapon"), LanguageManager.Instance.GetString("MessageTitle_CannotRemoveQualityWeapon"), MessageBoxButtons.OK, MessageBoxIcon.Information);
                        return;
                    }

                    DeleteAccessoryGear(objWeapon, lookup, tree, improvements);
                    foreach (var underbarrel in objWeapon.UnderbarrelWeapons)
                        DeleteAccessoryGear(underbarrel, lookup, tree, improvements);

                    character.Weapons.Remove(objWeapon);
                    tree.SelectedNode.Remove();
                }
                else
                {
                    bool removedAccessory = false;
                    // Locate the selected Underbarrel Weapon if applicable.
                    foreach (Weapon objCharacterWeapon in character.Weapons)
                    {
                        if (objCharacterWeapon.UnderbarrelWeapons.Count > 0)
                        {
                            foreach (Weapon objUnderbarrelWeapon in objCharacterWeapon.UnderbarrelWeapons)
                            {
                                if (objUnderbarrelWeapon.InternalId == tree.SelectedNode.Tag.ToString())
                                {
                                    objCharacterWeapon.UnderbarrelWeapons.Remove(objUnderbarrelWeapon);
                                    tree.SelectedNode.Remove();
                                    return;
                                }
                            }
                        }
                    }

                    Weapon objWeapon = new Weapon(character);
                    // Locate the Weapon that is selected in the tree.
                    foreach (Weapon objCharacterWeapon in character.Weapons)
                    {
                        if (objCharacterWeapon.InternalId == tree.SelectedNode.Parent.Tag.ToString())
                        {
                            objWeapon = objCharacterWeapon;
                            break;
                        }
                        if (objCharacterWeapon.UnderbarrelWeapons.Count > 0)
                        {
                            foreach (Weapon objUnderbarrelWeapon in objCharacterWeapon.UnderbarrelWeapons)
                            {
                                if (objUnderbarrelWeapon.InternalId == tree.SelectedNode.Parent.Tag.ToString())
                                {
                                    objWeapon = objUnderbarrelWeapon;
                                    break;
                                }
                            }
                        }
                    }

                    // Locate the Accessory that is selected in the tree.
                    WeaponAccessory objAccessory = lookup.FindWeaponAccessory(tree.SelectedNode.Tag.ToString(), character.Weapons);
                    if (objAccessory != null)
                    {
                        foreach (Gear objGear in objAccessory.Gear)
                            lookup.DeleteGear(objGear, tree, improvements);
                        objWeapon.WeaponAccessories.Remove(objAccessory);
                        tree.SelectedNode.Remove();
                        removedAccessory = true;
                    }

                    if (!removedAccessory)
                    {
                        // Locate the Mod that is selected in the tree.
                        bool blnMod = false;
                        WeaponMod objMod = lookup.FindWeaponMod(tree.SelectedNode.Tag.ToString(), character.Weapons);
                        if (objMod != null)
                            blnMod = true;

                        if (blnMod)
                        {
                            objWeapon.WeaponMods.Remove(objMod);
                            tree.SelectedNode.Remove();
                        }
                        else
                        {
                            // Find the selected Gear.
                            Gear objGear = lookup.FindWeaponGear(tree.SelectedNode.Tag.ToString(), character.Weapons, out objAccessory);
                            lookup.DeleteGear(objGear, tree, improvements);
                            if (objGear.Parent == null)
                                objAccessory.Gear.Remove(objGear);
                            else
                                objGear.Parent.Children.Remove(objGear);
                            tree.SelectedNode.Remove();
                        }
                    }
                }
            }
            updateCharacter();
            refreshSelection();

            markDirty();
        }
        catch
        {
            // Retain the legacy response to stale tree entries and model failures.
        }
    }
    private static void DeleteAccessoryGear(Weapon weapon, CommonFunctions lookup, TreeView tree, ImprovementManager improvements)
    {
        foreach (var accessory in weapon.WeaponAccessories)
            foreach (var gear in accessory.Gear)
                lookup.DeleteGear(gear, tree, improvements);
    }

}
