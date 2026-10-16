#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeGearEligibilityChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType()
        .GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
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
                Field<Forms.TextBox>(picker, "txtSearch").Text = excluded;
                await Task.Delay(20);
                check(Field<Forms.ListBox>(picker, "lstGear").Items.Count == 0 && !Field<Forms.Button>(picker, "cmdOK").Enabled,
                    "Native gear " + mode + " search excludes incompatible purchases");
                Field<Forms.TextBox>(picker, "txtSearch").Text = included;
                var list = Field<Forms.ListBox>(picker, "lstGear");
                list.SelectedIndex = list.Items.Cast<ListItem>().Select((item, index) => (item, index))
                    .First(pair => pair.item.Value.StartsWith(included + "^", StringComparison.Ordinal)).index;
                Field<Forms.Button>(picker, "cmdOK").PerformClick();
                check(await pending == Forms.DialogResult.OK && picker.SelectedGear == included,
                    "Native gear " + mode + " search accepts an eligible canonical choice");
            }
        }
        finally { character.Options.Books.Clear(); character.Options.Books.AddRange(books); }
        if (!character.CaptureSnapshot().SequenceEqual(original)) throw new InvalidOperationException("Gear eligibility check changed its source character.");
    }
}
