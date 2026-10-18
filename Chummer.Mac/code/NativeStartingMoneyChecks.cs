#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeStartingMoneyChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot());
        character.BuildMethod = CharacterBuildMethod.BP; character.IgnoreRules = true;
        character.Nuyen = 123456; character.Lifestyles.Clear();
        using var editor = new frmCreate(character) { Loading = true, MdiParent = owner }; editor.Show();
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (!Forms.Application.OpenForms.OfType<frmLifestyleNuyen>().Any())
                {
                    if (DateTime.UtcNow >= deadline) throw new TimeoutException("Starting-money prompt did not open.");
                    await Task.Delay(10);
                }
                var prompt = Forms.Application.OpenForms.OfType<frmLifestyleNuyen>().Last();
                prompt.DialogResult = Forms.DialogResult.Cancel; prompt.Close(); closed.TrySetResult();
            }
            catch (Exception error) { closed.TrySetException(error); Forms.Application.OpenForms.OfType<frmLifestyleNuyen>().LastOrDefault()?.Close(); }
        });
        try
        {
            bool valid = editor.ValidateCharacter(); await closed.Task;
            check(!valid && character.Nuyen == 123456 && character.Lifestyles.Count == 0 && !character.Created,
                "Native starting-money cancellation rejects finalization without replacing money or adding lifestyle");
        }
        finally { editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false); editor.Close(); }
    }
}
