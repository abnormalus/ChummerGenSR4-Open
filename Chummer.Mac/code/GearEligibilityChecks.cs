#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Majorsilence.Forms;

namespace Chummer;

internal static class GearEligibilityChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    internal static void Run(Character character, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        var books = character.Options.Books.ToArray();
        try
        {
            if (!character.Options.Books.Contains("AR")) character.Options.Books.Add("AR");
            using (var categoryPicker = new frmSelectGear(character) { AllowedCategories = "Commlink" })
            {
                categoryPicker.Show();
                var list = Field<ListBox>(categoryPicker, "lstGear");
                string[] categoryItems = list.Items.Cast<ListItem>().Select(item => item.Value).OrderBy(name => name).ToArray();
                check(categoryItems.Length > 0, "commlink category provides an eligible search baseline");
                foreach (string query in new[] { "commlink", "comlink", "COMLINKS", "commlinks" })
                {
                    Field<TextBox>(categoryPicker, "txtSearch").Text = query;
                    check(categoryItems.SequenceEqual(list.Items.Cast<ListItem>().Select(item => item.Value.Split('^')[0]).OrderBy(name => name)),
                        "gear category search includes all eligible commlinks for " + query);
                }
                Field<TextBox>(categoryPicker, "txtSearch").Text = "comlink ' or true()";
                check(list.Items.Count == 0, "category search treats quotes and operators literally");
                categoryPicker.Close();
            }
            var sample = new System.Xml.XmlDocument(); sample.LoadXml("<gear><name>Model</name><translate>Modèle</translate><category>Commlink</category></gear>");
            check(CatalogSearch.MatchesNameOrCategory(sample.DocumentElement!, "liaison", "Appareils de liaison") &&
                CatalogSearch.MatchesNameOrCategory(sample.DocumentElement!, "Modèle", "Appareils de liaison"),
                "catalog category search retains localized category and item names");
            void Compare(frmSelectGear picker, string excluded, string included, string description)
            {
                using (picker)
                {
                    picker.Show(); var list = Field<ListBox>(picker, "lstGear");
                    check(!list.Items.Cast<ListItem>().Any(item => item.Value == excluded) && list.Items.Cast<ListItem>().Any(item => item.Value == included),
                        description + " browsing retains established eligibility");
                    Field<TextBox>(picker, "txtSearch").Text = Field<ComboBox>(picker, "cboCategory").SelectedValue?.ToString() ?? "";
                    check(list.Items.Cast<ListItem>().All(item => !item.Value.StartsWith(excluded + "^", StringComparison.Ordinal)),
                        description + " category search retains contextual restrictions");
                    Field<TextBox>(picker, "txtSearch").Text = excluded;
                    check(!list.Items.Cast<ListItem>().Any(item => item.Value.StartsWith(excluded + "^", StringComparison.Ordinal)), description + " search cannot restore an ineligible choice");
                    Field<TextBox>(picker, "txtSearch").Text = included;
                    check(list.Items.Cast<ListItem>().Any(item => item.Value.StartsWith(included + "^", StringComparison.Ordinal)),
                        description + " search retains eligible choices");
                    picker.Close();
                }
            }
            Compare(new frmSelectGear(character) { AllowedCategories = "Commlink Upgrade", CommlinkResponse = 3, CommlinkSignal = 3 },
                "Response 1", "Response 4", "commlink lower upgrade bound");
            Compare(new frmSelectGear(character) { AllowedCategories = "Commlink Upgrade", CommlinkResponse = 3, CommlinkSignal = 3 },
                "Response 6", "Response 5", "commlink upper upgrade bound");
            Compare(new frmSelectGear(character) { AllowedCategories = "Commlink Operating System Upgrade", CommlinkFirewall = 3, CommlinkSystem = 3 },
                "Firewall 1", "Firewall 4", "operating-system upgrade");
            Compare(new frmSelectGear(character) { AllowedCategories = "Electronics and Sensor Options", ShowPositiveCapacityOnly = true },
                "Self-Destruct, Area Effect", "Skinlink", "positive capacity");
            Compare(new frmSelectGear(character) { AllowedCategories = "Electronics and Sensor Options", ShowNegativeCapacityOnly = true },
                "Skinlink", "Self-Destruct, Area Effect", "installed capacity");
            Compare(new frmSelectGear(character) { AllowedCategories = "Electronics and Sensor Options", ShowArmorCapacityOnly = true },
                "Self-Destruct, Area Effect", "Skinlink", "armor capacity");
            using (var exhausted = new frmSelectGear(character) { AllowedCategories = "Commlink Operating System Upgrade", CommlinkFirewall = 10, CommlinkSystem = 10 })
            {
                exhausted.Show();
                check(Field<ComboBox>(exhausted, "cboCategory").Items.Count == 0 && !Field<Button>(exhausted, "cmdOK").Enabled,
                    "gear with no eligible categories opens safely with purchasing disabled");
                exhausted.Close();
            }
            using (var custom = new frmSelectGear(character) { AllowedCategories = "Commlink Upgrade", CommlinkResponse = 0, CommlinkSignal = 0 })
            {
                custom.Show(); Field<TextBox>(custom, "txtSearch").Text = "Response 8";
                check(Field<ListBox>(custom, "lstGear").Items.Count > 0, "custom commlinks retain the established unrestricted hardware-upgrade exception");
                custom.Close();
            }
            using (var changed = new frmSelectGear(character) { AllowedCategories = "Commlink Upgrade", CommlinkResponse = 3, CommlinkSignal = 3 })
            {
                changed.Show(); Field<ListBox>(changed, "lstGear").SelectedValue = "Response 4";
                changed.CommlinkResponse = 5; changed.CommlinkSignal = 5;
                Field<Button>(changed, "cmdOK").PerformClick();
                check(changed.DialogResult != DialogResult.OK && changed.SelectedGear == "",
                    "gear confirmation rejects context changed after selection without buying a replacement");
                changed.Close();
            }
            using (var changed = new frmSelectGear(character) { AllowedCategories = "Commlink Upgrade", CommlinkResponse = 3, CommlinkSignal = 3 })
            {
                changed.Show(); Field<ListBox>(changed, "lstGear").SelectedValue = "Response 4";
                character.Options.Books.Clear(); Field<Button>(changed, "cmdOK").PerformClick();
                check(changed.DialogResult != DialogResult.OK && changed.SelectedGear == "" && Field<ListBox>(changed, "lstGear").Items.Count == 0,
                    "gear confirmation rejects a source disabled after selection");
                changed.Close();
            }
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(books); }
        check(character.CaptureSnapshot().SequenceEqual(original), "gear eligibility checks preserve the source character and settings");
    }
}
