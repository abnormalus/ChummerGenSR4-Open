using System;
using System.Collections.Generic;
using Majorsilence.Forms;
namespace Chummer
{
    public partial class frmCareer
    {
        private void InitializeInventoryUndo()
        {
            if (_inventoryUndo != null) return;
            _inventoryUndo = new InventoryUndo(_objCharacter, this, RefreshAfterInventoryUndo);
            _inventoryUndo.Attach(mnuCreateEdit, cmsAdvancedLifestyle, cmsArmor, cmsArmorGear, cmsArmorLocation, cmsArmorMod, cmsBioware, cmsCyberware, cmsCyberwareGear, cmsGear, cmsGearButton, cmsGearLocation, cmsGearPlugin, cmsLifestyle, cmsLifestyleNotes, cmsVehicle, cmsVehicleGear, cmsVehicleLocation, cmsVehicleWeapon, cmsVehicleWeaponAccessory, cmsVehicleWeaponAccessoryGear, cmsVehicleWeaponMod, cmsWeapon, cmsWeaponAccessory, cmsWeaponAccessoryGear, cmsWeaponLocation, cmsWeaponMod);
            // Root/background context menus keep Undo accessible after deleting the last item.
            foreach (var tree in new[] { treGear, treArmor, treWeapons, treCyberware, treVehicles, treLifestyles, treFoci })
            {
                if (tree.ContextMenuStrip == null) tree.ContextMenuStrip = new ContextMenuStrip();
                _inventoryUndo.Attach(null, tree.ContextMenuStrip);
            }
        }
        private void RefreshAfterInventoryUndo()
        {
            bool loading = _blnLoading, skip = _blnSkipRefresh;
            Action select;
            _blnLoading = _blnSkipRefresh = true;
            try
            {
                select = InventoryUndoViews.Refresh(_objCharacter, _objFunctions, _objController,
                    PopulateGearList, PopulateCyberware,
                    new Dictionary<string, TreeView> { { "gear", treGear }, { "armor", treArmor }, { "weapons", treWeapons }, { "cyberware", treCyberware }, { "vehicles", treVehicles }, { "lifestyles", treLifestyles }, { "foci", treFoci } },
                    new Dictionary<string, ContextMenuStrip> { { "cmsAdvancedLifestyle", cmsAdvancedLifestyle }, { "cmsArmor", cmsArmor }, { "cmsArmorGear", cmsArmorGear }, { "cmsArmorLocation", cmsArmorLocation }, { "cmsArmorMod", cmsArmorMod }, { "cmsBioware", cmsBioware }, { "cmsCyberware", cmsCyberware }, { "cmsCyberwareGear", cmsCyberwareGear }, { "cmsGear", cmsGear }, { "cmsGearButton", cmsGearButton }, { "cmsGearLocation", cmsGearLocation }, { "cmsGearPlugin", cmsGearPlugin }, { "cmsLifestyle", cmsLifestyle }, { "cmsLifestyleNotes", cmsLifestyleNotes }, { "cmsVehicle", cmsVehicle }, { "cmsVehicleGear", cmsVehicleGear }, { "cmsVehicleLocation", cmsVehicleLocation }, { "cmsVehicleWeapon", cmsVehicleWeapon }, { "cmsVehicleWeaponAccessory", cmsVehicleWeaponAccessory }, { "cmsVehicleWeaponAccessoryGear", cmsVehicleWeaponAccessoryGear }, { "cmsVehicleWeaponMod", cmsVehicleWeaponMod }, { "cmsWeapon", cmsWeapon }, { "cmsWeaponAccessory", cmsWeaponAccessory }, { "cmsWeaponAccessoryGear", cmsWeaponAccessoryGear }, { "cmsWeaponLocation", cmsWeaponLocation }, { "cmsWeaponMod", cmsWeaponMod } });
            }
            finally { _blnLoading = loading; _blnSkipRefresh = skip; }
            select();
            RefreshSelectedGear();
            if (treArmor.SelectedNode != null) RefreshSelectedArmor();
            RefreshSelectedWeapon(); RefreshSelectedCyberware();
            PopulateExpenseList();

            UpdateCharacterInfo();
            _blnIsDirty = true;
            UpdateWindowTitle();
        }
    }
}
