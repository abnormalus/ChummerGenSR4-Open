#nullable enable
using System;
namespace Chummer;
// Keep the independently characterized legacy monetary/date boundaries while
// native interaction checks cover controls and actual career transactions.
internal static class ExpensePromptChecks
{
    internal static void Run(Action<bool, string> check)
    {
        var date = new DateTime(2072, 3, 15, 17, 26, 43, DateTimeKind.Local).AddTicks(123);
        var selection = new ExpenseSelection(101, 50, ExpenseType.Nuyen, "Synthetic expense", true, date);
        check(selection.Amount == 50, "expense percentage preserves midpoint-to-even rounding below odd boundary");
        selection = selection with { RawAmount = 103 };
        check(selection.Amount == 52, "expense percentage preserves midpoint-to-even rounding above odd boundary");
        check((selection with { Percent = 0 }).Amount == 0, "expense percentage supports a zero result");
        check((selection with { Percent = 1000 }).Amount == 1030, "expense percentage supports the historical thousand-percent limit");
        check((selection with { Mode = ExpenseType.Karma }).Amount == 103, "Karma expense ignores the Nuyen percentage field");
        using (var prompt = new frmExpense { SelectedDate = date, strReason = "Synthetic expense", Refund = true })
        {
            check(prompt.SelectedDate == date && prompt.SelectedDate.Kind == date.Kind && prompt.strReason == "Synthetic expense" && prompt.Refund,
                "expense editing preserves configured date time reason and refund");
            prompt.LockFields();
            check(!prompt.Settings.AmountEditable && !prompt.Settings.PercentEditable && !prompt.Settings.RefundEditable,
                "locked automatic expense permits only date and reason editing");
        }
        using (var prompt = new frmExpense { Amount = -3 })
        {
            prompt.LockFields(true);
            check(prompt.Settings.AmountEditable && prompt.Settings.Minimum == -9999999 && prompt.Amount == -3,
                "manual negative expense editing preserves the symmetric historical range");
        }
        check(ExpenseDateTime.TryCompose(date, date.Date, date.TimeOfDay, true, out var unchanged) && unchanged == date && unchanged.Kind == date.Kind,
            "expense date composition preserves untouched hidden seconds ticks and kind");
        check(ExpenseDateTime.TryCompose(date, date.AddDays(1).Date, TimeSpan.Zero, false, out var hidden) && hidden.TimeOfDay == date.TimeOfDay && hidden.Day == 16,
            "date-only expense editing preserves hidden original time");
        check(ExpenseDateTime.TryCompose(date, date.Date, new TimeSpan(9, 30, 0), true, out var changed) && changed.Hour == 9 && changed.Minute == 30 && changed.Second == 43 && changed.Ticks % TimeSpan.TicksPerSecond == 123,
            "expense minute editing preserves undisplayed seconds and fractional ticks");
        check(!ExpenseDateTime.TryCompose(date, null, date.TimeOfDay, true, out _) && !ExpenseDateTime.TryCompose(date, date.Date, null, true, out _),
            "expense date composition rejects incomplete visible date or time");
        check(ExpenseDateTime.TryCompose(ExpenseDateTime.Maximum, ExpenseDateTime.Maximum, TimeSpan.Zero, true, out _) && !ExpenseDateTime.TryCompose(ExpenseDateTime.Maximum, ExpenseDateTime.Maximum, TimeSpan.FromMinutes(1), true, out _),
            "expense maximum date retains the legacy midnight upper bound");
        check(ExpenseDateTime.TryCompose(ExpenseDateTime.Minimum, ExpenseDateTime.Minimum, TimeSpan.Zero, true, out _) && !ExpenseDateTime.TryCompose(date, ExpenseDateTime.Minimum.AddDays(-1), TimeSpan.Zero, true, out _),
            "expense minimum date retains the legacy lower bound");
    }
}
