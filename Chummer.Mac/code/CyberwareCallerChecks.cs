#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class CyberwareCallerChecks
{
    private const BindingFlags Flags = BindingFlags.NonPublic | BindingFlags.Instance;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;

    internal static void RunLookup(Action<bool, string> check)
    {
        const string name = "Synthetic \"Ware's\" Node";
        var catalog = new XmlDocument(); catalog.LoadXml("<chummer><cyberwares><cyberware><name/><category>Headware</category></cyberware></cyberwares></chummer>");
        var expected = catalog.SelectSingleNode("/chummer/cyberwares/cyberware")!; expected["name"]!.InnerText = name;
        XmlNode? actual = null; Exception? error = null;
        try { actual = CyberwareSelectionPolicy.FindDefinition(catalog, false, name, "Headware"); }
        catch (Exception failure) { error = failure; }
        check(ReferenceEquals(actual, expected), "Cyberware caller lookup preserves quoted canonical name [error=" + (error?.GetType().Name ?? "none") + "]");
        var duplicate = expected.CloneNode(true); duplicate["category"]!.InnerText = "Bodyware"; expected.ParentNode!.AppendChild(duplicate);
        check(ReferenceEquals(CyberwareSelectionPolicy.FindDefinition(catalog, false, name, "Bodyware"), duplicate),
            "Cyberware caller lookup distinguishes identical names in different canonical categories");
        var biowares = catalog.CreateElement("biowares"); catalog.DocumentElement!.AppendChild(biowares);
        var bioware = catalog.CreateElement("bioware"); bioware.InnerXml = expected.InnerXml; bioware["category"]!.InnerText = "Basic"; biowares.AppendChild(bioware);
        check(ReferenceEquals(CyberwareSelectionPolicy.FindDefinition(catalog, true, name, "Basic"), bioware),
            "Cyberware caller lookup retains Bioware mode and canonical category");
        check(CyberwareSelectionPolicy.FindDefinition(catalog, false, name, "Missing category") == null &&
            CyberwareSelectionPolicy.FindDefinition(catalog, false, "", "Headware") == null,
            "Cyberware caller lookup rejects missing or unaccepted canonical identity");
    }

    internal static void Run(Character original, Action<bool, string> check)
    {
        using var main = new Form(); main.Show();
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = career;
            foreach (string book in new[] { "SR4", "AR", "AU" }) if (!character.Options.Books.Contains(book)) character.Options.Books.Add(book);
            using Form editor = career ? new frmCareer(character) { Loading = true, MdiParent = main } : new frmCreate(character) { Loading = true, MdiParent = main };
            editor.Show();
            var previousNotice = AppErrors.Notify;
            try
            {
                var vehicle = new Vehicle(character); var vehicleNode = new TreeNode();
                var vehicles = XmlManager.Instance.Load("vehicles.xml");
                vehicle.Create(vehicles.SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!, vehicleNode, null, null, null, null, null, false);
                var mod = new VehicleMod(character); var modNode = new TreeNode();
                mod.Create(vehicles.SelectSingleNode("/chummer/mods/mod[name='Mechanical Arm']")!, modNode, 0);
                vehicle.Mods.Add(mod); character.Vehicles.Add(vehicle);
                var tree = Field<TreeView>(editor, "treVehicles"); tree.Nodes[0].Nodes.Add(vehicleNode); vehicleNode.Nodes.Add(modNode);
                Field<TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<TabPage>(editor, "tabVehicles"); tree.SelectedNode = modNode;
                check(mod.AllowCyberware && mod.Subsystems.Length > 0, "vehicle Cyberware caller retains real parent fixture career=" + career);
                // Empty context must refuse before requesting a native backend.
                // Nonempty actual-window purchase/cancel coverage belongs to native CI.
                mod.Subsystems = "No matching category";
                // Record the fixture inventory before measuring cancellation. A modal loop may
                // process an already queued inventory observation from fixture setup.
                Field<InventoryUndo>(editor, "_inventoryUndo").Observe();
                byte[] before = character.CaptureSnapshot(); bool dirtyBefore = Field<bool>(editor, "_blnIsDirty");
                int historyBefore = Field<InventoryUndo>(editor, "_inventoryUndo").Count;
                int nativeBefore = NativeDialogHost.Count, notices = 0;
                AppErrors.Notify = (_, message, _) =>
                {
                    if (message != LanguageManager.Instance.GetString("Message_NoCompatibleCyberwareAddins"))
                        throw new InvalidOperationException("Unexpected Cyberware caller notice.");
                    notices++;
                };
                editor.GetType().GetMethod("tsVehicleAddCyberware_Click", Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                check(notices == 1 && NativeDialogHost.Count == nativeBefore && editor.Enabled,
                    "actual vehicle Cyberware empty context cancels before native UI and preserves owner career=" + career);
                check(character.CaptureSnapshot().SequenceEqual(before) && Field<bool>(editor, "_blnIsDirty") == dirtyBefore &&
                    Field<InventoryUndo>(editor, "_inventoryUndo").Count == historyBefore,
                    "actual vehicle Cyberware empty cancellation preserves snapshot dirty state and undo career=" + career +
                    " snapshot=" + character.CaptureSnapshot().SequenceEqual(before) +
                    " dirty=" + (Field<bool>(editor, "_blnIsDirty") == dirtyBefore) +
                    " undo=" + (Field<InventoryUndo>(editor, "_inventoryUndo").Count == historyBefore));
            }
            finally
            {
                AppErrors.Notify = previousNotice;
                editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close();
            }
        }
    }
}
