#nullable enable
using System;
using System.Reflection;
using Forms = Majorsilence.Forms;
namespace Chummer;
// Characterize the legacy monetary boundary before replacing its presentation.
internal static class ExpensePromptChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    internal static void Run(Action<bool, string> check)
    {
        using (var prompt = new frmExpense { Mode = ExpenseType.Nuyen, Amount = 101 })
        {
            var percent = Field<Forms.NumericUpDown>(prompt, "nudPercent"); percent.Value = 50;
            check(prompt.Amount == 50, "expense percentage preserves midpoint-to-even rounding below odd boundary");
            prompt.Amount = 103;
            check(prompt.Amount == 52, "expense percentage preserves midpoint-to-even rounding above odd boundary");
            percent.Value = 0; check(prompt.Amount == 0, "expense percentage supports a zero result");
            percent.Value = 1000; check(prompt.Amount == 1030, "expense percentage supports the historical thousand-percent limit");
            prompt.Mode = ExpenseType.Karma;
            check(prompt.Amount == 103, "Karma expense ignores the Nuyen percentage field");
            var selected = new DateTime(2072, 3, 15, 17, 26, 43, DateTimeKind.Local);
            prompt.SelectedDate = selected; prompt.strReason = "Synthetic expense"; prompt.Refund = true;
            check(prompt.SelectedDate == selected && prompt.strReason == "Synthetic expense" && prompt.Refund,
                "expense editing preserves configured date time reason and refund");
            prompt.LockFields();
            check(!Field<Forms.NumericUpDown>(prompt, "nudAmount").Enabled && !percent.Enabled && !Field<Forms.CheckBox>(prompt, "chkRefund").Enabled,
                "locked automatic expense permits only date and reason editing");
        }
        using (var prompt = new frmExpense { Amount = -3 })
        {
            prompt.LockFields(true); var amount = Field<Forms.NumericUpDown>(prompt, "nudAmount");
            check(amount.Enabled && amount.Minimum == -9999999 && amount.Maximum == 9999999 && prompt.Amount == -3,
                "manual negative expense editing preserves the symmetric historical range");
        }
    }
}
