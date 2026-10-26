#nullable enable
using System;
using System.Linq;
using System.Xml;

namespace Chummer;

internal static class GearPreviewChecks
{
    internal static void Run(Character original, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot());
        if (!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
        byte[] snapshot = character.CaptureSnapshot();
        var catalog = (XmlDocument)XmlManager.Instance.Load("gear.xml").CloneNode(true);
        var model = new GearPickerModel(character, true, new(InitialCategory: "Biotech", AvailabilityModifier: 2, CostMultiplier: 3), catalog);
        var reference = GearPickerReference.Previews();
        void Compare(string label, CapacityStyle style = CapacityStyle.Standard, bool armor = false)
        {
            var result = GearPreview.Calculate(character, catalog, model.SelectedEntry!.Definition, new GearPreviewRequest(
                model.Rating, model.Quantity, model.QuantityIncrement, model.Markup, model.DoItYourself,
                model.Hacked, model.FreeCost, 2, 3, armor, style));
            check(result == reference[label], "gear extracted preview matches frozen legacy result for " + label);
        }
        void Select(string category, string name)
        {
            model.Category = model.Categories.Single(item => item.Code == category);
            model.SelectedEntry = model.Items.Single(item => item.Name == name);
            check(model.SelectedEntry.Name == name, "gear preview reference selects " + name);
        }
        Select("Biotech", "Medkit"); model.Rating = 4; model.Quantity = 3; model.Markup = 15;
        Compare("rated quantity and markup"); Compare("armor capacity", armor: true);
        Compare("zero capacity", CapacityStyle.Zero); Compare("per-rating capacity", CapacityStyle.PerRating);
        Select("Ammunition", "Ammo: Regular Ammo"); model.Quantity = 30;
        Compare("pack quantity"); Compare("unrated per-rating capacity", CapacityStyle.PerRating);
        Select("Commlink Upgrade", "Response 4"); model.DoItYourself = true;
        Compare("DIY and caller modifiers"); model.FreeCost = true; Compare("free price"); model.FreeCost = false;
        Select("Matrix Programs", "Analyze");
        foreach (int rating in new[] { 1, 3, 4, 6, 7, 10 }) { model.Rating = rating; Compare("software rating tier " + rating); }
        model.Hacked = true; Compare("hacked software"); model.Hacked = false;
        foreach (var item in new[] {
            ("Variable(100-300)", "[Rating]", "variable range and bracket capacity"),
            ("Variable(100+)", "Rating * 0.5", "open variable range and fractional capacity"),
            ("Rating * 101.5", "Rating/[2]", "fractional price and split capacity"),
            ("100", "FixedValues(1,2,3,4,5,6)", "legacy fixed-value capacity fallback"),
            ("100", "[*]", "legacy wildcard capacity fallback") })
        {
            var definition = catalog.CreateElement("gear");
            definition.InnerXml = "<name>Synthetic Preview</name><category>Biotech</category><rating>6</rating><avail>+RatingR</avail><cost>" + item.Item1 +
                "</cost><capacity>" + item.Item2 + "</capacity><source>SR4</source><page>337</page>";
            catalog.SelectSingleNode("/chummer/gears")!.AppendChild(definition);
            model.Category = model.Categories.Single(category => category.Code == "Ammunition");
            Select("Biotech", "Synthetic Preview"); model.Rating = 3; Compare(item.Item3);
            catalog.SelectSingleNode("/chummer/gears")!.RemoveChild(definition);
        }
        check(character.CaptureSnapshot().SequenceEqual(snapshot), "gear preview reference checks preserve complete character snapshot");
    }
}
