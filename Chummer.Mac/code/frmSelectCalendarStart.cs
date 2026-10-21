#nullable enable
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;

namespace Chummer;

// This is Chummer's existing 4/4/5-week calendar, not Gregorian date arithmetic.
internal sealed record CalendarStartSelection(int Year, int Week)
{
    internal static int WeeksInMonth(int month) => month % 3 == 0 ? 5 : 4;
    internal static CalendarStartSelection FromMonth(int year, int month, int week)
    {
        if (month < 1 || month > 12) throw new ArgumentOutOfRangeException(nameof(month));
        if (week < 1 || week > WeeksInMonth(month)) throw new ArgumentOutOfRangeException(nameof(week));
        return new(year, (month - 1) * 4 + (month - 1) / 3 + week);
    }
}
public sealed class frmSelectCalendarStart : IDisposable
{
    private readonly int year, month, week;
    private CalendarStartWindow? window;
    public int SelectedYear { get; private set; } = 2072;
    public int SelectedWeek { get; private set; } = 1;
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    public frmSelectCalendarStart() : this(new CalendarWeek()) { }
    public frmSelectCalendarStart(CalendarWeek initial) { year = initial.Year; month = initial.Month; week = initial.MonthWeek; }
    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Calendar selection is already open.");
        DialogResult = Forms.DialogResult.Cancel;
        window = new CalendarStartWindow(year, month, week);
        try
        {
            var accepted = await NativeDialogHost.ShowAsync<CalendarStartSelection>(window, owner);
            if (accepted != null) { SelectedYear = accepted.Year; SelectedWeek = accepted.Week; DialogResult = Forms.DialogResult.OK; }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() => window?.Close();
}
internal sealed class CalendarStartWindow : Window
{
    internal NumericUpDown YearInput { get; } = NativeIntegerInput.Create("calendarYear", 2000, 9000);
    internal NumericUpDown MonthInput { get; } = NativeIntegerInput.Create("calendarMonth", 1, 12);
    internal NumericUpDown WeekInput { get; } = NativeIntegerInput.Create("calendarWeek", 1, 4);
    internal CalendarStartWindow(int year, int month, int week)
    {
        Title = LanguageManager.Instance.GetString("Title_CalendarStart");
        Width = 480; MinWidth = 350; SizeToContent = SizeToContent.Height; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);
        YearInput.Value = Math.Clamp(year, 2000, 9000); MonthInput.Value = Math.Clamp(month, 1, 12);
        // Set the limit before the draft so a quarter's fifth week survives loading.
        UpdateWeekLimit(); WeekInput.Value = Math.Clamp(week, 1, (int)WeekInput.Maximum);
        MonthInput.ValueChanged += (_, _) => UpdateWeekLimit();
        var layout = new StackPanel { Margin = new Thickness(20), Spacing = 14 };
        layout.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString("Label_CalendarStart"), TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        AddField(layout, "Label_Year", YearInput); AddField(layout, "Label_Month", MonthInput); AddField(layout, "Label_Week", WeekInput);
        var cancel = new Button { Content = LanguageManager.Instance.GetString("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        var accept = new Button { Content = LanguageManager.Instance.GetString("String_OK"), IsDefault = true }; accept.Click += (_, _) => Accept();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        buttons.Children.Add(cancel); buttons.Children.Add(accept); layout.Children.Add(buttons); Content = layout;
        Opened += (_, _) => YearInput.Focus();
    }
    private static void AddField(StackPanel layout, string label, NumericUpDown input)
    {
        var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        var text = new TextBlock { Text = LanguageManager.Instance.GetString(label), VerticalAlignment = VerticalAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        row.Children.Add(text); Grid.SetColumn(input, 1); row.Children.Add(input); layout.Children.Add(row);
    }
    private void UpdateWeekLimit() => WeekInput.Maximum = CalendarStartSelection.WeeksInMonth((int)(MonthInput.Value ?? 1));
    internal void Accept() => Close(CalendarStartSelection.FromMonth((int)(YearInput.Value ?? 2000), (int)(MonthInput.Value ?? 1), (int)(WeekInput.Value ?? 1)));
}
