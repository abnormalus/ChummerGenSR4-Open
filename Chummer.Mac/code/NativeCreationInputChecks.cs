#nullable enable
using System;
using System.Linq;
using System.IO;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Avalonia.Input;
using Avalonia.VisualTree;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeCreationInputChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
    private static async Task WithRuleNotice(Action change)
    {
        var dismissed = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () =>
        {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Creation rule notice did not open."); await Task.Delay(10); }
                var notice = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last();
                notice.DialogResult = Forms.DialogResult.OK; notice.Close(); dismissed.TrySetResult();
            }
            catch (Exception error) { dismissed.TrySetException(error); }
        });
        change(); await dismissed.Task;
    }
    internal static async Task RunAsync(Forms.Form owner, Character source, string output, Action<bool, string> check)
    {
        var character = new Character(); character.LoadSnapshot(source.CaptureSnapshot()); character.Created = false; character.IgnoreRules = true;
        using var editor = new frmCreate(character) { MdiParent = owner, Loading = true };
        editor.Show(); await Task.Delay(20);
        bool previousBudgetOption = character.Options.AllowExceedAttributeBP;
        try
        {
            var section = Field<CreationInputSection>(editor, "creationInputSection");
            check(section.Inputs.Count == 12 && section.Inputs.Values.All(input => input.GetType() == typeof(Avalonia.Controls.NumericUpDown)),
                "Native creation batch exposes eleven attribute inputs and one money allocation input");
            check(section.Inputs.Keys.All(name => Field<Forms.NumericUpDown>(editor, name).Parent == null),
                "Native creation inputs retain detached controller state without drawing legacy spinners");
            var application = Avalonia.Application.Current!;
            var originalTheme = application.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    application.RequestedThemeVariant = theme;
                    await Task.Delay(120);
                    var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
                    using var image = new RenderTargetBitmap(new PixelSize((int)Math.Ceiling(window.ClientSize.Width * 2),
                        (int)Math.Ceiling(window.ClientSize.Height * 2)), new Vector(192, 192));
                    image.Render(window);
                    image.Save(Path.Combine(output, "native-creation-inputs-" + theme.Key.ToString()!.ToLowerInvariant() + ".png"), PngBitmapEncoderOptions.Default);
                    check(section.Inputs.Values.Any(input => input.IsVisible) && section.Inputs.Values.Where(input => input.IsVisible).All(input => input.Bounds.Width > 0 && input.Bounds.Height >= 20),
                        "Native creation input batch has measured controls in " + theme.Key);
                }
            }
            finally { application.RequestedThemeVariant = originalTheme; }
            var body = Field<Forms.NumericUpDown>(editor, "nudBOD"); var input = section.Inputs["nudBOD"];
            editor.Loading = true; body.Minimum = 1; body.Maximum = 10; body.Value = 2;
            check(input.Minimum == 1 && input.Maximum == 10 && input.Value == 2,
                "Native creation attributes follow controller loading bounds and values");
            editor.Loading = false;
            int before = character.BOD.Value; input.Value = null;
            check(character.BOD.Value == before && body.Value == 2,
                "Cleared inline creation input does not commit a fallback value");
            var textEditor = input.GetVisualDescendants().OfType<Avalonia.Controls.TextBox>().Single();
            textEditor.Focus();
            section.Inputs["nudNuyen"].Focus(); await Task.Delay(20);
            check(input.Value == body.Value && character.BOD.Value == before,
                "Leaving an incomplete creation input restores controller state without committing a fallback");
            byte[] beforeFractionalEdit = character.CaptureSnapshot();
            decimal approvedAttribute = body.Value;
            bool dirtyBeforeFractionalEdit = Field<bool>(editor, "_blnIsDirty");
            input.Value = approvedAttribute + 0.5m;
            check(input.ParsingNumberStyle == System.Globalization.NumberStyles.Integer &&
                input.Value == approvedAttribute && body.Value == approvedAttribute &&
                character.CaptureSnapshot().SequenceEqual(beforeFractionalEdit) &&
                Field<bool>(editor, "_blnIsDirty") == dirtyBeforeFractionalEdit,
                "Native fractional creation attribute edit preserves character and dirty state");
            input.Text = "3"; await Task.Delay(30);
            check(body.Value == 3 && character.BOD.Value == 3 && Field<bool>(editor, "_blnIsDirty"),
                "Native creation attribute edit uses the existing controller and dirty-state path");
            textEditor.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Up }); await Task.Delay(20);
            check(input.Value == 4 && body.Value == 4 && character.BOD.Value == 4,
                "Native creation keyboard step follows existing attribute rules and model updates");
            var infoTabs = Field<Forms.TabControl>(editor, "tabInfo"); var common = Field<Forms.TabPage>(editor, "tabCommon");
            infoTabs.SelectedTab = Field<Forms.TabPage>(editor, "tabCharacterInfo"); await Task.Delay(20);
            bool hidden = !input.IsEffectivelyVisible;
            infoTabs.SelectedTab = common; await Task.Delay(20);
            check(hidden && input.IsEffectivelyVisible,
                "Native creation input section hides and restores with its Common tab");
            using (var prompt = new frmSelectNumber { Minimum = 0, Maximum = 10, Description = "Synthetic creation ownership check" })
            {
                var pending = prompt.ShowDialogAsync(editor); await Task.Delay(20);
                check(!input.IsEffectivelyEnabled,
                    "Native creation inputs respect modal owner blocking");
                NativeDialogHost.ActiveWindow!.Close(); await pending;
                check(input.IsEffectivelyEnabled,
                    "Native creation inputs resume after modal cancellation");
            }
            var money = section.Inputs["nudNuyen"]; money.Value = 2.5m;
            check(character.NuyenBP == 2.5m && Field<Forms.NumericUpDown>(editor, "nudNuyen").Value == 2.5m,
                "Native money allocation preserves decimal model values through existing accounting");
            editor.Loading = true; body.Maximum = 2;
            check(input.Maximum == 2 && input.Value == body.Value && body.Value == 2,
                "Native attribute refresh follows controller range clipping without a stale value");
            editor.Loading = false; input.Value = 1;
            var reopened = new Character(); reopened.LoadSnapshot(character.CaptureSnapshot());
            check(reopened.BOD.Value == character.BOD.Value && reopened.NuyenBP == character.NuyenBP,
                "Native creation attribute and money edits retain the unchanged character serialization contract");
            body.Enabled = false;
            decimal disabledValue = body.Value; int disabledModel = character.BOD.Value;
            input.Value = disabledValue == body.Minimum ? disabledValue + 1 : disabledValue - 1;
            check(body.Value == disabledValue && character.BOD.Value == disabledModel && input.Value == disabledValue,
                "Native disabled creation attribute rejects queued edits without changing the character");
            body.Enabled = true;
            character.BuildMethod = CharacterBuildMethod.BP;
            character.BuildPoints = 2000; character.IgnoreRules = false; character.Options.AllowExceedAttributeBP = false;
            typeof(frmCreate).GetField("_blnFreestyle", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
            void ResetPrimary()
            {
                editor.Loading = true;
                foreach (string name in new[] { "BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL" })
                { var state = Field<Forms.NumericUpDown>(editor, "nud" + name); state.Value = state.Minimum; }
                editor.Loading = false;
            }
            ResetPrimary(); editor.Loading = true;
            var agility = Field<Forms.NumericUpDown>(editor, "nudAGI"); agility.Value = agility.Maximum;
            editor.Loading = false;
            await WithRuleNotice(() => input.Value = body.Maximum);
            check(body.Value == body.Maximum - 1 && input.Value == body.Value,
                "Native creation edit retains the one-primary-attribute-at-maximum rule");
            character.IgnoreRules = true; input.Value = body.Maximum;
            check(input.Value == body.Value && body.Value == body.Maximum,
                "Native creation Ignore Rules permits an otherwise rejected maximum");
            character.IgnoreRules = false; ResetPrimary();
            character.BuildPoints = character.Options.BPAttribute * 2;
            decimal firstPaidStep = body.Minimum + 1;
            await WithRuleNotice(() => input.Value = body.Minimum + 2);
            check(body.Value == firstPaidStep && input.Value == firstPaidStep,
                "Native creation edit retains the half-BP primary attribute budget and controller correction");
            character.IgnoreRules = true; character.BuildMethod = CharacterBuildMethod.Karma; money.Value = 3.5m;
            int expectedMoney = Convert.ToInt32(3.5m) * character.Options.KarmaNuyenPer +
                Convert.ToInt32(new ImprovementManager(character).ValueOf(Improvement.ImprovementType.Nuyen));
            check(character.NuyenBP == 3.5m && Field<Forms.Label>(editor, "lblNuyenTotal").Text == string.Format("= {0:###,###,##0¥}", expectedMoney),
                "Native Karma money allocation follows existing conversion and improvement accounting");
        }
        finally { character.Options.AllowExceedAttributeBP = previousBudgetOption; typeof(frmCreate).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false); editor.Close(); }
        var closedSection = Field<CreationInputSection>(editor, "creationInputSection"); decimal savedMoney = character.NuyenBP;
        closedSection.Inputs["nudNuyen"].Value = savedMoney == 1 ? 2 : 1;
        check(character.NuyenBP == savedMoney,
            "Closed creation editor rejects queued native input changes");
    }
}
