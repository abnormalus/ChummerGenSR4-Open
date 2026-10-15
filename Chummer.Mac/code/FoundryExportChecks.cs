#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Nodes;
using Majorsilence.Forms;

namespace Chummer;

internal static class FoundryExportChecks
{
    internal static void Run(string output, Action<bool, string> check)
    {
        var character = new Character { FileName = Path.Combine(output, "career.chum") };
        check(character.Load(), "Foundry fixture loads from a disposable character copy");
        character.Alias = "Foundry Export Regression";
        character.Age = "unknown";
        character.REA.Value = 3;
        character.INT.Value = 3;
        character.GetAttribute("INI").MetatypeAugmentedMaximum = 30;
        var grades = new GradeList(); grades.LoadList(Improvement.ImprovementSource.Cyberware);
        Cyberware AddWare(string name, int rating)
        {
            var definition = XmlManager.Instance.Load("cyberware.xml").SelectSingleNode("/chummer/cyberwares/cyberware[name='" + name + "']");
            var ware = new Cyberware(character);
            ware.Create(definition, character, grades.GetGrade("Alphaware"), Improvement.ImprovementSource.Cyberware, rating,
                new TreeNode(), new List<Weapon>(), new List<TreeNode>());
            character.Cyberware.Add(ware); return ware;
        }
        var wired = AddWare("Wired Reflexes", 2);
        wired.Notes = "User notes must survive exactly: <custom> & café.";
        var enabledBefore = character.Improvements.Select(i => i.Enabled).ToArray();
        int reaction = character.REA.TotalValue;
        int passes = FoundryActorExporter.Effective(character.InitiativePasses);
        var actor = FoundryActorExporter.Export(character);
        JsonObject Find(JsonObject root, string name) => root["items"]!.AsArray().OfType<JsonObject>().Single(i => i["name"]!.GetValue<string>().StartsWith(name, StringComparison.Ordinal));
        var wiredItem = Find(actor, "Wired Reflexes");
        var effects = wiredItem["effects"]!.AsArray();
        check(effects.Count == 1 && effects[0]!["disabled"]!.GetValue<bool>() == false, "switchable ware exports an enabled activation effect");
        int Change(string key) => effects[0]!["changes"]!.AsArray().Single(n => n!["key"]!.GetValue<string>() == key)!["value"]!.GetValue<string>() is var value ? int.Parse(value) : 0;
        check(actor["system"]!["sheetStats"]!["REACTION"]!.GetValue<int>() + Change("system.sheetStats.REACTION") == reaction &&
            actor["system"]!["modifiers"]!["initiative"]!["passes"]!["physical"]!.GetValue<int>() + Change("system.modifiers.initiative.passes.physical") == passes - 1,
            "activation effects reconstruct augmented Reaction and passes without double counting");
        check(wiredItem["system"]!["notes"]!.GetValue<string>() == wired.Notes &&
            character.Improvements.Select(i => i.Enabled).SequenceEqual(enabledBefore) && character.REA.TotalValue == reaction,
            "fully supported ware preserves notes and export restores live improvement state");
        check(actor["system"]!["metaData"]!["age"]!.GetValue<int>() == 0 && actor["system"]!["descriptionAndNotes"]!["notes"]!.GetValue<string>().Contains("Original age: unknown"),
            "Foundry age is numeric while nonnumeric age is retained in notes");
        var ids = actor["items"]!.AsArray().Select(i => i!["_id"]!.GetValue<string>()).ToArray();
        check(ids.Distinct().Count() == ids.Length && ids.All(id => id.Length == 16 && id.All(char.IsLetterOrDigit)), "Foundry embedded document IDs are unique and valid");
        check(actor["items"]!.AsArray().OfType<JsonObject>().Where(i => i["type"]!.GetValue<string>() == "Skill" && i["system"]!["type"]!.GetValue<string>() == "active")
            .All(i => !i["name"]!.GetValue<string>().Contains(' ')),
            "legacy skill names support Foundry's exact internal defense lookup");
        check(!actor.ToJsonString().Contains("worldId") && !actor.ToJsonString().Contains("lastModifiedBy"), "export does not copy Foundry world or user identifiers");
        bool restored = false;
        try { FoundryWareEffects.Measure<int>(character.Improvements, () => { foreach (var i in character.Improvements) i.Enabled = false; }, () => throw new InvalidOperationException("probe")); }
        catch (InvalidOperationException) { restored = character.Improvements.Select(i => i.Enabled).SequenceEqual(enabledBefore); }
        check(restored, "temporary export measurements restore flags even if a calculation fails");
        wired.Name = "Wired Reflexes (2050)";
        check(Find(FoundryActorExporter.Export(character), "Wired Reflexes")["effects"]!.AsArray().Count == 1,
            "edition-labelled ware uses the same verified activation profile");
        wired.Name = "Custom triggered augmentation";
        var unknown = Find(FoundryActorExporter.Export(character), "Custom triggered");
        check(unknown["effects"]!.AsArray().Count == 0 && unknown["system"]!["notes"]!.GetValue<string>().Contains("no verified activation profile"),
            "unknown numeric ware gets explicit snapshot/activation notes rather than a guessed toggle");
        wired.Name = "Wired Reflexes";
        foreach (var i in character.Improvements.Where(i => i.SourceName == wired.InternalId)) i.Enabled = false;
        var inactive = FoundryActorExporter.Export(character);
        check(Find(inactive, "Wired Reflexes")["effects"]![0]!["disabled"]!.GetValue<bool>() &&
            inactive["system"]!["sheetStats"]!["REACTION"]!.GetValue<int>() == character.REA.TotalValue,
            "inactive ware exports a disabled effect and an inactive baseline");
        foreach (var i in character.Improvements.Where(i => i.SourceName == wired.InternalId)) i.Enabled = true;
        var mbw = AddWare("Move-by-Wire System", 1);
        mbw.Notes = "Original MBW notes";
        character.Skills.RemoveAll(skill => skill.Name == "Dodge");
        check(Find(FoundryActorExporter.Export(character), "Move-by-Wire")["system"]!["notes"]!.GetValue<string>().Contains("no matching exported skill record"),
            "ware with a missing skill emits a manual instruction instead of silently losing its bonus");
        var dodge = character.Skills.FirstOrDefault(s => s.Name == "Dodge");
        if (dodge == null) { dodge = new Skill(character) { Name = "Dodge", Attribute = "REA", SkillCategory = "Combat Active", Rating = 3 }; character.Skills.Add(dodge); }
        dodge.Rating = 3;
        var mixed = FoundryActorExporter.Export(character, FoundryActorExporter.Target.Version126);
        var mbwItem = Find(mixed, "Move-by-Wire");
        string notes = mbwItem["system"]!["notes"]!.GetValue<string>();
        check(notes.StartsWith(mbw.Notes, StringComparison.Ordinal) && notes.Contains("Dodge") && notes.Contains("never Gymnastics") && notes.Contains("skillwire"),
            "partially supported ware carries original notes and specific Dodge/skillwire instructions");
        check(Find(mixed, "Wired Reflexes")["system"]!["notes"]!.GetValue<string>().Contains("interact or reach a cap") &&
            mixed["system"]!["modifiers"]!["initiative"]!["passes"]!["physical"]!.GetValue<int>() == FoundryActorExporter.Effective(character.InitiativePasses) - 1,
            "nonadditive initiative stacking retains the correct current total with manual notes");
        var repeated = FoundryActorExporter.Export(character, FoundryActorExporter.Target.Version126);
        check(Find(repeated, "Move-by-Wire")["system"]!["notes"]!.GetValue<string>() == notes && mbw.Notes == "Original MBW notes",
            "repeated exports neither duplicate notices nor modify saved ware notes");
        string firstNotes = FoundryActorExporter.AddNotes("Original", new[] { "A", "A" });
        check(FoundryActorExporter.AddNotes(firstNotes, new[] { "A", "B" }).Split("Chummer export — manual handling required").Length == 2,
            "manual handling notes have one heading and deduplicate generated notices");
        var eyes = AddWare("Cybereyes Basic System", 2);
        var vision = AddWare("Low-Light Vision", 0);
        character.Cyberware.Remove(vision); vision.Parent = eyes; eyes.Children.Add(vision);
        var weaponXml = XmlManager.Instance.Load("weapons.xml");
        var weapon = new Weapon(character);
        weapon.Create(weaponXml.SelectSingleNode("/chummer/weapons/weapon[name='AK-97']"), character, new TreeNode(), null, null, null);
        character.Weapons.Add(weapon);
        var attachment = new WeaponAccessory(character);
        attachment.Create(weaponXml.SelectSingleNode("/chummer/accessories/accessory[name='Smartgun System, Internal']"), new TreeNode(), "Internal");
        attachment.Parent = weapon; weapon.WeaponAccessories.Add(attachment);
        var nested = FoundryActorExporter.Export(character, FoundryActorExporter.Target.Version126);
        var eyeRecord = Find(nested, "Cybereyes Basic"); var visionRecord = Find(nested, "Low-Light Vision");
        check(visionRecord["system"]!["essenceActual"]!.GetValue<decimal>() == 0 &&
            visionRecord["flags"]!["shadowrun4e"]!["chummerExport"]!["parentId"]!.GetValue<string>() == eyeRecord["_id"]!.GetValue<string>() &&
            visionRecord["system"]!["notes"]!.GetValue<string>().Contains("Containment/capacity"),
            "nested ware retains parent linkage and manual notes without a second Essence charge");
        var weaponRecord = Find(nested, "AK-97"); var attachmentRecord = Find(nested, "Smartgun System");
        check(weaponRecord["system"]!["installedModIds"]!.AsArray().Any(id => id!.GetValue<string>() == attachmentRecord["_id"]!.GetValue<string>()) &&
            attachmentRecord["system"]!["damageBonus"]!.GetValue<int>() == 0 &&
            attachmentRecord["system"]!["notes"]!.GetValue<string>().Contains("already included"),
            "weapon attachments use valid parent references and explain already-included bonuses");
        decimal cyberLoss = nested["items"]!.AsArray().OfType<JsonObject>().Where(i => i["type"]!.GetValue<string>() == "Implant")
            .Sum(i => i["system"]!["essenceActual"]!.GetValue<decimal>());
        check(nested["system"]!["sheetStats"]!["ESSENCE"]!.GetValue<decimal>() - cyberLoss == character.Essence,
            "newer Foundry Essence baseline reconstructs current Essence instead of subtracting implants twice");
        using (var viewer = new frmViewer { Characters = new List<Character> { character } }) {
            string exportPath = Path.Combine(output, "foundry-ui-export.json");
            File.WriteAllText(exportPath, "old output");
            viewer.ExportFoundryAsync(exportPath, 0, FoundryActorExporter.Target.Version16).GetAwaiter().GetResult();
            byte[] bytes = File.ReadAllBytes(exportPath);
            check(bytes[0] == (byte)'{' && JsonNode.Parse(File.ReadAllText(exportPath))!["name"]!.GetValue<string>() == character.Alias,
                "preview JSON output works without a loaded browser and writes UTF-8 atomically over an existing export");
        }
        File.WriteAllText(Path.Combine(output, "foundry-actor.json"), mixed.ToJsonString(new System.Text.Json.JsonSerializerOptions { WriteIndented = true }));
        using var dialog = new FoundryExportDialog(new[] { character });
        check(dialog.Target == FoundryActorExporter.Target.Version16 && dialog.CharacterIndex == 0, "Foundry export defaults to the supplied sample version and permits explicit version choice");
        dialog.Show();
        File.WriteAllBytes(Path.Combine(output, "foundry-export-options.png"), Majorsilence.Forms.Headless.HeadlessRenderer.CapturePng(dialog, 700, 360));
    }
}
