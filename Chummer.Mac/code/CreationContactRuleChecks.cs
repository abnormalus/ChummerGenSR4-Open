using System;
using System.Linq;
using System.Reflection;
using System.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CreationContactRuleChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags).GetValue(owner);
    private static void Clean(frmCreate editor) => typeof(frmCreate).GetField("_blnIsDirty", Flags).SetValue(editor, false);
    private static int WithRuleNotice(Action edit)
    {
        int notices = 0; bool expired = false;
        var backend = Forms.Backends.Platform.Backend;
        // Post through the public headless dispatcher. The original blocking
        // modal pumps this reply only after the actual notice has been shown.
        backend.Post(() =>
        {
            var dialog = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().LastOrDefault();
            if (dialog == null) throw new InvalidOperationException("Expected contact rule notice did not open.");
            notices++; dialog.DialogResult = Forms.DialogResult.OK; dialog.Close();
        });
        using var deadline = new CancellationTokenSource(TimeSpan.FromSeconds(20));
        using var cancel = deadline.Token.Register(() => backend.Post(() =>
        {
            expired = true;
            foreach (var dialog in Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().ToArray()) dialog.Close();
        }));
        edit();
        if (expired) throw new TimeoutException("Contact rule notice exceeded its operation deadline.");
        return notices;
    }
    internal static void Run(Character source, Action<bool, string> check)
    {
        foreach (var method in new[] { CharacterBuildMethod.BP, CharacterBuildMethod.Karma })
        foreach (bool qualityLimit in new[] { false, true })
        {
            var fixture = CreationContactRuleFixture.Create(source, method, qualityLimit);
            var originalModel = new Character(); originalModel.LoadSnapshot(fixture);
            var nativeModel = new Character(); nativeModel.LoadSnapshot(fixture);
            bool exceed = nativeModel.Options.ExceedNegativeQualities;
            using var original = new frmCreate(originalModel) { Loading = true };
            using var editor = new frmCreate(nativeModel) { Loading = true };
            try
            {
                nativeModel.Options.ExceedNegativeQualities = false;
                original.Show(); editor.Show(); original.Loading = editor.Loading = false;
                editor.InitializeCreationContactPresentation(attachPresenter: false);
                var canonical = original.CreationEnemyControllers.Last();
                var adapted = editor.CreationEnemyControllers.Last();
                var input = editor.ContactPresentation(true).Rows[adapted].Connection;
                int notices = WithRuleNotice(() => canonical.CreationConnectionInput.Value = 5);
                notices += WithRuleNotice(() => input.Value = 5);
                string label = " method=" + method + " qualityLimit=" + qualityLimit;
                check(notices == 2 && adapted.ContactObject.Connection == canonical.ContactObject.Connection
                    && input.Value == adapted.CreationConnectionInput.Value && editor.Enabled,
                    "Native-adapted enemy limit restores the canonical input after the actual rule notice" + label);
                check(nativeModel.CaptureSnapshot().SequenceEqual(originalModel.CaptureSnapshot())
                    && Field<Forms.Label>(editor, "lblEnemiesBP").Text == Field<Forms.Label>(original, "lblEnemiesBP").Text,
                    "Native-adapted enemy and negative-quality limit retains full snapshot and accounting parity" + label);
            }
            finally
            {
                nativeModel.Options.ExceedNegativeQualities = exceed;
                Clean(original); Clean(editor); original.Close(); editor.Close();
            }
        }
    }
}
