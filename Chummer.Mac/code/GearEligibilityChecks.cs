#nullable enable
using System;
using System.Linq;

namespace Chummer;

internal static class GearEligibilityChecks
{
    internal static void Run(Character character, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot(); var books = character.Options.Books.ToArray();
        try
        {
            if (!character.Options.Books.Contains("AR")) character.Options.Books.Add("AR");
            var categories = new GearPickerModel(character, false, new("Commlink"));
            string[] choices = categories.Items.Select(item => item.Name).OrderBy(x => x).ToArray();
            check(choices.Length > 0, "commlink category provides an eligible search baseline");
            foreach (string query in new[] { "commlink", "comlink", "COMLINKS", "commlinks" })
            {
                categories.SearchText = query;
                check(choices.SequenceEqual(categories.Items.Select(item => item.Name).OrderBy(x => x)), "gear category search includes eligible commlinks for " + query);
            }
            categories.SearchText = "comlink ' or true()"; check(categories.Items.Count == 0, "category search treats quotes and operators literally");
            var sample = new System.Xml.XmlDocument(); sample.LoadXml("<gear><name>Model</name><translate>Modèle</translate><category>Commlink</category></gear>");
            check(CatalogSearch.MatchesNameOrCategory(sample.DocumentElement!, "liaison", "Appareils de liaison") && CatalogSearch.MatchesNameOrCategory(sample.DocumentElement!, "Modèle", "Appareils de liaison"),
                "catalog category search retains localized category and item names");
            void Compare(GearPickerOptions options, string excluded, string included, string label)
            {
                var model = new GearPickerModel(character, false, options);
                check(!model.Items.Any(item => item.Name == excluded) && model.Items.Any(item => item.Name == included), label + " browsing retains established eligibility");
                model.SearchText = model.Category?.Code ?? "";
                check(!model.Items.Any(item => item.Name == excluded), label + " category search retains contextual restrictions");
                model.SearchText = excluded; check(!model.Items.Any(item => item.Name == excluded), label + " search cannot restore an ineligible choice");
                model.SearchText = included; check(model.Items.Any(item => item.Name == included), label + " search retains eligible choices");
            }
            var hardware = new GearPickerOptions("Commlink Upgrade", Context: new(false,false,false,5,5,0,0));
            Compare(hardware,"Response 1","Response 4","commlink lower upgrade bound");
            Compare(hardware,"Response 6","Response 5","commlink upper upgrade bound");
            Compare(new("Commlink Operating System Upgrade",Context:new(false,false,false,0,0,3,3)),"Firewall 1","Firewall 4","operating-system upgrade");
            Compare(new("Electronics and Sensor Options",Context:new(true,false,false,0,0,0,0)),"Self-Destruct, Area Effect","Skinlink","positive capacity");
            Compare(new("Electronics and Sensor Options",Context:new(false,true,false,0,0,0,0)),"Skinlink","Self-Destruct, Area Effect","installed capacity");
            Compare(new("Electronics and Sensor Options",Context:new(false,false,true,0,0,0,0)),"Self-Destruct, Area Effect","Skinlink","armor capacity");
            var empty = new GearPickerModel(character,false,new("Commlink Operating System Upgrade",Context:new(false,false,false,0,0,10,10)));
            check(empty.Categories.Count == 0 && !empty.CanAccept, "gear with no eligible categories opens safely with purchasing disabled");
            using var custom = new frmSelectGear(character) { AllowedCategories="Commlink Upgrade", CommlinkResponse=0, CommlinkSignal=0 };
            var customModel = custom.CreateModel(); customModel.SearchText = "Response 8";
            check(customModel.Items.Count > 0, "custom commlinks retain the established unrestricted hardware-upgrade exception");
            var changed = new GearPickerModel(character,false,hardware); changed.SelectedEntry=changed.Items.Single(item=>item.Name=="Response 4");
            changed.SelectionContext=new(false,false,false,7,7,0,0);
            check(changed.AcceptGear(false)==null, "gear confirmation rejects changed context without buying a replacement");
            changed=new GearPickerModel(character,false,hardware); changed.SelectedEntry=changed.Items.Single(item=>item.Name=="Response 4");
            character.Options.Books.Clear();
            check(changed.AcceptGear(false)==null && changed.Items.Count==0, "gear confirmation rejects a source disabled after selection");
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(books); }
        check(character.CaptureSnapshot().SequenceEqual(original), "gear eligibility checks preserve the source character and settings");
    }
}
