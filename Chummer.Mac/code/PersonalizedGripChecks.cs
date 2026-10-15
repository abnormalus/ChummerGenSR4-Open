#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class PersonalizedGripChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    internal static void Run(Character character, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        var previousBooks = character.Options.Books.ToArray();
        var copy = new Character(); copy.LoadSnapshot(original);
        try
        {
            if (!character.Options.Books.Contains("AR")) character.Options.Books.Add("AR");
            foreach (var context in new[] { ("Melee", "Combat Axe", WeaponModificationNames.MeleePersonalizedGrip),
                ("Ranged", "AK-97", WeaponModificationNames.PersonalizedGrip) })
            {
                using var picker = new frmSelectVehicleMod(character) { InputFile = "weapons", WeaponType = context.Item1 };
                picker.Show(); Field<TextBox>(picker, "txtSearch").Text = "Personalized Grip";
                var list = Field<ListBox>(picker, "lstMod");
                check(list.Items.Count == 1 && ((ListItem)list.Items[0]).Value == context.Item3 &&
                    ((ListItem)list.Items[0]).Name == WeaponModificationNames.DisplayName(context.Item3, null),
                    "single Personalized Grip entry resolves the canonical " + context.Item1 + " definition");
                list.SelectedIndex = 0;
                Field<Button>(picker, "cmdOK").PerformClick();
                check(picker.SelectedMod == context.Item3, "grip selection returns its legacy " + context.Item1 + " catalog name");
                picker.Close();
                var catalog = XmlManager.Instance.Load("weapons.xml");
                var weapon = new Weapon(copy);
                weapon.Create(catalog.SelectNodes("/chummer/weapons/weapon")!.Cast<XmlNode>().First(node => node["name"]?.InnerText == context.Item2), copy,
                    new TreeNode(), null, null, null);
                int originalDice = Convert.ToInt32(weapon.DicePool.Split(' ')[0]);
                int originalRecoil = Convert.ToInt32(weapon.TotalRC.Split(' ')[0]);
                var modification = new WeaponMod(copy);
                modification.Create(catalog.SelectNodes("/chummer/mods/mod")!.Cast<XmlNode>().Single(node => node["name"]?.InnerText == picker.SelectedMod), new TreeNode());
                modification.Parent = weapon; weapon.WeaponMods.Add(modification); copy.Weapons.Add(weapon);
                check(modification.DisplayNameShort == WeaponModificationNames.DisplayName(context.Item3, null) &&
                    Convert.ToInt32(weapon.DicePool.Split(' ')[0]) == originalDice + (context.Item1 == "Melee" ? 1 : 0) &&
                    Convert.ToInt32(weapon.TotalRC.Split(' ')[0]) == originalRecoil + (context.Item1 == "Ranged" ? 1 : 0),
                    "unified grip applies the established " + context.Item1 + " dice/recoil bonus");
                var reloaded = new Character(); reloaded.LoadSnapshot(copy.CaptureSnapshot());
                var restored = reloaded.Weapons.Single(item => item.InternalId == weapon.InternalId).WeaponMods.Single(item => item.InternalId == modification.InternalId);
                check(restored.Name == context.Item3 && restored.DicePool == modification.DicePool && restored.RC == modification.RC,
                    "grip round trip preserves the original " + context.Item1 + " name and bonus fields for older Chummer");
            }
            using var filtered = new frmSelectVehicleMod(character) { InputFile = "weapons", WeaponType = "Melee" };
            filtered.Show(); Field<TextBox>(filtered, "txtSearch").Text = "no \"quoted\" modification";
            check(Field<ListBox>(filtered, "lstMod").Items.Count == 0, "modification search treats quotes as literal text");
            character.Options.Books.Remove("AR"); Field<TextBox>(filtered, "txtSearch").Text = "Personalized Grip";
            check(Field<ListBox>(filtered, "lstMod").Items.Count == 0, "unified grip still obeys enabled sourcebooks");
            filtered.Close();
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(previousBooks); }
        check(character.CaptureSnapshot().SequenceEqual(original), "grip checks do not alter the original character or existing saved modifications");
    }
}
