#nullable enable
using System;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class PersonalizedGripChecks
{
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
                var model = picker.CreateModel(); model.SearchText = "Personalized Grip";
                check(model.Items.Count == 1 && model.Items[0].Name == context.Item3 &&
                    model.Items[0].DisplayName == WeaponModificationNames.DisplayName(context.Item3, null),
                    "single Personalized Grip entry resolves the canonical " + context.Item1 + " definition");
                var selected = model.Accept(false)!;
                check(selected.Name == context.Item3, "grip selection returns its legacy " + context.Item1 + " catalog name");
                var catalog = XmlManager.Instance.Load("weapons.xml");
                var weapon = new Weapon(copy);
                weapon.Create(catalog.SelectNodes("/chummer/weapons/weapon")!.Cast<XmlNode>().First(node => node["name"]?.InnerText == context.Item2), copy,
                    new TreeNode(), null, null, null);
                int originalDice = Convert.ToInt32(weapon.DicePool.Split(' ')[0]);
                int originalRecoil = Convert.ToInt32(weapon.TotalRC.Split(' ')[0]);
                var modification = new WeaponMod(copy);
                modification.Create(catalog.SelectNodes("/chummer/mods/mod")!.Cast<XmlNode>().Single(node => node["name"]?.InnerText == selected.Name), new TreeNode());
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
            var filteredModel = filtered.CreateModel(); filteredModel.SearchText = "no \"quoted\" modification";
            check(filteredModel.Items.Count == 0, "modification search treats quotes as literal text");
            character.Options.Books.Remove("AR"); filteredModel.SearchText = "Personalized Grip";
            check(filteredModel.Items.Count == 0, "unified grip still obeys enabled sourcebooks");
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(previousBooks); }
        check(character.CaptureSnapshot().SequenceEqual(original), "grip checks do not alter the original character or existing saved modifications");
    }
}
