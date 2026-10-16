#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

internal static class NativeAmmunitionViewChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static void Invoke(Form owner, string name)
    {
        bool completed = false, unexpectedModal = false;
        // A queued command must return synchronously for a read-only row. Unwind an accidental
        // nested modal instead of hanging the whole suite; fail with a fixed structural message.
        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
            if (completed) return;
            unexpectedModal = true;
            NativeDialogHost.CancelAll();
            foreach (var modal in Application.OpenForms.OfType<Form>().Where(form => form.Modal).ToArray()) modal.Close();
        });
        try { owner.GetType().GetMethod(name, BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(owner, new object[] { owner, EventArgs.Empty }); }
        finally { completed = true; }
        if (unexpectedModal) throw new InvalidOperationException("Read-only ammunition command opened a dialog.");
    }
    private static TreeNode[] Rows(TreeView tree)
    {
        System.Collections.Generic.IEnumerable<TreeNode> Walk(TreeNodeCollection nodes)
        { foreach (TreeNode node in nodes) { yield return node; foreach (var child in Walk(node.Nodes)) yield return child; } }
        return Walk(tree.Nodes).Where(WeaponInventoryViews.IsAmmunition).ToArray();
    }
    internal static async Task RunAsync(Form main, Character source, string output, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career;
            character.Gear.Clear(); character.Weapons.Clear(); character.Vehicles.Clear();
            var catalog = XmlManager.Instance.Load("weapons.xml");
            Weapon Rifle()
            {
                var weapon = new Weapon(character);
                weapon.Create(catalog.SelectSingleNode("/chummer/weapons/weapon[name='AK-97']")!, character, new TreeNode(), null, null, null);
                return weapon;
            }
            var first = Rifle(); var second = Rifle(); var underbarrel = Rifle();
            first.UnderbarrelWeapons.Add(underbarrel); character.Weapons.Add(first); character.Weapons.Add(second);
            var reserve = new Gear(character) { Name = "Ammo: Regular", Category = "Ammunition", Extra = "Assault Rifles", Quantity = 40, Cost = "2" };
            character.Gear.Add(reserve);
            var vehicle = new Vehicle(character);
            vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!, new TreeNode(), null, null, null, null, null, false);
            var mounted = Rifle(); mounted.VehicleMounted = true; vehicle.Weapons.Add(mounted); character.Vehicles.Add(vehicle);
            var vehicleReserve = new Gear(character) { Name = "Ammo: APDS", Category = "Ammunition", Extra = "Assault Rifles", Quantity = 12, Cost = "7" };
            vehicle.Gear.Add(vehicleReserve);
            using Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
            editor.MdiParent = main; editor.Show();
            string mode = career ? "career" : "creation";
            try
            {
                var tree = Field<TreeView>(editor, "treWeapons"); var vehicleTree = Field<TreeView>(editor, "treVehicles");
                WeaponInventoryViews.Refresh(character, tree, false); WeaponInventoryViews.Refresh(character, vehicleTree, true);
                check(Rows(tree).Length == 3 && Rows(tree).All(row => ((WeaponInventoryViews.AmmunitionRow)row.Tag!).GearId == reserve.InternalId) &&
                    Rows(vehicleTree).Length == 1 && ((WeaponInventoryViews.AmmunitionRow)Rows(vehicleTree)[0].Tag!).GearId == vehicleReserve.InternalId,
                    "Native " + mode + " carried underbarrel and mounted views share canonical stock in their original pools");
                byte[] original = character.CaptureSnapshot();
                Field<TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<TabPage>(editor, "tabStreetGear");
                Field<TabControl>(editor, "tabStreetGearTabs").SelectedTab = Field<TabPage>(editor, "tabWeapons");
                tree.Nodes[0].Expand(); tree.Nodes[0].Nodes[0].Expand();
                tree.SelectedNode = Rows(tree)[0];
                check(Field<Label>(editor, "lblWeaponName").Text == "" && !Field<Button>(editor, "cmdDeleteWeapon").Enabled &&
                    !Field<ToolStripMenuItem>(editor, "mnuEditCopy").Enabled && tree.SelectedNode.ContextMenuStrip == null,
                    "Native " + mode + " ammunition rows clear weapon details and expose no item context or destructive button");
                foreach (string command in new[] { "cmdDeleteWeapon_Click", "tsWeaponName_Click", "tsWeaponNotes_Click", "tsWeaponAddAccessory_Click", "tsWeaponAddModification_Click", "tsWeaponAddUnderbarrel_Click", "chkWeaponAccessoryInstalled_CheckedChanged", "mnuEditCopy_Click" }) Invoke(editor, command);
                if (career) foreach (string command in new[] { "tsWeaponSell_Click", "cmdReloadWeapon_Click", "cmsAmmoSingleShot_Click", "cmdWeaponMoveToVehicle_Click" }) Invoke(editor, command);
                else Invoke(editor, "mnuEditPaste_Click");
                check(character.CaptureSnapshot().SequenceEqual(original) && NativeDialogHost.Count == 0,
                    "Native " + mode + " queued weapon edit reload fire sell and clipboard commands reject ammunition projections");
                Field<TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<TabPage>(editor, "tabVehicles");
                vehicleTree.SelectedNode = Rows(vehicleTree)[0];
                foreach (string command in new[] { "cmdDeleteVehicle_Click", "tsVehicleWeaponNotes_Click", "tsVehicleAddWeaponAccessory_Click", "tsVehicleAddWeaponModification_Click", "tsVehicleWeaponModNotes_Click" }) Invoke(editor, command);
                if (career) foreach (string command in new[] { "tsVehicleSell_Click", "cmdReloadVehicleWeapon_Click", "cmsVehicleAmmoSingleShot_Click", "cmdVehicleMoveToInventory_Click" }) Invoke(editor, command);
                check(character.CaptureSnapshot().SequenceEqual(original) && !Field<Button>(editor, "cmdDeleteVehicle").Enabled,
                    "Native " + mode + " mounted ammunition projections cannot mutate sell or delete their vehicle or weapon");
                if (!career)
                {
                    Field<TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<TabPage>(editor, "tabStreetGear");
                    Field<TabControl>(editor, "tabStreetGearTabs").SelectedTab = Field<TabPage>(editor, "tabGear");
                    var gearTree = Field<TreeView>(editor, "treGear");
                    gearTree.SelectedNode = gearTree.Nodes[0].Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == reserve.InternalId);
                    editor.GetType().GetProperty("Loading")!.SetValue(editor, false);
                    Field<NumericUpDown>(editor, "nudGearQty").Value = 7;
                    check(reserve.Quantity == 7 && Rows(tree).All(row => row.Text == reserve.DisplayName),
                        "Native creation editing canonical Gear quantity refreshes every compatible weapon automatically");
                }
                else { reserve.Quantity = 7; WeaponInventoryViews.Refresh(character, tree, false); }
                check(Rows(tree).All(row => row.Text == reserve.DisplayName) && character.Gear.Count == 1 && vehicle.Gear.Count == 1,
                    "Native " + mode + " reserve quantity updates every weapon row without cloning backend inventory");
                tree.SelectedNode = Rows(tree)[0]; reserve.Quantity = 0; WeaponInventoryViews.Refresh(character, tree, false);
                check(Rows(tree).Length == 0 && tree.SelectedNode == null && Rows(vehicleTree).Length == 1,
                    "Native " + mode + " depleted shared stock disappears everywhere without selecting its parent or another pool");
                reserve.Quantity = 9; WeaponInventoryViews.Refresh(character, tree, false);
                Field<TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<TabPage>(editor, "tabStreetGear");
                Field<TabControl>(editor, "tabStreetGearTabs").SelectedTab = Field<TabPage>(editor, "tabWeapons");
                tree.SelectedNode = tree.Nodes[0].Nodes[0];
                check(Field<Label>(editor, "lblWeaponName").Text == first.DisplayNameShort && Field<Button>(editor, "cmdDeleteWeapon").Enabled,
                    "Native " + mode + " ordinary weapon selection regains details and commands after ammunition selection");
                if (career)
                {
                    // Multiple reserve subtypes force a real choice dialog. Cancelling must
                    // preserve canonical stock, loaded slots and every shared projection.
                    var alternate = new Gear(character) { Name = "Ammo: APDS", Category = "Ammunition", Extra = "Assault Rifles", Quantity = 6, Cost = "7" };
                    character.Gear.Add(alternate);
                    WeaponInventoryViews.Refresh(character, tree, false);
                    byte[] beforeCancel = character.CaptureSnapshot();
                    bool cancelledReload = false;
                    Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                        var dialog = Application.OpenForms.OfType<frmReload>().SingleOrDefault();
                        if (dialog == null) return;
                        cancelledReload = true;
                        dialog.DialogResult = DialogResult.Cancel;
                    });
                    editor.GetType().GetMethod("cmdReloadWeapon_Click", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                    check(cancelledReload && character.CaptureSnapshot().SequenceEqual(beforeCancel) && Rows(tree).Length == 6,
                        "Native career cancelling ammunition selection preserves reserve magazines and all shared views");
                    character.Gear.Remove(alternate);
                    reserve.Quantity = 40; WeaponInventoryViews.Refresh(character, tree, false);
                    var history = new InventoryUndo(character);
                    // Both ammo and loading-method lists have one choice: the existing reload
                    // dialog accepts itself. Exercise the real transfer and firing handlers.
                    editor.GetType().GetMethod("cmdReloadWeapon_Click", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                    check(first.AmmoRemaining == 38 && reserve.Quantity == 2 && Rows(tree).Length == 3 && Rows(tree).All(row => row.Text == reserve.DisplayName),
                        "Native career reload transfers canonical reserve into one magazine and refreshes all compatible views");
                    Invoke(editor, "cmsAmmoSingleShot_Click");
                    check(first.AmmoRemaining == 37 && reserve.Quantity == 2 && second.AmmoRemaining == 0,
                        "Native career firing consumes only loaded rounds without charging shared reserve twice");
                    tree.SelectedNode = tree.Nodes[0].Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == second.InternalId);
                    editor.GetType().GetMethod("cmdReloadWeapon_Click", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                    check(second.AmmoRemaining == 2 && first.AmmoRemaining == 37 && reserve.Quantity == 0 && Rows(tree).Length == 0,
                        "Native career another weapon exhausts shared reserve while preserving the first magazine");
                    history.Observe(); bool undone = history.Undo();
                    editor.GetType().GetMethod("RefreshAfterInventoryUndo", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);
                    check(undone && character.Gear.Single(item => item.InternalId == reserve.InternalId).Quantity == 40 &&
                        character.Weapons.All(item => item.AmmoRemaining == 0) && Rows(tree).Length == 3 && Rows(vehicleTree).Length == 1,
                        "Native career inventory undo rebuilds shared ammunition rows from restored canonical records");
                }
                if (career)
                {
                    // A full magazine in an ordinary container must consume the child reserve,
                    // unlike the existing full Spare Clip path which consumes one parent unit.
                    character.Gear.Clear();
                    var bag = new Gear(character) { Name = "Backpack", Category = "General", Quantity = 1, Cost = "0" };
                    var bagAmmo = new Gear(character) { Name = "Ammo: Regular", Category = "Ammunition", Extra = "Assault Rifles", Quantity = 38, Cost = "2", Parent = bag };
                    bag.Children.Add(bagAmmo); character.Gear.Add(bag);
                    editor.GetType().GetMethod("RefreshAfterInventoryUndo", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);
                    tree.SelectedNode = tree.Nodes[0].Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == first.InternalId);
                    editor.GetType().GetMethod("cmdReloadWeapon_Click", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                    check(character.Weapons.Single(item => item.InternalId == first.InternalId).AmmoRemaining == 38 && bagAmmo.Quantity == 0 && bag.Quantity == 1 && Rows(tree).Length == 0,
                        "Native career a full magazine in an ordinary container consumes its child reserve without duplicating rounds");
                    character.Gear.Clear();
                    var clip = new Gear(character) { Name = "Spare Clip", Category = "General", Quantity = 1, Cost = "0" };
                    var clipAmmo = new Gear(character) { Name = "Ammo: Regular", Category = "Ammunition", Extra = "Assault Rifles", Quantity = 38, Cost = "2", Parent = clip };
                    clip.Children.Add(clipAmmo); character.Gear.Add(clip);
                    var currentWeapon = character.Weapons.Single(item => item.InternalId == first.InternalId);
                    currentWeapon.AmmoRemaining = 0; currentWeapon.AmmoLoaded = Guid.Empty.ToString();
                    editor.GetType().GetMethod("RefreshAfterInventoryUndo", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);
                    tree.SelectedNode = tree.Nodes[0].Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == first.InternalId);
                    Field<CheckBox>(editor, "chkCommlinks").Checked = true;
                    bool filteredReload = true;
                    try { editor.GetType().GetMethod("cmdReloadWeapon_Click", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, new object[] { editor, EventArgs.Empty }); }
                    catch (TargetInvocationException error) when (error.InnerException is NullReferenceException) { filteredReload = false; }
                    check(filteredReload, "Native career spare-clip reload works when the Gear tree hides non-commlinks");
                    check(currentWeapon.AmmoRemaining == 38 && clip.Quantity == 0 && clipAmmo.Quantity == 38,
                        "Native career a full spare clip consumes one parent unit while preserving per-clip ammunition capacity");
                    check(WeaponAmmunition.Carried(character, currentWeapon).Count == 0 && Rows(tree).Length == 0,
                        "Native career an exhausted spare-clip stack cannot supply another weapon or remain in shared views");
                    // Switching the UI magazine selector must isolate loaded rounds while
                    // both slots continue sharing the same canonical reserve inventory.
                    character.Gear.Clear();
                    var slotReserve = new Gear(character) { Name = "Ammo: Regular", Category = "Ammunition", Extra = "Assault Rifles", Quantity = 40, Cost = "2" };
                    character.Gear.Add(slotReserve);
                    currentWeapon.Ammo = "2x38(c)";
                    currentWeapon.ActiveAmmoSlot = 2; currentWeapon.AmmoRemaining = 0;
                    currentWeapon.ActiveAmmoSlot = 1;
                    editor.GetType().GetMethod("RefreshAfterInventoryUndo", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null);
                    tree.SelectedNode = tree.Nodes[0].Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == first.InternalId);
                    Field<ComboBox>(editor, "cboWeaponAmmo").SelectedValue = "2";
                    editor.GetType().GetMethod("cmdReloadWeapon_Click", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                    Invoke(editor, "cmsAmmoSingleShot_Click");
                    check(currentWeapon.ActiveAmmoSlot == 2 && currentWeapon.AmmoRemaining == 37 && slotReserve.Quantity == 2,
                        "Native career second magazine reload and firing consume reserve once and only its loaded rounds");
                    Field<ComboBox>(editor, "cboWeaponAmmo").SelectedValue = "1";
                    check(currentWeapon.ActiveAmmoSlot == 1 && currentWeapon.AmmoRemaining == 38 && slotReserve.Quantity == 2,
                        "Native career switching magazine slots preserves the first loaded magazine and shared reserve");
                }
                // Only synthetic inventory is shown in these explicitly whitelisted images.
                // Restore the host preference even if rendering fails.
                var app = Avalonia.Application.Current!;
                var originalTheme = app.RequestedThemeVariant;
                try
                {
                    foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
                    {
                        app.RequestedThemeVariant = theme;
                        await Task.Delay(100);
                        var window = AvaloniaHostInterop.ToAvaloniaWindow(main);
                        using var image = new Avalonia.Media.Imaging.RenderTargetBitmap(new Avalonia.PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Avalonia.Vector(192, 192));
                        image.Render(window);
                        image.Save(Path.Combine(output, "native-ammunition-" + mode + "-" + theme.Key.ToString()!.ToLowerInvariant() + ".png"), Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                    }
                }
                finally { app.RequestedThemeVariant = originalTheme; }
            }
            finally { editor.GetType().GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false); editor.Close(); }
        }
    }
}
