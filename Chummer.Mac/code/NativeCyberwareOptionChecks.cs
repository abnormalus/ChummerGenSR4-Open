#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeCyberwareOptionChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, Action<bool,string> check)
    {
        async Task ExpectAsync(Func<bool> condition, string label)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) { check(false, label); throw new TimeoutException("Native Bioware option assertion timed out."); }
                await Task.Delay(20);
            }
            check(true, label);
        }
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = career;
            if (!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
            character.Options.AllowCustomTransgenics = true;
            byte[] before = character.CaptureSnapshot();
            using var dialog = new frmSelectCyberware(character, career) { WindowMode = frmSelectCyberware.Mode.Bioware, SetGrade = "Standard" };
            var pending = dialog.ShowDialogAsync(owner); var window = (CyberwarePickerWindow)NativeDialogHost.ActiveWindow!;
            try
            {
                T Input<T>(string name) where T : Control => window.GetVisualDescendants().OfType<T>().Single(input => input.Name == name);
                await ExpectAsync(() => window.IsVisible && !Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled,
                    "Native Bioware options opens owned window career=" + career);
                window.Model.Category = window.Model.Categories.Single(category => category.Code == "Basic");
                window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == "Muscle Toner");
                var grade = Input<ComboBox>("cyberwarePickerGrade");
                grade.SelectedItem = window.Model.Grades.First(item => item.SecondHand);
                await ExpectAsync(() => window.Model.SelectedGrade?.SecondHand == true, "Native Bioware grade selector binds secondhand choice career=" + career);
                window.SearchBox.Text = "Cerebral Booster";
                await ExpectAsync(() => window.Model.SelectedEntry?.Category == "Cultured" && !window.Model.Grades.Any(item => item.SecondHand) &&
                    window.Model.SelectedGrade?.Name == "Standard", "Native Bioware Cultured search removes secondhand grade career=" + career);
                window.SearchBox.Text = "Muscle Toner";
                window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == "Muscle Toner");
                var transgenic = Input<CheckBox>("cyberwarePickerTransgenic"); var free = Input<CheckBox>("cyberwarePickerFreeCost");
                transgenic.IsChecked = true;
                await ExpectAsync(() => window.Model.Transgenic && !grade.IsEnabled && window.Model.SelectedGrade?.Name == "Standard" && free.IsVisible,
                    "Native Bioware transgenic checkbox forces Standard and exposes Free career=" + career);
                free.IsChecked = true;
                await ExpectAsync(() => window.Model.FreeCost && window.Model.Details?.Cost == string.Format("{0:###,###,##0¥}",0),
                    "Native Bioware Free checkbox binds zero-cost preview career=" + career);
                transgenic.IsChecked = false;
                await ExpectAsync(() => !window.Model.Transgenic && grade.IsEnabled && free.IsVisible == career && window.Model.FreeCost == career,
                    "Native Bioware leaving transgenic normalizes contextual Free career=" + career);
                transgenic.IsChecked = true; free.IsChecked = true;
                window.AcceptButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await ExpectAsync(() => pending.IsCompleted, "Native Bioware OK completes owned modal career=" + career);
                check(await pending == Forms.DialogResult.OK && dialog.SelectedCyberware == "Muscle Toner" && dialog.SelectedCategory == "Basic" &&
                    dialog.ForceTransgenic && dialog.FreeCost && !dialog.AddAgain && dialog.SelectedGrade?.Name == "Standard" &&
                    Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled && character.CaptureSnapshot().SequenceEqual(before),
                    "Native Bioware OK publishes transgenic Free canonical draft with unchanged snapshot career=" + career);
            }
            finally { if (!pending.IsCompleted) window.Close(); }
        }
    }
}
