#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml;

namespace Chummer;

internal static class CyberwareModelChecks
{
    private static Dictionary<string, T> Reference<T>(string resource)
    {
        using var stream = typeof(CyberwareModelChecks).Assembly.GetManifestResourceStream(resource)!;
        return JsonSerializer.Deserialize<Dictionary<string, T>>(stream)!;
    }
    internal static void Run(Character original, Action<bool, string> check)
    {
        Character Create()
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot());
            character.Options.Books.Clear(); character.Options.Books.AddRange(new[] { "SR4", "AU" });
            return character;
        }
        var choices = Reference<string>("Chummer.CyberwareLegacyCatalog.json");
        foreach (bool bio in new[] { false, true })
        foreach (bool modular in new[] { false, true })
        foreach (bool subsystems in new[] { false, true })
        {
            var character = Create(); byte[] before = character.CaptureSnapshot();
            var model = new CyberwarePickerModel(character, false, new(Bioware: bio, InitialGrade: "Standard",
                Context: new(modular, false, subsystems, "No matching category")));
            string context = (bio ? "bioware" : "cyberware") + "/modular=" + modular + "/subsystems=" + subsystems;
            foreach (var category in model.Categories)
            {
                model.Category = category;
                check(GearPickerReference.Digest(model.Items.Select(item => item.Name)) == choices[context + "/" + category.Code],
                    "Cyberware model matches frozen catalog choices: " + context + "/" + category.Code);
            }
            check(character.CaptureSnapshot().SequenceEqual(before), "Cyberware catalog model browsing preserves complete snapshot: " + context);
        }
        var previews = Reference<CyberwarePreviewCase>("Chummer.CyberwareLegacyPreview.json");
        foreach (var pair in previews)
        {
            var reference = pair.Value; var request = reference.Request; var character = Create();
            character.Options.AllowCyberwareESSDiscounts = request.DiscountEnabled;
            character.Options.AllowCustomTransgenics = true;
            character.Options.EssenceDecimals = reference.EssenceDecimals;
            foreach (var improvement in character.Improvements.Where(item => item.ImproveType == Improvement.ImprovementType.SensitiveSystem).ToArray()) character.Improvements.Remove(improvement);
            for (int index = 0; index < reference.SensitiveSystemCount; index++) character.Improvements.Add(new Improvement { ImproveType = Improvement.ImprovementType.SensitiveSystem, Enabled = true });
            var catalog = (XmlDocument)XmlManager.Instance.Load(request.Bioware ? "bioware.xml" : "cyberware.xml").CloneNode(true);
            var definition = new XmlDocument(); definition.LoadXml(reference.DefinitionXml);
            var node = definition.DocumentElement!; string name = node["name"]!.InnerText, category = node["category"]!.InnerText;
            string kind = request.Bioware ? "bioware" : "cyberware";
            foreach (XmlNode existing in catalog.SelectNodes("/chummer/" + kind + "s/" + kind)!.Cast<XmlNode>().Where(item => item["name"]?.InnerText == name && item["category"]?.InnerText == category).ToArray()) existing.ParentNode!.RemoveChild(existing);
            catalog.SelectSingleNode("/chummer/" + kind + "s")!.AppendChild(catalog.ImportNode(node, true));
            var grade = (request.Bioware ? GlobalOptions.BiowareGrades : GlobalOptions.CyberwareGrades).First(item =>
                item.Cost == request.GradeCostMultiplier && Convert.ToDouble(item.Essence) == request.GradeEssenceMultiplier && item.Avail == request.GradeAvailabilityModifier);
            byte[] before = character.CaptureSnapshot();
            var model = new CyberwarePickerModel(character, true, new(Bioware: request.Bioware, Context: new(AllowModularPlugins: true),
                InitialCategory: category, InitialGrade: grade.Name, CharacterEssenceMultiplier: request.CharacterEssenceMultiplier,
                BasicBiowareEssenceMultiplier: request.BasicBiowareEssenceMultiplier, GenetechCostMultiplier: request.GenetechCostMultiplier,
                TransgenicCostMultiplier: request.TransgenicCostMultiplier), catalog);
            model.SelectedEntry = model.Items.Single(item => item.Name == name && item.Category == category);
            model.SelectedGrade = model.Grades.First(item => item.Name == grade.Name);
            model.Transgenic = request.Transgenic; model.EssenceDiscount = Convert.ToInt32(request.EssenceDiscount);
            model.Rating = Convert.ToInt32(request.Rating); model.FreeCost = request.FreeCost;
            var details = model.Details;
            check(details != null && new CyberwarePreviewResult(details.Availability, details.Cost, details.AvailabilityTest, details.Essence, details.Capacity) == reference.Result &&
                character.CaptureSnapshot().SequenceEqual(before), "Cyberware model matches frozen independent preview and snapshot: " + pair.Key);
        }

        var ordinary = Create(); ordinary.Options.AllowCyberwareESSDiscounts = true;
        byte[] ordinaryBefore = ordinary.CaptureSnapshot();
        var creation = new CyberwarePickerModel(ordinary, false, new(InitialCategory: "Headware", InitialGrade: "Standard", MaximumCapacity: 0));
        creation.SelectedEntry = creation.Items.Single(item => item.Name == "Datajack"); creation.FreeCost = true;
        check(!creation.IsFreeCostVisible && !creation.FreeCost, "Cyberware creation rejects a hidden Free draft");
        creation.EssenceDiscount = 999;
        check(creation.EssenceDiscount == 100, "Cyberware model clips discount at established positive limit");
        creation.EssenceDiscount = -999;
        check(creation.EssenceDiscount == -100, "Cyberware model clips discount at established negative limit");
        creation.EssenceDiscount = 10;
        var accepted = creation.AcceptCyberware(true);
        check(accepted is { Item.Name: "Datajack", Item.AddAgain: true, Grade.Name: "Standard", EssenceDiscount: 10 } && creation.MaximumCapacityText.Length > 0,
            "Cyberware accepted canonical draft preserves discount AddAgain and legacy display-only capacity hint");
        var locked = new CyberwarePickerModel(ordinary, false, new(InitialCategory: "Headware", InitialGrade: "Alphaware", LockGrade: true));
        locked.SelectedEntry = locked.Items.Single(item => item.Name == "Datajack"); locked.SelectedGrade = locked.Grades.First(item => item.Name == "Standard");
        check(!locked.IsGradeEnabled && locked.SelectedGrade?.Name == "Alphaware", "Cyberware model preserves caller lock against grade edits");
        locked.Category = locked.Categories.Single(item => item.Code == "Bodyware"); locked.SelectedEntry = locked.Items.Single(item => item.Name == "Wired Reflexes");
        locked.Rating = 999;
        check(locked.Rating == 3 && locked.MinimumRating == 1 && locked.MaximumRating == 3, "Cyberware model preserves rated bounds");
        locked.Category = locked.Categories.Single(item => item.Code == "Headware"); locked.SelectedEntry = locked.Items.Single(item => item.Name == "Datajack");
        check(locked.Rating == 0 && !locked.IsRatingEnabled, "Cyberware model clears rating on unrated transition");
        check(ordinary.CaptureSnapshot().SequenceEqual(ordinaryBefore), "Cyberware accepted drafts and constrained edits preserve complete character snapshot");

        var biology = Create(); biology.Options.AllowCustomTransgenics = true;
        var bioModel = new CyberwarePickerModel(biology, false, new(Bioware: true, InitialCategory: "Basic", InitialGrade: "Standard", TransgenicCostMultiplier: 0.8));
        bioModel.SelectedGrade = bioModel.Grades.First(item => item.SecondHand); bioModel.SearchText = "Cerebral Booster";
        check(bioModel.SelectedEntry?.Category == "Cultured" && !bioModel.Grades.Any(item => item.SecondHand) && bioModel.SelectedGrade?.Name == "Standard",
            "Cyberware model applies Cultured grade constraints to canonical search results");
        bioModel.SearchText = ""; bioModel.Category = bioModel.Categories.Single(item => item.Code == "Basic");
        bioModel.SelectedEntry = bioModel.Items.Single(item => item.Name == "Muscle Toner");
        bioModel.Transgenic = true; bioModel.FreeCost = true;
        check(!bioModel.IsGradeEnabled && bioModel.SelectedGrade?.Name == "Standard" && bioModel.IsFreeCostVisible && bioModel.AcceptCyberware(false)?.Transgenic == true,
            "Bioware transgenic draft forces Standard grade and retains accepted flag");
        bioModel.Transgenic = false;
        check(bioModel.IsGradeEnabled && !bioModel.IsFreeCostVisible && !bioModel.FreeCost, "Bioware leaving transgenic clears hidden Free without recursive detail updates");

        var changing = Create(); var adapsin = new Improvement { ImproveType = Improvement.ImprovementType.Adapsin, Enabled = true }; changing.Improvements.Add(adapsin);
        var changingModel = new CyberwarePickerModel(changing, false, new(InitialCategory: "Headware", InitialGrade: "Alphaware (Adapsin)"));
        changingModel.SelectedEntry = changingModel.Items.Single(item => item.Name == "Datajack"); adapsin.Enabled = false;
        check(changingModel.AcceptCyberware(false) == null && changingModel.HasError, "Cyberware acceptance rejects a grade invalidated after selection");
        changingModel.SelectedEntry = changingModel.Items.First(item => item.Name == "Control Rig"); changing.Options.Books.Clear();
        check(changingModel.AcceptCyberware(false) == null && changingModel.Items.Count == 0, "Cyberware acceptance rejects a source disabled after selection");

        var customCharacter = Create(); byte[] customBefore = customCharacter.CaptureSnapshot();
        var custom = (XmlDocument)XmlManager.Instance.Load("cyberware.xml").CloneNode(true);
        const string customCategory = "Synthetic \"Group's\"", customName = "Synthetic \"Ware's\" Node";
        var categoryNode = custom.CreateElement("category"); categoryNode.InnerText = customCategory;
        custom.SelectSingleNode("/chummer/categories")!.AppendChild(categoryNode);
        var ware = custom.CreateElement("cyberware");
        ware.InnerXml = "<name/><category/><avail>0</avail><cost>100</cost><ess>0.1</ess><capacity>[1]</capacity><source>SR4</source><page>1</page>";
        ware["name"]!.InnerText = customName; ware["category"]!.InnerText = customCategory;
        custom.SelectSingleNode("/chummer/cyberwares")!.AppendChild(ware);
        var customModel = new CyberwarePickerModel(customCharacter, false, new(InitialCategory: customCategory, InitialGrade: "Standard"), custom);
        customModel.SearchText = "\"Ware's\"";
        var customDraft = customModel.AcceptCyberware(false);
        check(customModel.Items.Count == 1 && customDraft?.Item.Name == customName && customDraft.Item.Category == customCategory,
            "Cyberware model preserves quoted custom category and item identity through search and acceptance");
        var forced = custom.CreateElement("forcegrade"); forced.InnerText = "Betaware"; ware.AppendChild(forced); customModel.Refresh();
        customModel.SelectedGrade = customModel.Grades.First(item => item.Name == "Standard");
        check(customModel.SelectedGrade?.Name == "Betaware" && !customModel.IsGradeEnabled,
            "Cyberware model enforces an item forcegrade against draft edits");
        forced.InnerText = "No such grade"; customModel.Refresh();
        check(!customModel.CanAccept && customModel.HasError && customModel.AcceptCyberware(false) == null,
            "Cyberware model rejects unavailable forced grade instead of substituting a purchase");
        ware.RemoveChild(forced); ware["ess"]!.InnerText = "Rating +"; customModel.Refresh();
        check(customModel.Details == null && customModel.HasError && !customModel.CanAccept,
            "Cyberware model clears stale preview and blocks purchase for a broken essence expression");
        ware["ess"]!.InnerText = "0.1"; customModel.Refresh();
        check(customModel.CanAccept && !customModel.HasError && customModel.Details != null,
            "Cyberware model recovers after correcting the invalid definition");
        check(customCharacter.CaptureSnapshot().SequenceEqual(customBefore), "Cyberware custom and invalid draft transitions preserve complete character snapshot");
        var pluginModel = new CyberwarePickerModel(customCharacter, false, new(Context: new(PluginSelection: true, SubsystemsOnly: true, Subsystems: " Headware , Headware "), InitialGrade: "Standard"));
        pluginModel.SearchText = "Control Rig";
        check(pluginModel.Items.Count == 0 && !pluginModel.CanAccept, "Cyberware model search cannot restore ineligible nonconsuming plugin");
        pluginModel.SearchText = "Datajack";
        check(pluginModel.Items.Count == 1 && pluginModel.SelectedEntry?.Name == "Datajack", "Cyberware model search recovers an eligible consuming plugin");
    }
}
