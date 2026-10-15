#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;
using Majorsilence.Forms.Headless;

namespace Chummer;

internal static class PacksImportChecks
{
    private static XmlNode Kit(string body)
    {
        var document = new XmlDocument();
        document.LoadXml("<pack><name>Check</name><category>Custom</category>" + body + "</pack>");
        return document.DocumentElement!;
    }
    private static T Field<T>(object instance, string name) => (T)instance.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(instance)!;
    private static void Discard(frmCreate editor)
    {
        if (editor.IsDisposed) return;
        editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
        editor.Close();
    }
    internal static void Run(Character source, frmMain parent, Action<bool, string> check)
    {
        byte[] original = source.CaptureSnapshot();
        var copy = new Character { FileName = source.FileName };
        check(copy.LoadSnapshot(original) && copy.CaptureSnapshot().SequenceEqual(original),
            "in-memory character snapshots preserve character data without touching a file");
        byte[] file = File.ReadAllBytes(source.FileName);
        var kit = Kit("<skills><skill><name>Pistols</name><rating>2</rating><spec>Semi-Automatics</spec></skill></skills>" +
            "<gears><gear><name>Backpack</name><qty>2</qty><gears><gear><name>Medkit</name><rating>3</rating><qty>3</qty></gear></gears></gear></gears>");
        Task.Run(async () => {
            using var owner = new Form();
            var prepared = await frmCreate.PreparePacksKitAsync(original, source.FileName, kit, false,
                new BulkWork(CancellationToken.None, 2, (_, _) => {}), owner);
            var backpack = prepared.Gear.Last(g => g.Name == "Backpack");
            check(backpack.Quantity == 2 && backpack.Children.Single().Name == "Medkit" && backpack.Children.Single().Rating == 3 &&
                backpack.Children.Single().Quantity == 3 && prepared.Skills.Single(s => s.Name == "Pistols").Rating == 2,
                "isolated PACKS preparation retains nested quantities, ratings and skill changes");
            check(source.CaptureSnapshot().SequenceEqual(original) && File.ReadAllBytes(source.FileName).SequenceEqual(file),
                "preparing a successful PACKS kit leaves the live model and source file untouched");
            var catalog = XmlManager.Instance.Load("packs.xml");
            foreach (string name in new[] { "Brawler", "Handgun Trainee", "Pistoleer", "Aggression", "Light Mana Combat", "The Basics" })
            {
                var familyCharacter = new Character { FileName = source.FileName };
                familyCharacter.LoadSnapshot(original);
                if (name == "Pistoleer") { familyCharacter.AdeptEnabled = true; familyCharacter.MAGEnabled = true; familyCharacter.MAG.Value = 6; }
                if (name == "Light Mana Combat") { familyCharacter.MagicianEnabled = true; familyCharacter.MAGEnabled = true; familyCharacter.MAG.Value = 6; }
                if (name == "Aggression") { familyCharacter.TechnomancerEnabled = true; familyCharacter.RESEnabled = true; familyCharacter.RES.Value = 6; }
                var familyKit = catalog.SelectNodes("/chummer/packs/pack")!.Cast<XmlNode>().Single(n => n["name"]!.InnerText == name && (name != "Brawler" || n["category"]!.InnerText == "Attribute Kits")).CloneNode(true);
                foreach (XmlNode sinName in familyKit.SelectNodes(".//gear/name[.='Fake SIN']")!)
                {
                    var forced = sinName.OwnerDocument!.CreateAttribute("select"); forced.Value = "Check SIN"; sinName.Attributes!.Append(forced);
                }
                var result = await frmCreate.PreparePacksKitAsync(familyCharacter.CaptureSnapshot(), source.FileName, familyKit, true,
                    new BulkWork(CancellationToken.None, 100, (_, _) => {}), owner);
                bool valid = name switch {
                    "Brawler" => result.BOD.Value == 5 && result.AGI.Value == 4,
                    "Handgun Trainee" => result.Skills.Single(s => s.Name == "Pistols").Rating == 2 && result.Skills.Single(s => s.Name == "Clubs").Rating == 1,
                    "Pistoleer" => result.Powers.Any(p => p.Name == "Improved Ability (Combat)" && p.Rating == 2) && result.Powers.Any(p => p.Name == "Improved Reflexes 1"),
                    "Aggression" => result.TechPrograms.Any(p => p.Name == "Attack" && p.Rating == 5) && result.TechPrograms.Any(p => p.Name == "Black Hammer" && p.Rating == 3),
                    "Light Mana Combat" => new[] { "Death Touch", "Manabolt", "Stunbolt" }.All(n => result.Spells.Any(p => p.Name == n)),
                    "The Basics" => result.Armor.Any(a => a.Name == "Armor Jacket") && result.Gear.Any(g => g.Name == "Fake SIN" && g.Quantity == 3) && result.Gear.Any(g => g.Name == "Datachip" && g.Quantity == 4),
                    _ => false
                };
                check(valid && source.CaptureSnapshot().SequenceEqual(original), "isolated PACKS family preserves established results: " + name);
            }
            using var cancellation = new CancellationTokenSource();
            bool cancelled = false;
            try
            {
                await frmCreate.PreparePacksKitAsync(original, source.FileName, kit, false,
                    new BulkWork(cancellation.Token, 2, (item, _) => { if (item == 2) cancellation.Cancel(); }), owner);
            }
            catch (OperationCanceledException) { cancelled = true; }
            check(cancelled && source.CaptureSnapshot().SequenceEqual(original),
                "cancelling PACKS after an earlier entry leaves no partial live-character changes");
            bool failed = false;
            try
            {
                await frmCreate.PreparePacksKitAsync(original, source.FileName,
                    Kit("<gears><gear><name>Backpack</name></gear><gear><name>Missing catalog entry for checks</name></gear></gears>"),
                    false, new BulkWork(CancellationToken.None, 2, (_, _) => {}), owner);
            }
            catch (InvalidDataException) { failed = true; }
            check(failed && source.CaptureSnapshot().SequenceEqual(original) && File.ReadAllBytes(source.FileName).SequenceEqual(file),
                "PACKS catalog failure after an earlier entry leaves the original model and file unchanged");
        }).GetAwaiter().GetResult();

        var editor = new frmCreate(copy) { Loading = true, MdiParent = parent };
        editor.Show();
        var previousHistory = Field<InventoryUndo>(editor, "_inventoryUndo");
        var priorGear = copy.Gear.First();
        string priorGearId = priorGear.InternalId;
        int priorQuantity = priorGear.Quantity;
        priorGear.Quantity++;
        previousHistory.Observe();
        int priorActions = previousHistory.Count;
        byte[] beforeImport = copy.CaptureSnapshot();
        var pending = editor.ImportPacksKitAsync(kit, false);
        // Shown is posted by the toolkit. Its headless modal test loop delivers
        // dispatcher callbacks; production uses the native dispatcher instead.
        var progressDialog = Application.OpenForms.OfType<BulkProgressDialog>().Single();
        File.WriteAllBytes(Path.Combine(Path.GetDirectoryName(source.FileName)!, "packs-progress.png"),
            HeadlessRenderer.CapturePng(progressDialog, 880, 264));
        Majorsilence.Forms.Backends.Platform.Backend.RunModalLoop(pending.WaitAsync(TimeSpan.FromSeconds(20)));
        var replacement = pending.GetAwaiter().GetResult();
        check(replacement != null && editor.IsDisposed && !replacement.IsDisposed &&
            Field<Character>(replacement, "_objCharacter").Gear.Any(g => g.Name == "Backpack") &&
            copy.CaptureSnapshot().SequenceEqual(beforeImport) && Field<bool>(replacement, "_blnIsDirty"),
            "successful PACKS commits one complete unsaved editor replacement while preserving the original model");
        var history = Field<InventoryUndo>(replacement!, "_inventoryUndo");
        check(history.Count > 0 && history.Undo() && !Field<Character>(replacement!, "_objCharacter").Gear.Any(g => g.Name == "Backpack"),
            "inventory undo remains available for a committed PACKS kit");
        check(history.Count == priorActions && history.Undo() &&
            Field<Character>(replacement!, "_objCharacter").Gear.Single(g => g.InternalId == priorGearId).Quantity == priorQuantity,
            "inventory history from before PACKS remains bound to the replacement editor");
        Discard(replacement!);
        check(File.ReadAllBytes(source.FileName).SequenceEqual(file), "PACKS commit and inventory undo never automatically save the source character");
    }
}
