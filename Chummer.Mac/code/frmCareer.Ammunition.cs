#nullable enable
using Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private void InitializeWeaponAmmunitionMenus()
    {
        Weapon? Selected(TreeView tree, bool vehicle)
        {
            string? id = tree.SelectedNode?.Tag?.ToString();
            if (string.IsNullOrEmpty(id)) return null;
            return vehicle ? _objFunctions.FindVehicleWeapon(id, _objCharacter.Vehicles, out _)
                : _objFunctions.FindWeapon(id, _objCharacter.Weapons);
        }
        WeaponAmmunitionMenu.Attach(cmsWeapon, () => Selected(treWeapons, false), BuyAmmunitionForWeapon);
        WeaponAmmunitionMenu.Attach(cmsVehicleWeapon, () => Selected(treVehicles, true), BuyAmmunitionForWeapon);
    }

    private void BuyAmmunitionForWeapon(Weapon weapon)
    {
        if (!AmmunitionSelectionPolicy.CanBuyFor(weapon)) return;
        var previousGearSelection = treGear.SelectedNode;
        try
        {
            // Ammo is inventory, not a plug-in of whichever gear happened to be selected.
            bool addAgain;
            do
            {
                treGear.SelectedNode = treGear.Nodes[0];
                addAgain = PickGear(true, null, weapon.AmmoCategory);
            } while (addAgain && !IsDisposed);
        }
        finally
        {
            if (!IsDisposed && previousGearSelection?.TreeView == treGear)
                treGear.SelectedNode = previousGearSelection;
        }
    }
}
