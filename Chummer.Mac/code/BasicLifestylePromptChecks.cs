#nullable enable
using System;
using System.Linq;
using System.Xml;
namespace Chummer;

// Keep the contracts characterized against the former form, including its120
// catalog price cases. Calculation and accepted mutation are independent of UI.
internal static class BasicLifestylePromptChecks
{
    internal static void Run(Character character, Action<bool, string> check)
    {
        var books = character.Options.Books.ToArray(); var before = character.CaptureSnapshot();
        try
        {
            character.Options.Books.Clear(); character.Options.Books.AddRange(new[] { "SR4", "UN", "AU" });
            var model = new BasicLifestylePickerModel(character);
            check(model.Roommates == 0 && model.Percentage == 100 && model.SelectedEntry?.Name == "Street", "basic lifestyle defaults preserve zero roommates and full percentage");
            int definitions = 0, comparisons = 0;
            foreach (XmlNode definition in XmlManager.Instance.Load("lifestyles.xml").SelectNodes("/chummer/lifestyles/lifestyle")!)
            {
                model.SelectedEntry = model.Items.Single(item => item.Name == definition["name"]!.InnerText);
                foreach (int share in new[] { 0, 33, 50, 100, 900 })
                    foreach (int people in new[] { 0, 1, 10 })
                    {
                        model.Percentage = share; model.Roommates = people;
                        int expected = Convert.ToInt32(Convert.ToDouble(definition["cost"]!.InnerText, GlobalOptions.Instance.CultureInfo) *
                            (1 + Convert.ToDouble((decimal)people / 10)) * Convert.ToDouble((decimal)share / 100));
                        if (model.Details!.Cost != string.Format("{0:###,###,##0¥}", expected)) throw new InvalidOperationException("Basic lifestyle characterization differs.");
                        comparisons++;
                    }
                definitions++;
            }
            check(definitions == 8 && comparisons == 120, "all eight basic lifestyle prices preserve roommate and percentage preview rounding");
            model.SelectedEntry = model.Items.Single(item => item.Name == "Low"); model.Roommates = 2; model.Percentage = 33;
            var target = new Lifestyle(character); var accepted = model.Accept(true)!; model.PrepareAccepted(accepted).ApplyTo(target);
            check(accepted.AddAgain && target.Name == "Low" && target.Cost == 2000 && target.Roommates == 2 && target.Percentage == 33,
                "accepted basic lifestyle keeps base cost separate from roommates percentage and Add More");
            var namedModel = new BasicLifestylePickerModel(character, null, new Lifestyle(character) { LifestyleName = "Synthetic new residence", Notes = "Synthetic new notes", Purchased = true }); var namedTarget = new Lifestyle(character);
            namedModel.PrepareAccepted(namedModel.Accept(false)!).ApplyTo(namedTarget);
            check(namedTarget.Name == "Street" && namedTarget.LifestyleName == "Synthetic new residence" && namedTarget.Notes == "Synthetic new notes" && namedTarget.Purchased, "new basic lifestyle keeps its supplied residence name and legacy opening choice");
            var source = new Lifestyle(character) { Name = "Low", LifestyleName = "Synthetic residence", Cost = 2000, Roommates = 3, Percentage = 25, Months = 4, Notes = "Synthetic residence notes", Purchased = true };
            var editing = new BasicLifestylePickerModel(character, source);
            check(editing.IsEditing && editing.Items.Count == 1 && editing.SelectedEntry!.Name == "Low" && editing.Roommates == 3 && editing.Percentage == 25,
                "basic lifestyle edit locks identity and preloads financial drafts");
            editing.Percentage = 50;
            check(source.Percentage == 25 && source.Roommates == 3 && source.Months == 4, "unaccepted basic lifestyle edit never mutates the source");
            var replacement = new Lifestyle(character); editing.PrepareAccepted(editing.Accept(false)!).ApplyTo(replacement);
            check(replacement.LifestyleName == "Synthetic residence" && replacement.Percentage == 50 && source.Percentage == 25 && replacement.Notes == "Synthetic residence notes" && replacement.Purchased,
                "accepted basic lifestyle replacement retains residence name notes and purchased status without mutating its source");
            model.Percentage = null;
            check(!model.CanAccept && model.Details!.Cost == "" && model.Accept(false) == null, "basic lifestyle cleared percentage blocks acceptance and clears price preview");
            model.Percentage = 0; model.Roommates = null;
            check(!model.CanAccept && model.Accept(false) == null, "basic lifestyle cleared roommate count blocks acceptance");
            model.Roommates = 0;
            check(model.CanAccept && model.Details!.Cost == string.Format("{0:###,###,##0¥}", 0), "basic lifestyle accepts an explicit zero share");
            character.Options.Books.Clear();
            check(editing.Accept(false) == null && source.Name == "Low" && source.Percentage == 25, "disabled lifestyle source rejects replacement without substituting another identity");
            character.Options.Books.Add("SR4");
            var custom = new BasicLifestylePickerModel(character, new Lifestyle(character) { Name = "Synthetic custom identity", LifestyleName = "Retained custom residence" });
            check(custom.IsEmpty && !custom.CanAccept, "unknown basic lifestyle identity cannot silently become a different catalog entry");
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(books); }
        check(character.CaptureSnapshot().AsSpan().SequenceEqual(before), "basic lifestyle preparation never changes owned character data");
    }
}
