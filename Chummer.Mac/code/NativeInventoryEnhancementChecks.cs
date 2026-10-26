#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeInventoryEnhancementChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static void Invoke(object owner, string name) => owner.GetType()
        .GetMethod(name, BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(owner, new object[] { owner, EventArgs.Empty });
    private static async Task ChooseAsync(Action command, Forms.DialogResult answer)
    {
        var answered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(15);
                while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                {
                    if (DateTime.UtcNow >= deadline) throw new TimeoutException("Accessory confirmation did not open.");
                    await Task.Delay(20);
                }
                var dialog = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last();
                dialog.DialogResult = answer; dialog.Close(); answered.TrySetResult();
            }
            catch (Exception error) { answered.TrySetException(error); }
        });
        command(); await answered.Task;
    }
    internal static async Task RunAsync(Forms.Form main, Character originalCharacter, Action<bool, string> check)
    {
        byte[] original = originalCharacter.CaptureSnapshot();
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original);
            character.Created = career; character.Nuyen = 10000;
            if (!character.Options.Books.Contains("AR")) character.Options.Books.Add("AR");
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            editor.MdiParent = main; editor.Show();
            string mode = career ? "career" : "creation";
            try
            {
                var gearTree = Field<Forms.TreeView>(editor, "treGear");
                var weaponTree = Field<Forms.TreeView>(editor, "treWeapons");
                var topTabs = Field<Forms.TabControl>(editor, "tabCharacterTabs");
                var gearTabs = Field<Forms.TabControl>(editor, "tabStreetGearTabs");
                topTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
                gearTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabGear");
                var link = new Commlink(character); var linkNode = new Forms.TreeNode();
                XmlNode linkDefinition = XmlManager.Instance.Load("gear.xml").SelectNodes("/chummer/gears/gear")!.Cast<XmlNode>()
                    .First(node => node["category"]?.InnerText == "Commlink");
                link.Create(linkDefinition, character, linkNode, 0); character.Gear.Add(link); gearTree.Nodes[0].Nodes.Add(linkNode);
                gearTree.SelectedNode = linkNode;
                bool operatingSystemsSelected = false;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = (GearPickerWindow)NativeDialogHost.ActiveWindow!;
                    operatingSystemsSelected = picker.Model.Category?.Code == "Commlink Operating System";
                    picker.Close();
                });
                Invoke(editor, "tsGearAddAsPlugin_Click");
                check(operatingSystemsSelected && link.Children.Count == 0, "Native " + mode + " commlink add-on defaults to operating systems and cancels cleanly");

                XmlDocument catalog = XmlManager.Instance.Load("weapons.xml");
                Weapon AddWeapon(string name)
                {
                    var weapon = new Weapon(character); var node = new Forms.TreeNode();
                    var definition = catalog.SelectNodes("/chummer/weapons/weapon")!.Cast<XmlNode>().First(item => item["name"]?.InnerText == name);
                    weapon.Create(definition, character, node, null, null, null);
                    character.Weapons.Add(weapon); weaponTree.Nodes[0].Nodes.Add(node); weaponTree.SelectedNode = node;
                    return weapon;
                }
                gearTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabWeapons");
                var axe = AddWeapon("Combat Axe");
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = (AccessoryPickerWindow)NativeDialogHost.ActiveWindow!;
                    picker.SearchBox.Text = "Weapon Commlink";
                    picker.Model.SelectedEntry = picker.Model.Items.Single(item => item.Name == "Weapon Commlink");
                    picker.Accept(false);
                });
                Invoke(editor, "tsWeaponAddAccessory_Click");
                check(axe.WeaponAccessories.Any(item => item.Name == "Weapon Commlink"), "Native " + mode + " melee accessory purchase reaches the existing inventory path");

                void AddGrip()
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var picker = (VehicleModPickerWindow)NativeDialogHost.ActiveWindow!;
                        picker.SearchBox.Text = "Personalized Grip";
                        picker.Accept(false);
                    });
                    Invoke(editor, "tsWeaponAddModification_Click");
                }
                AddGrip();
                check(axe.WeaponMods.Any(item => item.Name == WeaponModificationNames.MeleePersonalizedGrip && item.DicePool == 1),
                    "Native " + mode + " unified grip purchase applies the melee definition");
                var rifle = AddWeapon("AK-97");
                AddGrip();
                check(rifle.WeaponMods.Any(item => item.Name == WeaponModificationNames.PersonalizedGrip && item.RC == "1" && item.DicePool == 0),
                    "Native " + mode + " unified grip purchase applies the ranged definition");
                void BuyClips(string handler)
                {
                    Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                    {
                        var picker = (AccessoryPickerWindow)NativeDialogHost.ActiveWindow!;
                        picker.SearchBox.Text = "Spare Clips";
                        picker.Model.SelectedEntry = picker.Model.Items.Single(item => item.Name == "Spare Clips");
                        picker.Model.Quantity = 3; picker.Accept(false);
                    });
                    Invoke(editor, handler);
                }
                int clipMoney = character.Nuyen, clipExpenses = character.ExpenseEntries.Count;
                BuyClips("tsWeaponAddAccessory_Click");
                var rifleNode = weaponTree.Nodes[0].Nodes.Cast<Forms.TreeNode>().Single(node => node.Tag?.ToString() == rifle.InternalId);
                var clipGroup = rifleNode.Nodes.Cast<Forms.TreeNode>().Single(AccessoryGroupViews.IsHeader);
                check(rifle.WeaponAccessories.Count(item => item.Name == "Spare Clips") == 3 && clipGroup.Nodes.Count == 3 && clipGroup.Text.Contains("×3") &&
                    (!career || character.Nuyen == clipMoney - 15 && character.ExpenseEntries.Count == clipExpenses + 3),
                    "Native " + mode + " spare clips buy as a grouped quantity with ordinary units and per-unit career expenses");
                weaponTree.SelectedNode = clipGroup;
                Invoke(editor, "cmdDeleteWeapon_Click");
                check(rifle.WeaponAccessories.Count(item => item.Name == "Spare Clips") == 3,
                    "Native " + mode + " accessory group header cannot delete its weapon or an arbitrary unit");
                var clipUnit = rifle.WeaponAccessories.First(item => item.Name == "Spare Clips");
                clipUnit.Notes = "synthetic customized clip";
                AccessoryGroupViews.Refresh(character, weaponTree, false);
                check(rifleNode.Nodes.Cast<Forms.TreeNode>().Single(AccessoryGroupViews.IsHeader).Nodes.Count == 2 &&
                    rifleNode.Nodes.Cast<Forms.TreeNode>().Any(node => node.Tag?.ToString() == clipUnit.InternalId),
                    "Native " + mode + " customized accessory unit separates from its equivalent group");
                clipUnit.Notes = ""; AccessoryGroupViews.Refresh(character, weaponTree, false);
                clipGroup = rifleNode.Nodes.Cast<Forms.TreeNode>().Single(AccessoryGroupViews.IsHeader);
                bool confirmDelete = character.Options.ConfirmDelete;
                try
                {
                    character.Options.ConfirmDelete = false;
                    weaponTree.SelectedNode = clipGroup.Nodes[0]; Invoke(editor, "cmdDeleteWeapon_Click");
                }
                finally { character.Options.ConfirmDelete = confirmDelete; }
                check(rifle.WeaponAccessories.Count(item => item.Name == "Spare Clips") == 2 &&
                    rifleNode.Nodes.Cast<Forms.TreeNode>().Single(AccessoryGroupViews.IsHeader).Nodes.Count == 2,
                    "Native " + mode + " deleting an expanded accessory unit removes exactly its canonical record");
                weaponTree.SelectedNode = rifleNode;
                if (career)
                {
                    var expense = character.ExpenseEntries.Last(item => item.Undo?.NuyenType == NuyenExpenseType.AddWeaponAccessory &&
                        rifle.WeaponAccessories.Any(unit => unit.InternalId == item.Undo.ObjectId));
                    ((frmCareer)editor).PopulateExpenseList();
                    var row = Field<Forms.ListView>(editor, "lstNuyen").Items.Cast<Forms.ListViewItem>().Single(item => item.SubItems[3].Text == expense.InternalId);
                    row.Selected = true;
                    int moneyBeforeUndo = character.Nuyen;
                    await ChooseAsync(() => Invoke(editor, "tsUndoNuyenExpense_Click"), Forms.DialogResult.Cancel);
                    check(character.Nuyen == moneyBeforeUndo && character.ExpenseEntries.Contains(expense),
                        "Native closing accessory expense undo confirmation changes neither money nor units");
                    await ChooseAsync(() => Invoke(editor, "tsUndoNuyenExpense_Click"), Forms.DialogResult.Yes);
                    AccessoryGroupViews.Refresh(character, weaponTree, false);
                    check(character.Nuyen == moneyBeforeUndo + 5 && !character.ExpenseEntries.Contains(expense) &&
                        rifle.WeaponAccessories.Count(unit => unit.Name == "Spare Clips") == 1 && !rifleNode.Nodes.Cast<Forms.TreeNode>().Any(AccessoryGroupViews.IsHeader),
                        "Native accessory expense undo refunds one canonical grouped unit and restores a single row");
                    character.Nuyen = 4; weaponTree.SelectedNode = rifleNode;
                    int unitsBeforeRejection = rifle.WeaponAccessories.Count, expensesBeforeRejection = character.ExpenseEntries.Count;
                    await ChooseAsync(() => BuyClips("tsWeaponAddAccessory_Click"), Forms.DialogResult.OK);
                    check(character.Nuyen == 4 && rifle.WeaponAccessories.Count == unitsBeforeRejection && character.ExpenseEntries.Count == expensesBeforeRejection,
                        "Native unaffordable accessory batch creates no partial units or expenses");
                    character.Nuyen = moneyBeforeUndo + 5;
                }
                weaponTree.SelectedNode = rifleNode;
                int unitsBeforeCancel = rifle.WeaponAccessories.Count, moneyBeforeCancel = character.Nuyen;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = (AccessoryPickerWindow)NativeDialogHost.ActiveWindow!;
                    picker.SearchBox.Text = "Spare Clips"; picker.Model.Quantity = 3; picker.Close();
                });
                Invoke(editor, "tsWeaponAddAccessory_Click");
                check(rifle.WeaponAccessories.Count == unitsBeforeCancel && character.Nuyen == moneyBeforeCancel,
                    "Native " + mode + " cancelling a configured quantity does not buy any units");
                var menu = Field<Forms.ContextMenuStrip>(editor, "cmsWeapon");
                var command = menu.Items.OfType<Forms.ToolStripMenuItem>().Single(item => item.Name == "buyWeaponAmmunition");
                menu.Show(weaponTree, new System.Drawing.Point(8, 8)); menu.Hide();
                if (!command.Visible) throw new InvalidOperationException("Ranged weapon ammunition command is unavailable.");
                gearTree.SelectedNode = linkNode;
                int beforeCount = character.Gear.Count, beforeMoney = character.Nuyen;
                bool literalSearchRecovered = false;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = (GearPickerWindow)NativeDialogHost.ActiveWindow!;
                    picker.SearchBox.Text = "missing \"quoted\" ammunition";
                    literalSearchRecovered = picker.Model.Items.Count == 0 &&
                        !picker.AcceptButton.IsEnabled && picker.Model.Details == null;
                    picker.SearchBox.Text = "";
                    picker.ItemList.SelectedItem = picker.Model.Items.Single(item => item.Name == "Ammo: Regular Ammo");
                    picker.Model.Quantity = 10;
                    picker.Accept(false);
                });
                command.PerformClick();
                check(literalSearchRecovered, "Native " + mode + " quoted gear search clears stale purchase state and recovers");
                var ammo = character.Gear.Last(item => item.Name == "Ammo: Regular Ammo");
                check(character.Gear.Count == beforeCount + 1 && ammo.Extra == rifle.AmmoCategory && ammo.Quantity == 10 &&
                    ammo.Parent == null && link.Children.Count == 0 && gearTree.SelectedNode == linkNode &&
                    (!career || character.Nuyen == beforeMoney - ammo.TotalCost),
                    "Native " + mode + " right-click ammo uses the weapon class, root inventory and existing purchase cost");
                beforeCount = character.Gear.Count; beforeMoney = character.Nuyen;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => NativeDialogHost.ActiveWindow!.Close());
                command.PerformClick();
                weaponTree.SelectedNode = weaponTree.Nodes[0].Nodes.Cast<Forms.TreeNode>().Single(node => node.Tag?.ToString() == axe.InternalId);
                menu.Show(weaponTree, new System.Drawing.Point(8, 8)); menu.Hide();
                check(character.Gear.Count == beforeCount && character.Nuyen == beforeMoney && !command.Visible,
                    "Native " + mode + " cancelled ammo purchase changes nothing and melee weapons hide the command");

                // Use a decorated label to reproduce the vehicle path's former lookup
                // bug. The purchase must use the weapon's canonical name/category.
                var vehicleTree = Field<Forms.TreeView>(editor, "treVehicles");
                var vehicle = new Vehicle(character); var vehicleNode = new Forms.TreeNode();
                vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!,
                    vehicleNode, null, null, null, null, null, false);
                character.Vehicles.Add(vehicle); vehicleTree.Nodes[0].Nodes.Add(vehicleNode);
                var mounted = new Weapon(character); var mountedNode = new Forms.TreeNode();
                mounted.Create(catalog.SelectSingleNode("/chummer/weapons/weapon[name='AK-97']")!, character, mountedNode, null, null, null);
                mounted.VehicleMounted = true; mounted.WeaponName = "Custom vehicle rifle";
                mountedNode.Text = mounted.DisplayName;
                vehicle.Weapons.Add(mounted); vehicleNode.Nodes.Add(mountedNode);
                topTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabVehicles");
                vehicleTree.SelectedNode = mountedNode;
                int vehicleMoney = character.Nuyen, weaponCost = mounted.TotalCost;
                Avalonia.Threading.Dispatcher.UIThread.Post(() =>
                {
                    var picker = (AccessoryPickerWindow)NativeDialogHost.ActiveWindow!;
                    picker.SearchBox.Text = "Weapon Commlink";
                    picker.Model.SelectedEntry = picker.Model.Items.Single(item => item.Name == "Weapon Commlink");
                    picker.Accept(false);
                });
                Invoke(editor, "tsVehicleAddWeaponAccessory_Click");
                check(mounted.DisplayName != mounted.Name && mounted.WeaponAccessories.Any(item => item.Name == "Weapon Commlink") &&
                    character.Nuyen == vehicleMoney - (career ? mounted.TotalCost - weaponCost : 0),
                    "Native " + mode + " decorated vehicle weapon buys an accessory with its existing cost rules");
                vehicleTree.SelectedNode = mountedNode;
                int mountedMoney = character.Nuyen;
                BuyClips("tsVehicleAddWeaponAccessory_Click");
                AccessoryGroupViews.Refresh(character, vehicleTree, true);
                check(mounted.WeaponAccessories.Count(item => item.Name == "Spare Clips") == 3 &&
                    mountedNode.Nodes.Cast<Forms.TreeNode>().Single(AccessoryGroupViews.IsHeader).Nodes.Count == 3 &&
                    (!career || character.Nuyen == mountedMoney - 15),
                    "Native " + mode + " vehicle accessory quantity uses ordinary records and grouped views");
                if (career)
                {
                    var expense = character.ExpenseEntries.Last(item => item.Undo?.NuyenType == NuyenExpenseType.AddVehicleWeaponAccessory);
                    ((frmCareer)editor).PopulateExpenseList();
                    Field<Forms.ListView>(editor, "lstNuyen").Items.Cast<Forms.ListViewItem>().Single(item => item.SubItems[3].Text == expense.InternalId).Selected = true;
                    await ChooseAsync(() => Invoke(editor, "tsUndoNuyenExpense_Click"), Forms.DialogResult.Yes);
                    AccessoryGroupViews.Refresh(character, vehicleTree, true);
                    check(mounted.WeaponAccessories.Count(unit => unit.Name == "Spare Clips") == 2 && character.Nuyen == mountedMoney - 10 &&
                        mountedNode.Nodes.Cast<Forms.TreeNode>().Single(AccessoryGroupViews.IsHeader).Nodes.Count == 2,
                        "Native mounted accessory expense undo finds its canonical unit and updates the group");
                }
                int accessoryCount = mounted.WeaponAccessories.Count;
                vehicleMoney = character.Nuyen; vehicleTree.SelectedNode = mountedNode;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => NativeDialogHost.ActiveWindow!.Close());
                Invoke(editor, "tsVehicleAddWeaponAccessory_Click");
                check(mounted.WeaponAccessories.Count == accessoryCount && character.Nuyen == vehicleMoney,
                    "Native " + mode + " cancelled vehicle accessory purchase changes neither inventory nor funds");
                var mountedGroup = mountedNode.Nodes.Cast<Forms.TreeNode>().Single(AccessoryGroupViews.IsHeader);
                bool previousDelete = character.Options.ConfirmDelete;
                try
                {
                    character.Options.ConfirmDelete = false;
                    vehicleTree.SelectedNode = mountedGroup.Nodes[0]; Invoke(editor, "cmdDeleteVehicle_Click");
                }
                finally { character.Options.ConfirmDelete = previousDelete; }
                check(mounted.WeaponAccessories.Count == accessoryCount - 1 && character.Vehicles.Contains(vehicle),
                    "Native " + mode + " deleting a grouped vehicle accessory removes only the canonical unit");
            }
            finally
            {
                editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
                editor.Close();
            }
            await Task.Delay(20);
        }
        check(originalCharacter.CaptureSnapshot().SequenceEqual(original), "Native inventory enhancements leave the source character unchanged");
    }
}
