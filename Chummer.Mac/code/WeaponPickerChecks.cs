#nullable enable
using System;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class WeaponPickerChecks
{
    internal static void Run(Character character, Action<bool, string> check)
    {
        byte[] before = character.CaptureSnapshot();
        string previousCategory = WeaponPickerModel.LastCategory;
        try
        {
            var model = new WeaponPickerModel(character, false);
            model.SearchText = "Ares Predator";
            check(model.Items.Count > 0 && model.CanAccept, "weapon picker searches enabled sources across categories");
            var entry = model.SelectedEntry!;
            var expected = new Weapon(character); expected.Create(entry.Definition, character, new TreeNode(), null, null, null);
            check(model.Details!.Damage == expected.CalculatedDamage() && model.Details.ArmorPenetration == expected.TotalAP &&
                model.Details.Ammo == expected.CalculatedAmmo() && model.Details.Availability == expected.TotalAvail,
                "native weapon picker retains established weapon calculations");
            var accepted = model.Accept(true);
            check(accepted?.Name == entry.Name && accepted.Category == entry.Category && accepted.AddAgain && !accepted.FreeCost,
                "weapon picker acceptance retains category identity and add-more intent");
            model.SearchText = "a nonexistent \"quoted\" weapon";
            check(model.IsEmpty && !model.CanAccept && model.Details == null && model.Accept(false) == null,
                "empty and quoted weapon searches clear details and cannot accept stale selections");
            model.SearchText = "Ares Predator";
            var career = new WeaponPickerModel(character, true) { SearchText = "Ares Predator", Markup = 25 };
            double baseCost = Convert.ToDouble(career.SelectedEntry!.Definition["cost"]!.InnerText, GlobalOptions.Instance.CultureInfo);
            check(career.Details!.Cost == string.Format("{0:###,###,##0¥}", baseCost * 1.25), "weapon markup preview preserves the existing cost calculation");
            career.FreeCost = true;
            check(career.Details!.Cost == string.Format("{0:###,###,##0¥}", 0) && career.Accept(false)!.FreeCost,
                "career free purchase is reflected in the preview and selection result");
            career.Markup = -1000; check(career.Markup == -99, "weapon markup retains its supported lower bound");
            career.Markup = 1001; check(career.Markup == 1000, "weapon markup retains its supported upper bound");
            check(character.CaptureSnapshot().SequenceEqual(before), "browsing native weapon choices never changes the character");

            var brokenCatalog = new XmlDocument(); brokenCatalog.LoadXml("<chummer><categories><category>Test</category></categories><weapons>" +
                "<weapon><name>Broken</name><category>Test</category><source>SR4</source></weapon></weapons></chummer>");
            var broken = new WeaponPickerModel(character, false, brokenCatalog);
            check(broken.HasError && broken.Details == null && !broken.CanAccept && broken.Accept(false) == null,
                "broken weapon definitions fail visibly without retaining old details or accepting an item");

            var sources = character.Options.Books.ToArray();
            try
            {
                character.Options.Books.Clear(); character.Options.Books.Add("SR4");
                var sourceAware = new WeaponPickerModel(character, false) { SearchText = "" };
                sourceAware.SearchText = "a";
                check(sourceAware.Items.Count > 0 && sourceAware.Items.All(item => item.Definition["source"]!.InnerText == "SR4"), "weapon search obeys current character source settings");
                character.Options.Books.Add("AR");
                var bottles = new WeaponPickerModel(character, false) { SearchText = "Bottle" };
                bottles.SelectedEntry = bottles.Items.Single(item => item.Name == "Bottle" && item.Category == "Clubs");
                check(bottles.Accept(false)?.Category == "Clubs" && bottles.Details!.Damage.EndsWith("S", StringComparison.Ordinal),
                    "same-name weapons retain their selected category and damage type");
                character.Options.Books.Clear();
                check(sourceAware.Accept(false) == null, "accepting a weapon rechecks source availability");
                sourceAware.Refresh();
                check(sourceAware.IsEmpty && sourceAware.Details == null, "source changes clear unavailable weapon choices without restart");
            }
            finally { character.Options.Books.Clear(); character.Options.Books.AddRange(sources); }
        }
        finally { WeaponPickerModel.LastCategory = previousCategory; }
    }
}
