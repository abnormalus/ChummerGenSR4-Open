#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

// Exercises the real native dispatcher and modal owner graph; headless rendering alone
// cannot establish cancellation/close behavior in WKWebView/Avalonia desktop windows.
internal static class NativeBulkChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static XmlNode Kit(string body)
    {
        var document = new XmlDocument(); document.LoadXml("<pack><name>Native check</name><category>Custom</category>" + body + "</pack>");
        return document.DocumentElement!;
    }
    private static async Task WaitForAsync(Func<bool> condition)
    {
        using var timeout = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        while (!condition()) await Task.Delay(5, timeout.Token);
    }
    internal static void DiscardEditors()
    {
        foreach (var editor in Application.OpenForms.OfType<frmCreate>().ToArray())
            editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false);
    }
    internal static async Task RunAsync(frmMain main, string path, string report)
    {
        var checks = new List<string>();
        bool passed = false;
        string? failedCheck = null;
        int uiThread = Environment.CurrentManagedThreadId;
        void Check(bool valid, string name)
        {
            // Only fixed check labels enter the report: exception messages may
            // contain local paths or character data and remain excluded.
            if (!valid) { failedCheck = name; throw new InvalidOperationException(name); }
            checks.Add(name); AppDiagnostics.Record("bulk.native-check");
        }
        try
        {
            Check(SynchronizationContext.Current != null, "Native UI synchronization context is installed");
            await NativeControlChecks.RunAsync(Check);
            await NativeRenderingChecks.RunAsync(Check);
            byte[] originalFile = File.ReadAllBytes(path);
            var character = new Character { FileName = path };
            if (!character.Load(false)) throw new InvalidDataException("Unable to load generated check character.");
            await NativeCharacterSwitchChecks.RunAsync(main, character, Check);
            await NativeWeaponPickerChecks.RunAsync(main, character, Path.GetDirectoryName(Path.GetFullPath(report))!, Check);
            await NativeArmorPickerChecks.RunAsync(main, character, Path.GetDirectoryName(Path.GetFullPath(report))!, Check);
            await NativeAccessoryPickerChecks.RunAsync(main, character, Path.GetDirectoryName(Path.GetFullPath(report))!, Check);
            await NativeInventoryEnhancementChecks.RunAsync(main, character, Check);
            await NativeGearEligibilityChecks.RunAsync(main, character, Check);
            var editor = new frmCreate(character) { Loading = true, MdiParent = main };
            editor.Show();
            byte[] original = character.CaptureSnapshot();
            var imported = await editor.ImportPacksKitAsync(Kit("<gears><gear><name>Backpack</name><qty>2</qty>" +
                "<gears><gear><name>Medkit</name><rating>3</rating></gear></gears></gear></gears>"), false);
            Check(imported != null && editor.IsDisposed && Environment.CurrentManagedThreadId == uiThread &&
                character.CaptureSnapshot().SequenceEqual(original), "Native PACKS commit stays on the UI thread and preserves the original model");
            editor = imported!;
            var current = Field<Character>(editor, "_objCharacter");
            var backpack = current.Gear.Last(g => g.Name == "Backpack");
            Check(backpack.Quantity == 2 && backpack.Children.Single().Rating == 3,
                "Native PACKS commit preserves nested equipment");
            byte[] beforeCancel = current.CaptureSnapshot();
            var pending = editor.ImportPacksKitAsync(Kit("<gears>" + string.Concat(Enumerable.Repeat(
                "<gear><name>Backpack</name></gear>", 1000)) + "</gears>"), false);
            await WaitForAsync(() => pending.IsCompleted || Application.OpenForms.OfType<BulkProgressDialog>()
                .Any(d => d.Controls.OfType<ProgressBar>().Single().Value >= 2));
            Check(!pending.IsCompleted, "Native dispatcher accepts cancellation while PACKS work is in progress");
            var progress = Application.OpenForms.OfType<BulkProgressDialog>().Single();
            progress.Controls.OfType<Button>().Single().PerformClick();
            Check(await pending == null && !editor.IsDisposed && current.CaptureSnapshot().SequenceEqual(beforeCancel),
                "Native Cancel discards partially prepared PACKS entries");

            // Simulate a queued/native edit while a prepared copy is outstanding.
            pending = editor.ImportPacksKitAsync(Kit("<gears><gear><name>Medkit</name><rating>2</rating></gear></gears>"), false);
            Field<TextBox>(editor, "txtAlias").Text = "Native concurrent edit";
            bool conflict = false;
            try { await pending; } catch (PacksImportConflictException) { conflict = true; }
            Check(conflict && !editor.IsDisposed && current.Alias == "Native concurrent edit",
                "Native PACKS conflict preserves newer live-character edits instead of applying a stale copy");
            beforeCancel = current.CaptureSnapshot();
            string profilePath = Path.Combine(MacPaths.SettingsDirectory, current.SettingsFile);
            byte[] profileBytes = File.ReadAllBytes(profilePath);
            pending = editor.ImportPacksKitAsync(Kit("<gears><gear><name>Medkit</name><rating>2</rating></gear></gears>"), false);
            conflict = false;
            try
            {
                var changedProfile = new XmlDocument(); changedProfile.Load(profilePath);
                changedProfile.AppendChild(changedProfile.CreateComment("Native rules change check"));
                changedProfile.Save(profilePath);
                try { await pending; } catch (PacksImportConflictException) { conflict = true; }
            }
            finally { File.WriteAllBytes(profilePath, profileBytes); }
            Check(conflict && !editor.IsDisposed && current.CaptureSnapshot().SequenceEqual(beforeCancel),
                "Native PACKS conflict preserves a changed rules profile without replacing the live character");

            // Quit while a legacy item-rule prompt owns a nested modal loop. Guard the
            // main close to simulate an unsaved-character cancellation after cleanup.
            pending = editor.ImportPacksKitAsync(Kit("<gears><gear><name>Fake SIN</name><rating>2</rating></gear></gears>"), false);
            await WaitForAsync(() => pending.IsCompleted || Application.OpenForms.OfType<frmSelectText>().Any());
            Check(!pending.IsCompleted && Application.OpenForms.OfType<frmSelectText>().Any(),
                "Native PACKS detail prompt opens as an operation-owned dialog");
            FormClosingEventHandler guard = (_, args) => args.Cancel = true;
            main.FormClosing += guard;
            try { await AppPlatform.QuitApplicationAsync(); }
            finally { main.FormClosing -= guard; }
            Check(await pending == null && !Application.OpenForms.OfType<BulkProgressDialog>().Any() &&
                !Application.OpenForms.OfType<frmSelectText>().Any() && current.CaptureSnapshot().SequenceEqual(beforeCancel),
                "Native Quit unwinds a PACKS detail prompt and waits for safe cleanup");

            // A loader warning is also a modal prompt owned by batch preparation.
            string wrongEdition = Path.Combine(Path.GetDirectoryName(Path.GetFullPath(report))!, "wrong-edition.chum");
            var wrong = new XmlDocument(); wrong.Load(path);
            wrong.SelectSingleNode("/character/gameedition")!.InnerText = "SR5";
            wrong.Save(wrongEdition);
            using var batch = new frmPrintMultiple();
            var batchDialog = batch.ShowDialogAsync(main);
            Field<TreeView>(batch, "treCharacters").Nodes.Add(new TreeNode("Generated check character") { Tag = wrongEdition });
            Field<Button>(batch, "cmdPrint").PerformClick();
            await WaitForAsync(() => BulkOperationDialogs.OwnedPrompts(batch).Length > 0);
            Check(BulkOperationDialogs.OwnedPrompts(batch).Length > 0, "Native batch loader warning is an operation-owned prompt");
            main.FormClosing += guard;
            try { await AppPlatform.QuitApplicationAsync(); }
            finally { main.FormClosing -= guard; }
            Check(Field<CancellationTokenSource?>(batch, "batchCancellation") == null && Field<Button>(batch, "cmdSelectCharacter").Enabled &&
                BulkOperationDialogs.OwnedPrompts(batch).Length == 0,
                "Native Quit cancels batch preparation, closes its loader warning and restores controls before closing");
            batch.Close();
            await batchDialog;
            Check(File.ReadAllBytes(path).SequenceEqual(originalFile), "Native bulk checks never modify the source character file");
            passed = true;
            AppDiagnostics.Record("bulk.native-checks-completed");
        }
        finally
        {
            Directory.CreateDirectory(Path.GetDirectoryName(Path.GetFullPath(report))!);
            await File.WriteAllTextAsync(report, JsonSerializer.Serialize(new { passed, checks, failedCheck }, new JsonSerializerOptions { WriteIndented = true }));
        }
    }
}
