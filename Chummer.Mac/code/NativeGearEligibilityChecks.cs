#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeGearEligibilityChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character character, Action<bool, string> check)
    {
        byte[] original = character.CaptureSnapshot();
        var books = character.Options.Books.ToArray();
        try
        {
            if (!character.Options.Books.Contains("AR")) character.Options.Books.Add("AR");
            foreach (string mode in new[] { "hardware", "positive capacity", "installed capacity" })
            {
                using var picker = mode == "hardware"
                    ? new frmSelectGear(character) { AllowedCategories = "Commlink Upgrade", CommlinkResponse = 3, CommlinkSignal = 3 }
                    : new frmSelectGear(character) { AllowedCategories = "Electronics and Sensor Options",
                        ShowPositiveCapacityOnly = mode == "positive capacity", ShowNegativeCapacityOnly = mode == "installed capacity" };
                string excluded = mode == "hardware" ? "Response 1" : mode == "positive capacity" ? "Self-Destruct, Area Effect" : "Skinlink";
                string included = mode == "hardware" ? "Response 4" : mode == "positive capacity" ? "Skinlink" : "Self-Destruct, Area Effect";
                var pending = picker.ShowDialogAsync(owner);
                var window = (GearPickerWindow)NativeDialogHost.ActiveWindow!;
                window.SearchBox.Text = mode == "hardware" ? "comlink" : "Electronics and Sensor Options";
                await Task.Delay(20);
                check(window.Model.Items.Any(item => item.Name == included) &&
                    !window.Model.Items.Any(item => item.Name == excluded),
                    "Native gear " + mode + " category search preserves eligibility");
                window.SearchBox.Text = excluded;
                await Task.Delay(20);
                check(window.Model.Items.Count == 0 && !window.AcceptButton.IsEnabled,
                    "Native gear " + mode + " search excludes incompatible purchases");
                window.SearchBox.Text = included;
                window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == included);
                window.Accept(false);
                check(await pending == Forms.DialogResult.OK && picker.SelectedGear == included,
                    "Native gear " + mode + " search accepts an eligible canonical choice");
            }
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(books); }
        if (!character.CaptureSnapshot().SequenceEqual(original)) throw new InvalidOperationException("Gear eligibility check changed its source character.");
    }
}
