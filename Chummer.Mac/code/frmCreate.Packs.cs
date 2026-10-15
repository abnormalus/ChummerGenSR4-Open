#nullable enable
using System;
using System.Linq;
using System.IO;
using System.Security.Cryptography;
using System.Threading.Tasks;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private readonly bool _packsWorkspace;
    private bool _packsCommandRunning;
    private bool _closeAfterPacks;
    private BulkProgressDialog? _packsProgress;
    private Task _packsCleanup = Task.CompletedTask;
    private BulkWork? _packsWork;
    private Form? _packsPromptOwner;
    internal Task CancelPacksForQuitAsync()
    {
        if (_packsProgress != null) _ = _packsProgress.CancelForQuitAsync();
        return _packsCleanup;
    }

    private async Task AddPacksKitsAsync()
    {
        if (_packsCommandRunning || AppPlatform.QuitInProgress) return;
        frmCreate editor = this;
        editor._packsCommandRunning = true;
        try
        {
            while (!editor.IsDisposed && !AppPlatform.QuitInProgress)
            {
                using var picker = new frmSelectPACKSKit(editor._objCharacter);
                if (await picker.ShowDialogAsync(editor) != DialogResult.OK || editor.IsDisposed || AppPlatform.QuitInProgress) return;
                // Compare names as values rather than interpolating them into an XPath expression.
                var catalogKits = XmlManager.Instance.Load("packs.xml").SelectNodes("/chummer/packs/pack")?.Cast<XmlNode>()
                    ?? throw new InvalidOperationException("PACKS catalog is unavailable.");
                var kit = catalogKits
                    .FirstOrDefault(n => n["name"]?.InnerText == picker.SelectedKit && n["category"]?.InnerText == picker.SelectedCategory);
                if (kit == null) throw new InvalidOperationException("Selected PACKS kit is unavailable.");
                var next = await editor.ImportPacksKitAsync(kit, picker.SelectedCategory != "Custom");
                if (next == null) return;
                editor._packsCommandRunning = false;
                editor = next;
                editor._packsCommandRunning = true;
                if (!picker.AddAgain) return;
            }
        }
        catch (PacksImportConflictException error)
        {
            AppDiagnostics.Record("packs.import-conflict", error);
            if (!editor.IsDisposed) AppErrors.Inform(editor, "Packs_Changed");
        }
        catch (Exception error)
        {
            AppDiagnostics.Record("packs.command-failed", error);
            if (!editor.IsDisposed) AppErrors.Inform(editor, "Packs_Failed");
        }
        finally { editor._packsCommandRunning = false; }
    }

    internal async Task<frmCreate?> ImportPacksKitAsync(XmlNode kit, bool createChildren)
    {
        if (_packsProgress != null || IsDisposed || AppPlatform.QuitInProgress) return null;
        _inventoryUndo?.Observe();
        byte[] snapshot = _objCharacter.CaptureSnapshot();
        string filename = _objCharacter.FileName;
        string rulesPath = Path.Combine(MacPaths.SettingsDirectory, _objCharacter.SettingsFile);
        byte[]? rulesStamp = File.Exists(rulesPath) ? SHA256.HashData(File.ReadAllBytes(rulesPath)) : null;
        var finished = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        _packsCleanup = finished.Task;
        using var progress = new BulkProgressDialog();
        _packsProgress = progress;
        AppDiagnostics.Record("packs.import-start");
        try
        {
            int count = kit.SelectNodes("qualities/positive/quality | qualities/negative/quality | attributes/* | skills/skill | skills/skillgroup | knowledgeskills/skill | martialarts/martialart | martialarts/maneuver | powers/power | programs/program | spells/spell | spirits/spirit | lifestyles/lifestyle | armors/armor | weapons/weapon | cyberwares/cyberware | biowares/bioware | gears/gear | vehicles/vehicle | selectmartialart | nuyenbp")?.Count ?? 0;
            var prepared = await progress.RunAsync(this, count,
                work => PreparePacksKitAsync(snapshot, filename, kit, createChildren, work, progress));
            _packsProgress = null;
            if (prepared == null || IsDisposed || AppPlatform.QuitInProgress) return null;
            bool sameRules = rulesStamp == null ? !File.Exists(rulesPath) : File.Exists(rulesPath) &&
                SHA256.HashData(File.ReadAllBytes(rulesPath)).SequenceEqual(rulesStamp);
            // A queued/native command may have edited the live character despite window
            // modality. Never replace newer edits or a newly saved rules profile.
            if (!sameRules || _objCharacter.FileName != filename || !_objCharacter.CaptureSnapshot().SequenceEqual(snapshot))
                throw new PacksImportConflictException();
            var replacement = CommitPreparedPacks(prepared);
            AppDiagnostics.Record("packs.import-completed");
            return replacement;
        }
        finally
        {
            _packsProgress = null;
            finished.TrySetResult();
            bool close = _closeAfterPacks;
            _closeAfterPacks = false;
            if (close && !AppPlatform.QuitInProgress && !IsDisposed) Close();
        }
    }

    internal static async Task<Character> PreparePacksKitAsync(byte[] snapshot, string filename,
        XmlNode kit, bool createChildren, BulkWork work, Form promptOwner)
    {
        var character = new Character { FileName = filename };
        if (!character.LoadSnapshot(snapshot)) throw new InvalidOperationException("Unable to prepare character copy.");
        var workspace = new frmCreate(character, true) { Loading = true, _packsPromptOwner = promptOwner };
        try
        {
            workspace.frmCreate_Load(workspace, EventArgs.Empty);
            await work.CheckpointAsync();
            await workspace.ApplyPacksKitAsync(kit, createChildren, work);
            return character;
        }
        finally { workspace.DisposePacksWorkspace(); }
    }

    private void DisposePacksWorkspace()
    {
        _blnIsDirty = false;
        _blnSkipToolStripRevert = true;
        frmCreate_FormClosing(this, new FormClosingEventArgs(CloseReason.None, false));
    }

    private frmCreate CommitPreparedPacks(Character prepared)
    {
        var original = _objCharacter;
        var history = _inventoryUndo;
        bool dirty = _blnIsDirty;
        var replacement = new frmCreate(prepared) { Loading = true, MdiParent = MdiParent, WindowState = WindowState };
        string? selectedTab = tabCharacterTabs.SelectedTab?.Name;
        try
        {
            replacement.Show();
            replacement._inventoryUndo?.ContinueAfterImport(history);
            var selected = replacement.tabCharacterTabs.TabPages.Cast<TabPage>().FirstOrDefault(t => t.Name == selectedTab);
            if (selected != null) replacement.tabCharacterTabs.SelectedTab = selected;
            replacement._blnIsDirty = true;
            replacement.UpdateWindowTitle(false);
            // The replacement is fully initialized before retiring the old editor. No
            // character save occurs: the imported character remains an unsaved change.
            _blnIsDirty = false;
            _blnSkipToolStripRevert = true;
            var parent = MdiParent as frmMain;
            Close();
            if (!IsDisposed) throw new InvalidOperationException("Unable to replace the original character editor.");
            parent?.ObserveReplacementCharacter(original, prepared);
            return replacement;
        }
        catch (Exception error)
        {
            if (IsDisposed)
            {
                // Once the old editor has gone, retain the fully initialized replacement.
                AppDiagnostics.Record("packs.commit-followup-failed", error);
                return replacement;
            }
            _blnIsDirty = dirty;
            if (!replacement.IsDisposed) replacement.DisposePacksWorkspace();
            throw;
        }
    }
}

internal sealed class PacksImportConflictException : InvalidOperationException
{
    internal PacksImportConflictException() : base("The character or rules changed during PACKS preparation.") { }
}
