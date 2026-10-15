#nullable enable
using System;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class ArmorPickerChecks
{
    internal static void Run(Character character, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        string previousCategory = ArmorPickerModel.LastCategory;
        var previousSources = character.Options.Books.ToArray();
        try
        {
            character.Options.Books.Clear(); character.Options.Books.Add("SR4");
            var model = new ArmorPickerModel(character, false) { SearchText = "Armor Jacket" };
            check(model.CanAccept && model.SelectedEntry?.Name == "Armor Jacket", "native armor picker finds armor from enabled sources");
            var expected = new Armor(character); expected.Create(model.SelectedEntry!.Definition, new TreeNode(), null, true);
            check(model.Details!.Ballistic == "8" && model.Details.Impact == "6" && model.Details.Capacity == "12" &&
                model.Details.Availability == expected.TotalAvail, "armor preview retains protection, derived capacity and availability rules");
            var selection = model.Accept(true);
            check(selection?.Name == "Armor Jacket" && selection.AddAgain && !selection.FreeCost,
                "armor acceptance retains selected identity and Add More intent");
            model.SearchText = "nonexistent \"quoted\" armor";
            check(model.IsEmpty && model.Details == null && !model.CanAccept && model.Accept(false) == null,
                "quoted armor searches are literal and clear stale details");
            var career = new ArmorPickerModel(character, true) { SearchText = "Armor Jacket", Markup = 25 };
            int baseCost = Convert.ToInt32(career.SelectedEntry!.Definition["cost"]!.InnerText);
            check(career.Details!.Cost == string.Format("{0:###,###,##0¥}", baseCost * 1.25), "armor markup retains the established preview calculation");
            career.SearchText = "Clothing";
            career.SelectedEntry = career.Items.Single(item => item.Name == "Clothing");
            check(career.Details!.Cost == string.Format("{0:###,###,##0}-{1:###,###,##0¥}", 20, 100000),
                "variable-price armor displays the original range without prompting during preview");
            career.FreeCost = true;
            check(career.Details!.Cost == string.Format("{0:###,###,##0¥}", 0) && career.Accept(false)!.FreeCost,
                "free armor purchase clears the preview cost and reaches the caller");
            character.Options.Books.Add("AR");
            model.SearchText = "Mitsuhama EE Suit";
            var sealedArmor = new Armor(character); sealedArmor.Create(model.SelectedEntry!.Definition, new TreeNode(), null, true);
            check(model.CanAccept && model.Details!.Availability == sealedArmor.TotalAvail && sealedArmor.ArmorMods.Any(modification => modification.Name == "Chemical Seal"),
                "armor preview retains included Chemical Seal and established availability handling");
            character.Options.Books.Remove("AR"); model.Refresh();
            check(model.IsEmpty && !model.CanAccept, "armor source changes remove unavailable choices without restart");
            model.SearchText = "Armor Jacket";
            character.Options.Books.Clear();
            check(model.Accept(false) == null && model.IsEmpty, "armor acceptance rejects a selection whose source was disabled");
            character.Options.Books.Add("SR4");
            var brokenCatalog = new XmlDocument(); brokenCatalog.LoadXml("<chummer><categories><category>Test</category></categories><armors>" +
                "<armor><name>Broken</name><category>Test</category><source>SR4</source></armor></armors></chummer>");
            var broken = new ArmorPickerModel(character, false, brokenCatalog);
            check(broken.HasError && broken.Details == null && !broken.CanAccept,
                "malformed armor displays an error and cannot be accepted");
        }
        finally
        {
            character.Options.Books.Clear(); character.Options.Books.AddRange(previousSources);
            ArmorPickerModel.LastCategory = previousCategory;
        }
        check(character.CaptureSnapshot().SequenceEqual(original), "browsing armor and included modifications never changes character data");
    }
}
