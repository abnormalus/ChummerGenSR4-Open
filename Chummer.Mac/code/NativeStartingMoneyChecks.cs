#nullable enable
using System;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeStartingMoneyChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool, string> check)
    {
        using (var prompt = new frmLifestyleNuyen { Dice = 4, Extra = 12, Multiplier = 100 })
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (StartingMoneyWindow)NativeDialogHost.ActiveWindow!;
            check(window.RollInput.Minimum == 4 && window.RollInput.Maximum == 24 && window.RollInput.Value == 4 && prompt.StartingNuyen == 0,
                "Native starting-money draft preserves catalog dice bounds without applying money");
            window.RollInput.Value = 20;
            check(window.Amount == 3200 && window.ResultText.Text?.Contains("3") == true, "Native starting-money preview includes roll bonus and multiplier");
            var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var priorTheme = nativeOwner.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    nativeOwner.RequestedThemeVariant = theme; await Task.Delay(100);
                    check(window.ActualThemeVariant == theme && window.RollInput.Bounds.Width >= 110,
                        "Native starting-money follows " + (theme == ThemeVariant.Dark ? "dark" : "light") + " owner theme");
                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192));
                    bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-starting-money-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { nativeOwner.RequestedThemeVariant = priorTheme; }
            window.Accept();
            check(await pending == Forms.DialogResult.OK && prompt.StartingNuyen == 3200, "Native starting-money returns accepted calculated amount");
        }
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot());
        character.BuildMethod = CharacterBuildMethod.BP; character.IgnoreRules = true;
        character.Nuyen = 123456; character.Lifestyles.Clear();
        using var editor = new frmCreate(character) { Loading = true, MdiParent = owner }; editor.Show();
        var closed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (NativeDialogHost.ActiveWindow is not StartingMoneyWindow)
                {
                    if (DateTime.UtcNow >= deadline) throw new TimeoutException("Starting-money prompt did not open.");
                    await Task.Delay(10);
                }
                NativeDialogHost.ActiveWindow!.Close(); closed.TrySetResult();
            }
            catch (Exception error) { closed.TrySetException(error); NativeDialogHost.CancelAll(); }
        });
        try
        {
            bool valid = editor.ValidateCharacter(); await closed.Task;
            check(!valid && character.Nuyen == 123456 && character.Lifestyles.Count == 0 && !character.Created,
                "Native starting-money cancellation rejects finalization without replacing money or adding lifestyle");
            async Task WaitForAsync(Func<bool> ready)
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (!ready()) { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Creation confirmation did not open."); await Task.Delay(10); }
            }
            Task Reply(Forms.DialogResult answer, bool cancelMoney = false)
            {
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
                    try
                    {
                        await WaitForAsync(() => Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any());
                        var message = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last(); message.DialogResult = answer; message.Close();
                        if (cancelMoney) { await WaitForAsync(() => NativeDialogHost.ActiveWindow is StartingMoneyWindow); NativeDialogHost.ActiveWindow!.Close(); }
                        completion.TrySetResult();
                    }
                    catch (Exception error) { NativeDialogHost.CancelAll(); completion.TrySetException(error); }
                });
                return completion.Task;
            }
            foreach (var answer in new[] { Forms.DialogResult.No, Forms.DialogResult.Cancel })
            {
                var replied = Reply(answer); bool confirmed = editor.ConfirmSaveCreatedCharacter(); await replied;
                check(!confirmed && !character.Created && character.Nuyen == 123456 && character.Lifestyles.Count == 0,
                    "Native creation confirmation rejects " + answer + " without starting-money changes");
            }
            var yesReply = Reply(Forms.DialogResult.Yes, cancelMoney: true); bool finalization = editor.ConfirmSaveCreatedCharacter(); await yesReply;
            check(!finalization && !character.Created && character.Nuyen == 123456 && character.Lifestyles.Count == 0,
                "Native creation Yes followed by roll cancellation preserves creation state");
            character.BuildMethod = CharacterBuildMethod.Karma; character.BuildKarma = 100000; character.Karma = 17;
            foreach (var answer in new[] { Forms.DialogResult.No, Forms.DialogResult.Cancel })
            {
                var replied = Reply(answer); bool karmaValid = editor.ValidateCharacter(); await replied;
                check(!karmaValid && character.Karma == 17 && character.Nuyen == 123456 && character.Lifestyles.Count == 0,
                    "Native Karma carryover rejects " + answer + " without applying finalization");
            }
            var karmaReply = Reply(Forms.DialogResult.Yes, cancelMoney: true); bool karmaCancelled = editor.ValidateCharacter(); await karmaReply;
            check(!karmaCancelled && character.Karma == 17 && character.Nuyen == 123456 && character.Lifestyles.Count == 0,
                "Native accepted Karma warning followed by roll cancellation preserves Karma and money");
            character.BuildMethod = CharacterBuildMethod.BP;
            int expectedMoney = 0;
            Avalonia.Threading.Dispatcher.UIThread.Post(() => { var prompt = (StartingMoneyWindow)NativeDialogHost.ActiveWindow!; prompt.RollInput.Value = prompt.RollInput.Maximum; expectedMoney = Math.Max(0, prompt.Amount); prompt.Accept(); });
            bool accepted = editor.ValidateCharacter();
            check(accepted && character.Lifestyles.Count == 1 && character.Lifestyles[0].Name == "Street" && character.Nuyen == expectedMoney && !character.Created,
                "Native starting-money retry commits one fallback lifestyle and accepted catalog roll");
        }
        finally { editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false); editor.Close(); }
    }
}
