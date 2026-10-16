#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Xml;
namespace Chummer;

internal static class ArmorModPickerChecks
{
    internal static void Run(Character character, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot(); var books = character.Options.Books.ToArray();
        try
        {
            if (!character.Options.Books.Contains("AR")) character.Options.Books.Add("AR");
            var model = new ArmorModPickerModel(character, true, "Clothing", 5000, CapacityStyle.Standard) { SearchText = "Fire Resistance" };
            model.Rating = 4;
            check(model.CanAccept && model.MaximumRating == 6 && model.Details!.Capacity == "[2]" && model.Details.Cost == string.Format("{0:###,###,##0¥}", 400),
                "rated armor modification retains fixed-value capacity and rating-based cost");
            model.Markup = 25;
            check(model.Details!.Cost == string.Format("{0:###,###,##0¥}", 500) && model.Accept(true)!.Rating == 4,
                "armor modification selection carries rating and purchase options");
            model.Rating = 99; check(model.Rating == 6, "native rating model enforces the catalog upper limit");
            model.Rating = -1; check(model.Rating == 1, "native rating model enforces the catalog lower limit");
            model.SearchText = "Auto-Injector";
            check(model.MaximumRating == 1 && model.Rating == 1 && !model.IsRatingEnabled, "fixed armor modifications disable rating changes");
            model.SearchText = "no \"quoted\" armor mod";
            check(model.IsEmpty && model.Details == null && !model.IsRatingEnabled && model.Accept(false) == null, "empty armor modification search clears rating/details and acceptance");
            foreach (var style in new[] { CapacityStyle.Zero, CapacityStyle.PerRating })
            {
                var capacity = new ArmorModPickerModel(character, false, "", 5000, style) { SearchText = "Fire Resistance", Rating = 4 };
                check(capacity.Details!.Capacity == (style == CapacityStyle.Zero ? "[0]" : "[4]"), "armor modification retains capacity style " + style);
            }
            model.SearchText = "Fire Resistance"; model.FreeCost = true;
            check(model.Details!.Cost == string.Format("{0:###,###,##0¥}", 0), "free armor modification clears the preview price");
            character.Options.Books.Clear(); check(model.Accept(false) == null, "armor modification acceptance rejects disabled sources");
            var catalog = XmlManager.Instance.Load("armor.xml");
            foreach (string book in catalog.SelectNodes("/chummer/mods/mod/source")!.Cast<XmlNode>().Select(node => node.InnerText).Distinct()) character.Options.Books.Add(book);
            var environmental = new ArmorModPickerModel(character, true, "Environmental Armor", 5000, CapacityStyle.Standard)
                { SearchText = "Transparent Ruthenium Polymer Coating" };
            using (var adapter = new frmSelectArmorMod(character))
            {
                typeof(frmSelectArmorMod).GetField("selection", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(adapter, environmental.Accept(false));
                check(adapter.SelectedDefinition["category"]!.InnerText == "Environmental Armor" && adapter.SelectedDefinition["cost"]!.InnerText == "200",
                    "same-name armor modifications retain the selected category and purchase definition");
            }
            var all = new ArmorModPickerModel(character, false, string.Join(',', catalog.SelectNodes("/chummer/mods/mod/category")!.Cast<XmlNode>().Select(node => node.InnerText).Distinct()), 5000, CapacityStyle.Standard);
            foreach (var entry in all.Items)
            {
                all.SelectedEntry = entry; all.Rating = all.MaximumRating;
                if (!all.CanAccept) throw new InvalidOperationException("Armor modification preview failed.");
            }
            check(all.Items.Count == catalog.SelectNodes("/chummer/mods/mod")!.Count, "all armor modification catalog definitions preview at their maximum rating");
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(books); }
        check(character.CaptureSnapshot().SequenceEqual(original), "armor modification browsing preserves character state");
    }
}
