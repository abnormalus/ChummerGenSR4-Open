using System;
using System.Linq;
using System.Xml;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Preserve the characterized selection contract after retiring the legacy controls.
internal static class QualitySelectionChecks
{
    internal static void Run(Character source, Action<bool, string> check)
    {
        var eligibilityCharacter = new Character();
        eligibilityCharacter.LoadSnapshot(source.CaptureSnapshot());
        eligibilityCharacter.IgnoreRules = false;
        const string existingName = "Synthetic eligibility fixture";
        eligibilityCharacter.Qualities.Add(new Quality(eligibilityCharacter) { Name = existingName });
        byte[] eligibilityBefore = eligibilityCharacter.CaptureSnapshot();
        var catalog = XmlManager.Instance.Load("qualities.xml");
        var metatypes = XmlManager.Instance.Load("metatypes.xml");
        var critters = XmlManager.Instance.Load("critters.xml");
        var policy = new QualityEligibilityPolicy(eligibilityCharacter, catalog, metatypes, critters, "");
        XmlNode Definition(string xml) { var document = new XmlDocument(); document.LoadXml(xml); return document.DocumentElement; }
        var duplicate = Definition("<quality><name>Synthetic eligibility fixture</name></quality>");
        check(!policy.RequirementMet(duplicate, false), "Quality policy rejects an existing limited quality");
        check(policy.RequirementMet(Definition("<quality><name>Synthetic eligibility fixture</name><limit>no</limit></quality>"), false),
            "Quality policy preserves explicitly repeatable qualities");
        var forbidden = Definition("<quality><name>Synthetic candidate</name><forbidden><oneof><quality>Synthetic eligibility fixture</quality></oneof></forbidden></quality>");
        check(!policy.RequirementMet(forbidden, false), "Quality policy rejects a forbidden existing quality");
        var swapPolicy = new QualityEligibilityPolicy(eligibilityCharacter, catalog, metatypes, critters, existingName);
        check(swapPolicy.RequirementMet(forbidden, false), "Quality swap ignores only the requested forbidden quality");
        check(!swapPolicy.RequirementMet(duplicate, false), "Quality swap exemption does not bypass duplicate limits");
        check(eligibilityCharacter.CaptureSnapshot().SequenceEqual(eligibilityBefore),
            "Quality eligibility evaluation preserves the complete character snapshot");
        eligibilityCharacter.IgnoreRules = true;
        check(policy.RequirementMet(duplicate, false) && policy.RequirementMet(forbidden, false),
            "Quality policy retains the explicit Ignore Rules override");

        var restricted = new Character(); restricted.LoadSnapshot(source.CaptureSnapshot());
        restricted.IgnoreRules = true; restricted.Metatype = "A.I.";
        var restrictedModel = new QualityPickerModel(restricted);
        var ai = metatypes.SelectSingleNode("/chummer/metatypes/metatype[name='A.I.']");
        foreach (var category in restrictedModel.Categories)
        {
            restrictedModel.Category = category;
            var allowed = ai.SelectNodes("qualityrestriction/" + category.Code.ToLowerInvariant() + "/quality")
                .Cast<XmlNode>().Select(node => node.InnerText).ToArray();
            check(restrictedModel.Items.All(item => allowed.Contains(item.Name)),
                "Quality restricted-metatype choices retain their catalog whitelist " + category.Code);
        }
        var filtered = new Character(); filtered.LoadSnapshot(source.CaptureSnapshot()); filtered.IgnoreRules = true;
        var forced = new QualityPickerModel(filtered, "Negative"); forced.SearchText = "a";
        check(forced.Items.Count > 0 && forced.Items.All(item => item.Category == "Negative"),
            "Quality forced category remains effective during cross-category search");
        forced.Category = forced.Categories.Single(category => category.Code == "Positive");
        check(forced.Items.Count == 0, "Quality forced category cannot accept a conflicting category");
        var sources = filtered.Options.Books.ToArray();
        try
        {
            var changingSources = new QualityPickerModel(filtered);
            check(changingSources.SelectedEntry != null, "Quality source-change fixture starts with a selected quality");
            filtered.Options.Books.Clear();
            check(changingSources.Accept(false) == null && changingSources.Items.Count == 0,
                "Quality acceptance rejects a source disabled while the selector is open");
        }
        finally { filtered.Options.Books.Clear(); filtered.Options.Books.AddRange(sources); }

        foreach (int context in new[] { 0, 1, 2 })
        foreach (bool addAgain in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot());
            character.IgnoreRules = true;
            character.BuildMethod = context == 1 ? CharacterBuildMethod.Karma : CharacterBuildMethod.BP;
            character.Created = context == 2;
            byte[] before = character.CaptureSnapshot();
            var model = new QualityPickerModel(character);
            foreach (var category in model.Categories)
            {
                model.Category = category;
                check(model.Items.Count > 0 && model.Items.All(item => item.Category == category.Code),
                    "Quality choices preserve category isolation context=" + context);
                check(model.Items.All(item => item.Definition.SelectSingleNode("self::quality[(" + character.Options.BookXPath() + ")]") != null),
                    "Quality category choices respect enabled source books context=" + context);
            }
            model.SearchText = "a";
            check(model.Items.Count > 0 && model.Items.Select(item => item.Category).Distinct().Count() > 1,
                "Quality search finds choices across categories");
            model.MetageneticOnly = true;
            check(model.Items.Count > 0 && model.Items.All(item => item.Definition.SelectSingleNode(
                "required/oneof[contains(., 'Changeling (Class I SURGE)')] | metagenetic[.='yes']") != null),
                "Quality metagenetic filter retains only matching definitions");
            model.MetageneticOnly = false;
            model.SearchText = "\" [O'Brien]";
            check(model.Items.Count == 0 && !model.CanAccept, "Quality literal quoted search remains safe and rejects empty acceptance");
            model.SearchText = "a";
            var entry = model.SelectedEntry;
            int expected = Convert.ToInt32(entry.Definition["bp"].InnerText) * (context == 0 ? 1 : character.Options.KarmaQuality);
            check(model.Details.Cost == expected.ToString(), "Quality preview preserves BP/Karma conversion");
            model.FreeCost = true;
            check(model.FreeCost && model.Details.Cost == "0", "Quality Free remains available in creation and career");
            var selection = model.Accept(addAgain);
            check(selection != null && selection.Name == entry.Name && selection.FreeCost && selection.AddAgain == addAgain,
                "Quality acceptance preserves canonical identity, Free and Add Again intent");
            check(character.CaptureSnapshot().SequenceEqual(before), "Quality selection leaves all character accounting to the caller");
            using var cancelled = new frmSelectQuality(character);
            check(cancelled.DialogResult == Forms.DialogResult.Cancel && cancelled.SelectedQuality == "" && !cancelled.AddAgain && !cancelled.FreeCost,
                "Quality unopened wrapper defaults to cancellation with no selected purchase");
        }
    }
}
