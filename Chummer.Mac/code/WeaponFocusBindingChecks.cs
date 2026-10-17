#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class WeaponFocusBindingChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static int Remaining(frmCreate editor) => (int)typeof(frmCreate).GetMethod("CalculateBP", BindingFlags.Instance | BindingFlags.NonPublic)!.Invoke(editor, null)!;
    private static void Discard(frmCreate editor)
    {
        typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false); editor.Close();
    }
    internal static (Character Character, Weapon Weapon, WeaponMod Modification) Sample(Character source)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
        character.Foci.Clear(); character.StackedFoci.Clear(); character.BuildMethod = CharacterBuildMethod.BP;
        character.MAGEnabled = true; character.MagicianEnabled = true; character.MAG.Value = 6;
        var catalog = XmlManager.Instance.Load("weapons.xml");
        var weapon = new Weapon(character);
        weapon.Create(catalog.SelectNodes("/chummer/weapons/weapon")!.Cast<XmlNode>().First(node => node["name"]?.InnerText == "Katana"),
            character, new TreeNode(), null, null, null);
        var modification = new WeaponMod(character);
        modification.Create(catalog.SelectNodes("/chummer/mods/mod")!.Cast<XmlNode>().Single(node => node["name"]?.InnerText == "Weapon Focus"), new TreeNode());
        modification.Parent = weapon; modification.Rating = 2; weapon.WeaponMods.Add(modification); character.Weapons.Add(weapon);
        return (character, weapon, modification);
    }

    internal static void Run(Character source, Action<bool, string> check, Form? parent = null)
    {
        var (character, weapon, modification) = Sample(source);
        check(modification.TotalCost == 20000 && modification.TotalAvail == "10R",
            "new weapon focus modifications use the priced Gear focus catalog cost and restricted availability");
        var legacy = new Character();
        modification.Cost = "0"; modification.Avail = "0";
        legacy.LoadSnapshot(character.CaptureSnapshot());
        var legacyFocus = legacy.Weapons.Single(item => item.InternalId == weapon.InternalId).WeaponMods.Single(item => item.InternalId == modification.InternalId);
        check(legacyFocus.TotalCost == 0 && legacyFocus.TotalAvail == "0" && legacy.Nuyen == character.Nuyen,
            "legacy zero-priced weapon focus saves retain their original price availability and balance");
        modification.Cost = "Rating * 10000"; modification.Avail = "(Rating * 5)R";
        int originalGearCount = character.Gear.Count;
        using var editor = new frmCreate(character) { MdiParent = parent }; editor.Show();
        try
        {
            var tree = Field<TreeView>(editor, "treFoci");
            TreeNode Node() => tree.Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == modification.InternalId);
            check(!Node().Checked && Node().Text.Contains("Katana"), "weapon modification focus appears unbound beside gear foci");
            int remaining = Remaining(editor); string dice = weapon.DicePool; decimal cost = weapon.TotalCost;
            Node().Checked = true;
            check(character.Foci.Count(focus => focus.GearId == modification.InternalId) == 1 &&
                Remaining(editor) == remaining - 2 * character.Options.BPFocus,
                "binding a weapon modification focus charges force BP exactly once");
            check(character.Gear.Count == originalGearCount && weapon.TotalCost == cost && weapon.DicePool == dice,
                "weapon focus binding does not duplicate gear, purchase cost or the existing installed dice bonus");
            var gearFocus = new Gear(character) { Name = "Sustaining Focus", Category = "Foci", MaxRating = 6, Rating = 3 };
            character.Gear.Add(gearFocus);
            var totals = WeaponFocusBindings.TotalsIncluding(character, new[] { modification.InternalId }, gearFocus.InternalId);
            check(totals == (2, 5), "shared binding limits include weapon modification and gear focus forces");
            character.Gear.Remove(gearFocus);
            var restored = new Character(); restored.LoadSnapshot(character.CaptureSnapshot());
            check(restored.Foci.Single(focus => focus.GearId == modification.InternalId).Rating == 2 &&
                restored.Weapons.Single(item => item.InternalId == weapon.InternalId).WeaponMods.Single(item => item.InternalId == modification.InternalId).Name == "Weapon Focus",
                "weapon focus binding preserves canonical modification identity through the unchanged save format");
            using (var reopened = new frmCreate(restored) { MdiParent = parent })
            {
                reopened.Show();
                check(Field<TreeView>(reopened, "treFoci").Nodes.Cast<TreeNode>().Single(node => node.Tag?.ToString() == modification.InternalId).Checked,
                    "reopened weapon modification focus remains visibly bound");
                Discard(reopened);
            }
            var karmaBuild = new Character(); karmaBuild.LoadSnapshot(character.CaptureSnapshot());
            karmaBuild.BuildMethod = CharacterBuildMethod.Karma;
            using (var karmaEditor = new frmCreate(karmaBuild) { MdiParent = parent })
            {
                karmaEditor.Show();
                try
                {
                    int boundRemaining = Remaining(karmaEditor);
                    karmaBuild.Foci.RemoveAll(focus => focus.GearId == modification.InternalId);
                    karmaEditor.UpdateCharacterInfo();
                    check(Remaining(karmaEditor) == boundRemaining + 2 * karmaBuild.Options.KarmaWeaponFocus,
                        "Karma character creation uses the configured weapon focus multiplier");
                }
                finally { Discard(karmaEditor); }
            }
            restored.Foci.RemoveAll(focus => focus.GearId == modification.InternalId);
            restored.Created = true; restored.Karma = 100;
            bool previousConfirm = restored.Options.ConfirmKarmaExpense;
            restored.Options.ConfirmKarmaExpense = false;
            try
            {
                using var career = new frmCareer(restored) { MdiParent = parent }; career.Show();
                try
                {
                    int priorExpenses = restored.ExpenseEntries.Count;
                    var careerNode = Field<TreeView>(career, "treFoci").Nodes.Cast<TreeNode>()
                        .Single(node => node.Tag?.ToString() == modification.InternalId);
                    careerNode.Checked = true;
                    var expense = restored.ExpenseEntries.Last();
                    check(restored.Karma == 100 - 2 * restored.Options.KarmaWeaponFocus &&
                        restored.ExpenseEntries.Count == priorExpenses + 1 && expense.Undo.KarmaType == KarmaExpenseType.BindFocus &&
                        expense.Undo.ObjectId == modification.InternalId && restored.Foci.Any(focus => focus.GearId == modification.InternalId),
                        "career weapon focus binding charges configured Karma and records canonical expense undo identity");
                }
                finally
                {
                    typeof(frmCareer).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(career, false);
                    career.Close();
                }
            }
            finally { restored.Options.ConfirmKarmaExpense = previousConfirm; }
            Node().Checked = false;
            check(!character.Foci.Any(focus => focus.GearId == modification.InternalId) && Remaining(editor) == remaining,
                "unbinding a creation weapon focus releases its BP without deleting the modification");
            Node().Checked = true; modification.Rating = 3; editor.UpdateCharacterInfo();
            check(character.Foci.Single(focus => focus.GearId == modification.InternalId).Rating == 3 &&
                Remaining(editor) == remaining - 3 * character.Options.BPFocus,
                "bound weapon focus force and BP follow modification rating changes");
            weapon.WeaponMods.Remove(modification); editor.UpdateCharacterInfo();
            check(!character.Foci.Any(focus => focus.GearId == modification.InternalId) &&
                !tree.Nodes.Cast<TreeNode>().Any(node => node.Tag?.ToString() == modification.InternalId) && Remaining(editor) == remaining,
                "deleting the known weapon focus modification clears its binding and BP without orphaned UI entries");
            weapon.WeaponMods.Add(modification); editor.UpdateCharacterInfo(); Node().Checked = true;
            character.Weapons.Remove(weapon); editor.UpdateCharacterInfo();
            check(!character.Foci.Any(focus => focus.GearId == modification.InternalId) &&
                !tree.Nodes.Cast<TreeNode>().Any(node => node.Tag?.ToString() == modification.InternalId),
                "deleting the parent weapon clears the known modification focus binding");
            var unknown = new Focus { Name = "Weapon Focus", GearId = Guid.NewGuid().ToString(), Rating = 2 };
            character.Foci.Add(unknown); editor.UpdateCharacterInfo();
            check(character.Foci.Contains(unknown), "focus reconciliation preserves unknown legacy references rather than guessing their ownership");
        }
        finally { Discard(editor); }
    }
}
