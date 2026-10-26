#nullable enable
using System;
using System.Linq;
using System.Reflection;
using Majorsilence.Forms;

namespace Chummer;

internal static class GearPickerChecks
{
    internal static void Run(Character original, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = false;
        foreach (string book in new[] { "SR4", "AR", "UN" }) if (!character.Options.Books.Contains(book)) character.Options.Books.Add(book);
        byte[] snapshot = character.CaptureSnapshot();
        string[] books = character.Options.Books.ToArray();
        var reference = GearPickerReference.Catalog();
        character.Options.Books.Clear(); character.Options.Books.AddRange(reference.Books);
        snapshot = character.CaptureSnapshot(); books = character.Options.Books.ToArray();
        var nativeMemory = typeof(GearPickerModel).GetField("lastCategory", BindingFlags.Static | BindingFlags.NonPublic)!;
        object? oldNative = nativeMemory.GetValue(null);
        int caseIndex = 0;
        try
        {
            foreach (var options in new[] {
                new GearPickerOptions(InitialCategory: "Biotech"),
                new GearPickerOptions("Ammunition", AmmunitionCategory: "Assault Rifles"),
                new GearPickerOptions("Commlink Upgrade", Context: new(false,false,false,5,5,0,0)),
                new GearPickerOptions("Commlink Operating System Upgrade", Context: new(false,false,false,0,0,3,3)),
                new GearPickerOptions("Electronics and Sensor Options", Context: new(true,false,false,0,0,0,0)),
                new GearPickerOptions("Electronics and Sensor Options", Context: new(false,true,false,0,0,0,0)),
                new GearPickerOptions("Electronics and Sensor Options", Context: new(false,false,true,0,0,0,0)) })
            {
                var model = new GearPickerModel(character, false, options);
                check(GearPickerReference.Digest(model.Categories.Select(item => item.Code)) == reference.Baseline[caseIndex + ":categories"],
                    "gear model category discovery matches frozen legacy context " + options.AllowedCategories);
                foreach (var category in model.Categories)
                {
                    model.Category = category;
                    check(GearPickerReference.Digest(model.Items.Select(item => item.Name)) == reference.Baseline[caseIndex + ":category:" + category.Code],
                        "gear model browsing matches frozen legacy category " + category.Code);
                }
                foreach (string query in new[] { "comlink", "Medkit", "Ammo", "O'Brien \"quoted\"" })
                {
                    model.SearchText = query;
                    check(GearPickerReference.Digest(model.Items.Select(item => item.Name + "^" + item.Category)) == reference.Baseline[caseIndex + ":search:" + query],
                        "gear model literal category search matches frozen legacy for " + query);
                }
                caseIndex++;
            }
            var draft = new GearPickerModel(character, true, new(InitialCategory: "Ammunition"));
            void Select(string category, string name)
            {
                draft.Category = draft.Categories.Single(item => item.Code == category);
                draft.SelectedEntry = draft.Items.Single(item => item.Name == name);
            }
            Select("Ammunition", "Ammo: Regular Ammo");
            check(draft.Quantity == 10 && draft.QuantityIncrement == 10 && draft.MinimumQuantity == 1 && draft.MaximumQuantity == 100000,
                "gear model retains complete quantity domain and ammunition pack step");
            draft.Quantity = 100001; check(draft.Quantity == 100000, "gear model clips quantity only at established maximum");
            Select("Biotech", "Medkit"); draft.Rating = 4; draft.Quantity = 3;
            check(draft.Details?.Cost == string.Format("{0:###,###,##0¥}", 1200) && draft.MinimumRating == 1 && draft.MaximumRating == 6,
                "gear model composes rated quantities through shared preview");
            var accepted = draft.AcceptGear(true);
            check(accepted is { Item.Name: "Medkit", Item.Category: "Biotech", Item.Rating: 4, Item.Quantity: 3, Item.AddAgain: true },
                "gear model returns exact accepted quantity rating category and Add Again draft");
            Select("Ammunition", "Grenade: Fragmentation"); draft.Aerodynamic = true;
            check(draft.IsAerodynamicVisible && draft.Aerodynamic && draft.Rating == 0 && !draft.IsRatingEnabled,
                "gear model exposes grenade option and disables unrated input");
            Select("Biotech", "Medkit"); check(!draft.Aerodynamic && !draft.IsAerodynamicVisible && draft.Quantity == 1,
                "gear model leaving grenade clears its flag and resets quantity");
            var empty = new GearPickerModel(character, false, new("Commlink Operating System Upgrade", Context: new(false,false,false,0,0,10,10)));
            check(empty.Categories.Count == 0 && empty.Items.Count == 0 && !empty.CanAccept && empty.AcceptGear(false) == null,
                "gear model exhausted category context cannot become an uncategorized unrestricted catalog");
            var contextual = new GearPickerModel(character, false, new("Commlink Upgrade", Context: new(false,false,false,5,5,0,0)));
            contextual.SelectedEntry = contextual.Items.Single(item => item.Name == "Response 4");
            contextual.SelectionContext = new(false,false,false,7,7,0,0);
            check(contextual.AcceptGear(false) == null, "gear model rejects context changed after selection without accepting a replacement");
            character.Options.Books.Clear();
            check(draft.AcceptGear(false) == null && draft.Items.Count == 0 && !draft.CanAccept,
                "gear model rejects a source disabled after selection and clears details");
            character.Options.Books.AddRange(books);
            var ai = new Character(); ai.LoadSnapshot(snapshot); ai.Metatype = "A.I.";
            var aiDraft = new GearPickerModel(ai, true, new(InitialCategory: "Matrix Programs"));
            aiDraft.SelectedEntry = aiDraft.Items.Single(item => item.Name == "Analyze"); aiDraft.InherentProgram = true;
            check(aiDraft.IsInherentProgramVisible && aiDraft.InherentProgram, "gear model permits established AI inherent software choice");
            aiDraft.Hacked = true;
            check(!aiDraft.InherentProgram && !aiDraft.IsInherentProgramEnabled, "gear model hacked software clears incompatible inherent choice");
            var restricted = new GearPickerModel(character, false, new("Biotech", InitialCategory: "not a category"));
            check(restricted.Category?.Code == "Biotech" && !restricted.IsQuantityEnabled && !restricted.FreeCost && !restricted.Stack,
                "gear model restricted creation context retains fallback and purchase option defaults");
            var emptyAllowed = new GearPickerModel(character, false, new(","));
            check(emptyAllowed.Items.Count == 0 && !emptyAllowed.CanAccept, "gear model nonempty restriction with no category tokens cannot browse unrestricted items");
            var duplicateExtras = new GearPickerModel(character, false, new(AdditionalCategories: "Biotech,Biotech", InitialCategory: "Biotech"));
            check(duplicateExtras.Categories.Count(item => item.Code == "Biotech") == 1 && duplicateExtras.CanAccept,
                "gear duplicate extra categories retain one usable canonical category");
            var customCatalog = new System.Xml.XmlDocument();
            customCatalog.LoadXml("<chummer><categories><category>Biotech</category><category show='false'>Hidden</category></categories><gears>" +
                "<gear><name>Synthetic minimum</name><category>Biotech</category><rating>6</rating><minrating>invalid</minrating><cost>100</cost><avail>0</avail><source>SR4</source><page>1</page></gear>" +
                "<gear><name>Synthetic nonrated</name><category>Hidden</category><rating>-1</rating><cost>100</cost><avail>0</avail><source>SR4</source><page>1</page></gear></gears></chummer>");
            var custom = new GearPickerModel(character, false, catalog: customCatalog);
            check(custom.MinimumRating == 1 && custom.MaximumRating == 6 && custom.CanAccept,
                "gear model preserves malformed optional minimum fallback without rejecting the item");
            check(custom.Categories.Count == 1 && custom.Categories[0].Code == "Biotech", "gear model keeps implicit hidden categories out of ordinary browsing");
            var explicitlyHidden = new GearPickerModel(character, false, new("Hidden"), customCatalog);
            check(explicitlyHidden.SelectedEntry?.Name == "Synthetic nonrated" && explicitlyHidden.Rating == 0 && !explicitlyHidden.IsRatingEnabled,
                "gear model preserves explicit hidden category and nonpositive rating behavior");
            var largePackDefinition = customCatalog.CreateElement("gear");
            largePackDefinition.InnerXml = "<name>Synthetic large pack</name><category>Biotech</category><rating>0</rating><costfor>100001</costfor><cost>100</cost><avail>0</avail><source>SR4</source><page>1</page>";
            customCatalog.SelectSingleNode("/chummer/gears")!.AppendChild(largePackDefinition);
            var largePack = new GearPickerModel(character, false, catalog: customCatalog);
            largePack.SelectedEntry = largePack.Items.Single(item => item.Name == "Synthetic large pack");
            check(largePack.Quantity == 100000 && largePack.QuantityIncrement == 100001 && largePack.CanAccept,
                "gear oversized catalog pack retains legacy quantity clipping and original price divisor");
            check(character.CaptureSnapshot().SequenceEqual(snapshot), "gear native model browsing and accepted drafts preserve complete character snapshot");
        }
        finally
        {
            character.Options.Books.Clear(); character.Options.Books.AddRange(books);
            nativeMemory.SetValue(null, oldNative);
        }
    }
}
