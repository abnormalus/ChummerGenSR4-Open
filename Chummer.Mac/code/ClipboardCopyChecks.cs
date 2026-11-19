#nullable enable
using System;
using System.Collections.Generic;
using System.Reflection;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Exercise actual Copy commands before extracting their repeated XML wrapper.
// The dangling-weapon case records existing non-atomic type publication, not a desired fix.
internal static class ClipboardCopyChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static void CheckItemFamilies(Action<bool, string> check)
    {
        var character = new Character();
        var lookup = new CommonFunctions(character);
        var clipboard = GlobalOptions.Instance;
        var priorDocument = clipboard.Clipboard;
        var priorType = clipboard.ClipboardContentType;

        void Expect(InventoryCopyTarget target, string id, ClipboardContentType type, string element, int level = 1)
        {
            bool refresh = InventoryCopyCommand.Copy(character, lookup, target, id, level);
            check(refresh && clipboard.ClipboardContentType == type &&
                clipboard.Clipboard.SelectSingleNode("/character/" + element + "/guid")?.InnerText == id,
                "Inventory Copy writes expected identity and item family " + target + " " + type);
        }

        try
        {
            var lifestyle = new Lifestyle(character) { Name = "Synthetic lifestyle" };
            var armor = new Armor(character) { Name = "Synthetic armor" };
            var weapon = new Weapon(character) { Name = "Synthetic weapon", Category = "Pistols" };
            var vehicle = new Vehicle(character) { Name = "Synthetic vehicle" };
            var mounted = new Weapon(character) { Name = "Synthetic mounted weapon", Category = "Pistols" };
            var modification = new VehicleMod(character);
            character.Lifestyles.Add(lifestyle);
            character.Armor.Add(armor);
            character.Weapons.Add(weapon);
            character.Vehicles.Add(vehicle);
            vehicle.Mods.Add(modification);
            modification.Weapons.Add(mounted);

            var armorGear = new Commlink(character) { Name = "Synthetic commlink" };
            var weaponGear = new OperatingSystem(character) { Name = "Synthetic operating system" };
            var vehicleGear = new Gear(character) { Name = "Synthetic vehicle gear", WeaponID = weapon.InternalId };
            armor.Gear.Add(armorGear);
            var accessory = new WeaponAccessory(character);
            weapon.WeaponAccessories.Add(accessory);
            accessory.Gear.Add(weaponGear);
            vehicle.Gear.Add(vehicleGear);

            Expect(InventoryCopyTarget.Lifestyle, lifestyle.InternalId, ClipboardContentType.Lifestyle, "lifestyle");
            Expect(InventoryCopyTarget.Armor, armor.InternalId, ClipboardContentType.Armor, "armor");
            Expect(InventoryCopyTarget.Armor, armorGear.InternalId, ClipboardContentType.Commlink, "gear", 2);
            Expect(InventoryCopyTarget.Weapon, weapon.InternalId, ClipboardContentType.Weapon, "weapon");
            Expect(InventoryCopyTarget.Weapon, weaponGear.InternalId, ClipboardContentType.OperatingSystem, "gear", 2);
            Expect(InventoryCopyTarget.Vehicle, vehicle.InternalId, ClipboardContentType.Vehicle, "vehicle");
            Expect(InventoryCopyTarget.Vehicle, mounted.InternalId, ClipboardContentType.Weapon, "weapon", 2);
            Expect(InventoryCopyTarget.Vehicle, vehicleGear.InternalId, ClipboardContentType.Gear, "gear", 2);
            check(clipboard.Clipboard.SelectSingleNode("/character/weapon/guid")?.InnerText == weapon.InternalId,
                "Inventory Copy includes the weapon associated with copied vehicle gear");

            var copied = clipboard.Clipboard;
            weapon.Category = "Gear";
            mounted.Category = "Cyberware Weapons";
            check(!InventoryCopyCommand.Copy(character, lookup, InventoryCopyTarget.Weapon, weapon.InternalId, 1) &&
                !InventoryCopyCommand.Copy(character, lookup, InventoryCopyTarget.Vehicle, mounted.InternalId, 2) &&
                ReferenceEquals(clipboard.Clipboard, copied) && clipboard.ClipboardContentType == ClipboardContentType.Gear,
                "Inventory Copy protects Gear and Cyberware weapons without replacing the clipboard");
        }
        finally { clipboard.Clipboard = priorDocument; clipboard.ClipboardContentType = priorType; }
    }

    internal static void Run(Character source, Action<bool, string> check)
    {
        var priorDocument = GlobalOptions.Instance.Clipboard;
        var priorType = GlobalOptions.Instance.ClipboardContentType;
        try
        {
            foreach (bool career in new[] { false, true })
            {
                var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = career;
                var gear = new Gear(character); var node = new Forms.TreeNode();
                gear.Create(XmlManager.Instance.Load("gear.xml").SelectSingleNode("/chummer/gears/gear[name='Medkit']")!, character, node, 1,
                    new List<Weapon>(), new List<Forms.TreeNode>());
                const string notes = "Synthetic copy <雪> & '😀'"; gear.Notes = notes;
                character.Gear.Add(gear);
                using Forms.Form editor = career ? new frmCareer(character) { Loading = true } : new frmCreate(character) { Loading = true };
                Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
                Field<Forms.TabControl>(editor, "tabStreetGearTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabGear");
                var tree = Field<Forms.TreeView>(editor, "treGear");
                tree.Nodes.Clear(); var root = new Forms.TreeNode("Synthetic root"); root.Nodes.Add(node); tree.Nodes.Add(root); tree.SelectedNode = node;
                void Copy() => editor.GetType().GetMethod("mnuEditCopy_Click", Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                XmlDocument Sentinel()
                {
                    var doc = new XmlDocument(); doc.LoadXml("<character><sentinel /></character>");
                    GlobalOptions.Instance.Clipboard = doc; GlobalOptions.Instance.ClipboardContentType = ClipboardContentType.Lifestyle; return doc;
                }
                gear.WeaponID = Guid.NewGuid().ToString();
                var sentinel = Sentinel(); byte[] before = character.CaptureSnapshot(); Copy();
                check(ReferenceEquals(GlobalOptions.Instance.Clipboard, sentinel) && GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear,
                    "Legacy Gear Copy failure retains prior document after publishing Gear type career=" + career + " actualType=" + GlobalOptions.Instance.ClipboardContentType);
                check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
                    "Legacy failed Gear Copy preserves character snapshot career=" + career);
                gear.WeaponID = Guid.Empty.ToString(); Sentinel(); before = character.CaptureSnapshot(); Copy();
                var copied = GlobalOptions.Instance.Clipboard;
                check(GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Gear && copied.DocumentElement?.Name == "character" &&
                    copied.FirstChild is XmlDeclaration declaration && declaration.Encoding == "utf-16" && copied.SelectSingleNode("/character/gear/notes")?.InnerText == notes,
                    "Legacy Gear Copy preserves character-root Unicode declaration and escaped notes career=" + career);
                check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
                    "Legacy successful Gear Copy preserves character snapshot career=" + career);
                // Invalidate only the selected identity: raising AfterSelect on a
                // deliberately missing model would exercise a separate refresh path.
                node.Tag = Guid.NewGuid().ToString();
                sentinel = Sentinel(); Copy();
                check(ReferenceEquals(GlobalOptions.Instance.Clipboard, sentinel) && GlobalOptions.Instance.ClipboardContentType == ClipboardContentType.Lifestyle,
                    "Legacy missing Gear selection preserves clipboard document and type career=" + career);
            }
        }
        finally { GlobalOptions.Instance.Clipboard = priorDocument; GlobalOptions.Instance.ClipboardContentType = priorType; }
    }
}
