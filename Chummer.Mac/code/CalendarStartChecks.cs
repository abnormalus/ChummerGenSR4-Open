#nullable enable
using System;
namespace Chummer;
internal static class CalendarStartChecks
{
    internal static void Run(Action<bool, string> check)
    {
        bool compatible = true;
        for (int week = 1; week <= 52; week++)
        {
            var legacy = new CalendarWeek(2072, week);
            var selected = CalendarStartSelection.FromMonth(legacy.Year, legacy.Month, legacy.MonthWeek);
            compatible &= selected.Year == 2072 && selected.Week == week;
        }
        check(compatible, "Native calendar selection preserves all 52 legacy month/week mappings");
        check(CalendarStartSelection.FromMonth(2072, 3, 5).Week == 13 && CalendarStartSelection.FromMonth(2072, 12, 5).Week == 52,
            "Native calendar quarter fifth weeks preserve year boundaries");
    }
}
