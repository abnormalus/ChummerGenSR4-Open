#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Threading;
using Majorsilence.Forms;

namespace Chummer;

public partial class frmMain
{
    private readonly HashSet<Form> reportedAutosaveFailures = new();
    private bool autosaveInProgress;

    private void InitializeCharacterAutosave()
    {
        var timer = new DispatcherTimer();
        void UpdateTimer()
        {
            timer.Stop();
            if (GlobalOptions.Instance.CharacterAutosaveEnabled)
            {
                timer.Interval = TimeSpan.FromMinutes(GlobalOptions.Instance.CharacterAutosaveMinutes);
                timer.Start();
            }
        }

        timer.Tick += async (_, _) =>
        {
            if (IsDisposed || autosaveInProgress || repositoryBusy || NativeDialogHost.Count != 0 || !Enabled) return;
            autosaveInProgress = true;
            try
            {
                reportedAutosaveFailures.RemoveWhere(editor => editor.IsDisposed);
                foreach (Form editor in MdiChildren.ToArray())
                {
                    if (IsDisposed || editor.IsDisposed || !GlobalOptions.Instance.CharacterAutosaveEnabled) continue;
                    switch (editor)
                    {
                        case frmCreate creation: await creation.AutosaveAsync(this); break;
                        case frmCareer career: await career.AutosaveAsync(this); break;
                    }
                }
            }
            finally { autosaveInProgress = false; }
        };
        Shown += (_, _) =>
        {
            if (MacPaths.TestUserDirectory != null) return;
            GlobalOptions.Instance.CharacterAutosaveChanged += UpdateTimer;
            UpdateTimer();
        };
        FormClosed += (_, _) =>
        {
            timer.Stop();
            GlobalOptions.Instance.CharacterAutosaveChanged -= UpdateTimer;
        };
    }

    internal async Task AutosaveCharacterAsync(Form editor, Character character,
        Func<Action<Exception>, bool> saveLocal, Action savedToRepository, Func<bool>? stillCurrent = null)
    {
        if (repositoryBusy || string.IsNullOrEmpty(character.FileName)) return;
        bool saved = HasRepositoryLink(character.FileName)
            ? await SaveLinkedCharacterAsync(editor, character, character.CaptureSnapshot, savedToRepository, automatic: true, stillCurrent)
            : saveLocal(error => ReportSaveProblem(editor, error.Message, true));
        if (saved) reportedAutosaveFailures.Remove(editor);
    }

    private void ReportSaveProblem(Form editor, string message, bool automatic)
    {
        if (IsDisposed || editor.IsDisposed || automatic && !reportedAutosaveFailures.Add(editor)) return;
        MessageBox.Show(this, automatic ? $"Chummer could not autosave {editor.Text.TrimEnd('*')}.\n\n{message}" : message,
            automatic ? "Autosave Failed" : "Save Character", MessageBoxButtons.OK, MessageBoxIcon.Warning);
    }

    internal void ClearAutosaveFailure(Form editor) => reportedAutosaveFailures.Remove(editor);
}

public partial class frmCreate
{
    internal Task AutosaveAsync(frmMain main) =>
        !_blnIsDirty || chkCharacterCreated.Checked || _packsProgress != null ? Task.CompletedTask :
            main.AutosaveCharacterAsync(this, _objCharacter, SaveCharacterLocally, () => FinishCreationSave(false),
                () => !chkCharacterCreated.Checked && _packsProgress == null);

    private byte[] CaptureRepositorySave() => !_blnIsDirty && File.Exists(RepositoryLocalFile)
        ? File.ReadAllBytes(RepositoryLocalFile) : _objCharacter.CaptureSnapshot();
}

public partial class frmCareer
{
    internal Task AutosaveAsync(frmMain main) => !_blnIsDirty ? Task.CompletedTask :
        main.AutosaveCharacterAsync(this, _objCharacter, SaveCharacterLocally, CompleteRepositorySave);

    private byte[] CaptureRepositorySave() => !_blnIsDirty && File.Exists(RepositoryLocalFile)
        ? File.ReadAllBytes(RepositoryLocalFile) : _objCharacter.CaptureSnapshot();

    private void CompleteRepositorySave()
    {
        _blnIsDirty = false;
        (MdiParent as frmMain)?.ClearAutosaveFailure(this);
        GlobalOptions.Instance.AddToMRUList(RepositoryLocalFile);
        UpdateWindowTitle(false);
    }
}
