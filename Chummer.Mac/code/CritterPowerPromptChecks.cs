using System;
using System.Linq;
using System.Reflection;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

// Characterize the shipping dialog before replacing its category/rating UI.
internal static class CritterPowerPromptChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    private static void Invoke(object owner, string method) => owner.GetType().GetMethod(method, Flags)!.Invoke(owner, new object[] { owner, EventArgs.Empty });
    internal static void Run(Character source, Action<bool, string> check)
    {
        var remembered = typeof(frmSelectCritterPower).GetField("_strSelectCategory", BindingFlags.Static | BindingFlags.NonPublic)!;
        var previous = remembered.GetValue(null);
        try
        {
            foreach (bool spirit in new[] { false, true })
            {
                var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
                character.Metatype = spirit ? "Free Spirit" : "Human";
                character.MetatypeCategory = spirit ? "Special" : "Metahuman";
                character.IsCritter = false;
                remembered.SetValue(null, "");
                using var dialog = new frmSelectCritterPower(character);
                Invoke(dialog, "frmSelectCritterPower_Load");
                var category = Field<ComboBox>(dialog, "cboCategory");
                var tree = Field<TreeView>(dialog, "trePowers");
                var rating = Field<NumericUpDown>(dialog, "nudCritterPowerRating");
                var categories = ((System.Collections.Generic.List<ListItem>)category.DataSource).Select(item => item.Value).ToArray();
                check(categories.Contains("Allowed Optional Powers") == spirit && categories.Contains("Free Spirit") == spirit &&
                    !categories.Contains("Toxic Critter Powers") && !categories.Contains("Shapeshifter"),
                    "Legacy critter categories preserve metatype eligibility " + spirit);
                check(rating.Minimum == 1 && rating.Maximum == 100 && rating.Value == 1,
                    "Legacy critter rating retains integer range and default " + spirit);
                var catalog = XmlManager.Instance.Load("critterpowers.xml");
                var metatype = XmlManager.Instance.Load("metatypes.xml").SelectSingleNode("/chummer/metatypes/metatype[name='" + character.Metatype + "']")!;
                foreach (string available in categories)
                {
                    category.SelectedValue = available;
                    var extracted = CritterPowerEligibility.Select(catalog, metatype, available, character.CritterPowers.Select(power => power.Name));
                    check(tree.Nodes.Cast<TreeNode>().Select(node => node.Tag.ToString()).OrderBy(name => name)
                        .SequenceEqual(extracted.Select(node => node["name"]!.InnerText).OrderBy(name => name)),
                        "Extracted critter eligibility matches legacy category " + available + " spirit=" + spirit);
                }
                if (spirit)
                {
                    character.CritterPowers.Clear();
                    character.CritterPowers.Add(new CritterPower(character) { Name = "Materialization" });
                    category.SelectedValue = "Weakness";
                    category.SelectedValue = "Allowed Optional Powers";
                    check(tree.Nodes.Cast<TreeNode>().Select(node => node.Tag.ToString()).OrderBy(name => name)
                        .SequenceEqual(CritterPowerEligibility.Select(catalog, metatype, "Allowed Optional Powers", new[] { "Materialization" })
                            .Select(node => node["name"]!.InnerText).OrderBy(name => name)),
                        "Extracted critter eligibility preserves owned physical presence behavior");
                }
                category.SelectedValue = spirit ? "Allowed Optional Powers" : "Weakness";
                if (!spirit)
                {
                    var expected = catalog.SelectNodes("/chummer/powers/power[category='Weakness']")!.Cast<XmlNode>().Select(node => node["name"]!.InnerText).OrderBy(name => name);
                    check(tree.Nodes.Cast<TreeNode>().Select(node => node.Tag.ToString()).OrderBy(name => name).SequenceEqual(expected),
                        "Legacy weakness category retains exact canonical identities");
                    continue;
                }
                var definition = XmlManager.Instance.Load("metatypes.xml").SelectSingleNode("/chummer/metatypes/metatype[name='Free Spirit']")!;
                var optional = definition.SelectNodes("optionalpowers/power")!.Cast<XmlNode>().Select(node => node.InnerText).ToArray();
                check(optional.All(name => tree.Nodes.Cast<TreeNode>().Any(node => node.Tag.ToString() == name)),
                    "Legacy Free Spirit optional choices retain all canonical power identities");
                tree.SelectedNode = tree.Nodes.Cast<TreeNode>().Single(node => node.Tag.ToString() == "Accident");
                // Selection handlers are normally raised by the tree; invoke acceptance only.
                Invoke(dialog, "cmdOK_Click");
                check(dialog.DialogResult == DialogResult.OK && dialog.SelectedPower == "Accident" && dialog.PowerPoints == 0.5,
                    "Legacy Free Spirit acceptance retains fractional point cost");
            }
        }
        finally { remembered.SetValue(null, previous); }
    }
}
