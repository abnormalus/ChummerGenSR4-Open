#nullable enable
using System;
using System.Globalization;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal sealed record ExpenseSelection(int RawAmount, int Percent, ExpenseType Mode, string Reason, bool Refund, DateTime Date)
{
    // Retain the legacy decimal percentage and midpoint-to-even monetary boundary.
    internal int Amount => Convert.ToInt32(Math.Round(Mode == ExpenseType.Nuyen ? RawAmount * ((decimal)Percent / 100) : RawAmount, 0, MidpointRounding.ToEven));
}
internal sealed record ExpenseEditSettings(int Minimum, bool Locked, bool EditAmount, bool ShowTime)
{
    internal bool AmountEditable => !Locked || EditAmount;
    internal bool PercentEditable => !Locked;
    internal bool RefundEditable => !Locked;
}
internal static class ExpenseDateTime
{
    internal static readonly DateTime Minimum = new(1753, 1, 1);
    internal static readonly DateTime Maximum = new(9998, 12, 31);
    internal static bool InRange(DateTime date) => date >= Minimum && date <= Maximum;
    internal static bool TryCompose(DateTime original, DateTime? date, TimeSpan? time, bool showTime, out DateTime result)
    {
        result = original;
        if (date == null || (showTime && (time == null || time < TimeSpan.Zero || time >= TimeSpan.FromDays(1)))) return false;
        // Seconds/fractional ticks are not exposed by the minute editor. Preserve
        // them, the original kind, and all hidden time when only a date is shown.
        var selectedTime = showTime ? TimeSpan.FromHours(time!.Value.Hours) + TimeSpan.FromMinutes(time.Value.Minutes) + TimeSpan.FromTicks(original.Ticks % TimeSpan.TicksPerMinute) : original.TimeOfDay;
        result = DateTime.SpecifyKind(date.Value.Date + selectedTime, original.Kind);
        return InRange(result);
    }
}
public sealed class frmExpense : IDisposable
{
    private int rawAmount = 1, minimum = 1;
    private string reason = LanguageManager.Instance.GetString("String_ExpenseDefault");
    private bool refund, locked, editAmount;
    private DateTime date = DateTime.Now;
    private ExpenseType mode = ExpenseType.Karma;
    private ExpenseSelection? accepted;
    private ExpenseWindow? window;
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    public int Amount
    {
        get => (accepted ?? Draft).Amount;
        set { if (value > 9999999) throw new ArgumentOutOfRangeException(nameof(value)); if (value <= 0) minimum = value; rawAmount = value; }
    }
    public string strReason { get => accepted?.Reason ?? reason; set => reason = value; }
    public bool Refund { get => accepted?.Refund ?? refund; set => refund = value; }
    public DateTime SelectedDate
    {
        get => accepted?.Date ?? date;
        set { if (!ExpenseDateTime.InRange(value)) throw new ArgumentOutOfRangeException(nameof(value)); date = value; }
    }
    public ExpenseType Mode { set => mode = value; }
    internal ExpenseSelection Draft => new(rawAmount, 100, mode, reason, refund, date);
    internal ExpenseEditSettings Settings => new(minimum, locked, editAmount, GlobalOptions.Instance.DatesIncludeTime);
    public void LockFields(bool blnEditAmount = false)
    {
        locked = true; editAmount = blnEditAmount;
        if (blnEditAmount && minimum < 0) { minimum = -9999999; rawAmount = Math.Max(rawAmount, minimum); }
    }
    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending); return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Expense editor is already open.");
        accepted = null; DialogResult = Forms.DialogResult.Cancel; window = new ExpenseWindow(Draft, Settings);
        try
        {
            accepted = await NativeDialogHost.ShowAsync<ExpenseSelection>(window, owner);
            if (accepted != null) DialogResult = Forms.DialogResult.OK;
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() => window?.Close();
}
internal sealed class ExpenseWindow : Window
{
    internal NumericUpDown AmountInput { get; }
    internal NumericUpDown PercentInput { get; } = NativeIntegerInput.Create("expensePercent", 0, 1000);
    internal TextBox ReasonInput { get; }
    internal CheckBox RefundInput { get; }
    internal DatePicker DateInput { get; }
    internal TimePicker TimeInput { get; }
    internal Button AcceptButton { get; }
    internal TextBlock PreviewText { get; } = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly TextBlock validation = new() { TextWrapping = Avalonia.Media.TextWrapping.Wrap };
    private readonly ExpenseSelection original;
    private readonly ExpenseEditSettings settings;
    internal ExpenseWindow(ExpenseSelection original, ExpenseEditSettings settings)
    {
        this.original = original; this.settings = settings;
        bool nuyen = original.Mode == ExpenseType.Nuyen;
        Title = LanguageManager.Instance.GetString(nuyen ? "Title_Expense_Nuyen" : "Title_Expense_Karma");
        Width = 560; MinWidth = 400; SizeToContent = SizeToContent.Height; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        AmountInput = NativeIntegerInput.Create("expenseAmount", settings.Minimum, 9999999);
        AmountInput.Value = original.RawAmount; AmountInput.IsEnabled = settings.AmountEditable;
        PercentInput.Value = original.Percent; PercentInput.IsEnabled = settings.PercentEditable;
        ReasonInput = new TextBox { Text = original.Reason, HorizontalAlignment = HorizontalAlignment.Stretch };
        RefundInput = new CheckBox { IsChecked = original.Refund, IsEnabled = settings.RefundEditable,
            Content = new TextBlock { Text = LanguageManager.Instance.GetString(nuyen ? "Checkbox_Expense_RefundNuyen" : "Checkbox_Expense_Refund"), TextWrapping = Avalonia.Media.TextWrapping.Wrap } };
        DateInput = new DatePicker { Name = "expenseDate", MinYear = new DateTimeOffset(1753, 1, 1, 0, 0, 0, TimeSpan.Zero), MaxYear = new DateTimeOffset(9998, 12, 31, 0, 0, 0, TimeSpan.Zero),
            SelectedDate = new DateTimeOffset(original.Date.Year, original.Date.Month, original.Date.Day, 0, 0, 0, TimeSpan.Zero), Width = 300, Margin = new Thickness(0, 0, 12, 8) };
        TimeInput = new TimePicker { Name = "expenseTime", SelectedTime = original.Date.TimeOfDay, UseSeconds = false, IsVisible = settings.ShowTime,
            ClockIdentifier = CultureInfo.CurrentCulture.DateTimeFormat.ShortTimePattern.Contains('H') ? "24HourClock" : "12HourClock", Width = 200 };
        var layout = new StackPanel { Margin = new Thickness(20), Spacing = 12 };
        layout.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString(nuyen ? "Label_Expense_NuyenAmount" : "Label_Expense_KarmaAmount") });
        var numbers = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 }; numbers.Children.Add(AmountInput);
        var percentage = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 6, IsVisible = nuyen }; percentage.Children.Add(PercentInput); percentage.Children.Add(new TextBlock { Text = "%", VerticalAlignment = VerticalAlignment.Center }); numbers.Children.Add(percentage);
        layout.Children.Add(numbers); layout.Children.Add(PreviewText);
        layout.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString("Label_Expense_Description") }); layout.Children.Add(ReasonInput); layout.Children.Add(RefundInput);
        layout.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString("Label_Expense_Date") });
        var dateRow = new WrapPanel(); dateRow.Children.Add(DateInput); dateRow.Children.Add(TimeInput); layout.Children.Add(dateRow); layout.Children.Add(validation);
        var cancel = new Button { Content = LanguageManager.Instance.GetString("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        AcceptButton = new Button { Content = LanguageManager.Instance.GetString("String_OK"), IsDefault = true }; AcceptButton.Click += (_, _) => Accept();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 }; buttons.Children.Add(cancel); buttons.Children.Add(AcceptButton); layout.Children.Add(buttons); Content = layout;
        AmountInput.ValueChanged += (_, _) => Refresh(); PercentInput.ValueChanged += (_, _) => Refresh(); DateInput.SelectedDateChanged += (_, _) => Refresh(); TimeInput.SelectedTimeChanged += (_, _) => Refresh();
        Opened += (_, _) => { if (AmountInput.IsEnabled) AmountInput.Focus(); else ReasonInput.Focus(); }; Refresh();
    }
    private ExpenseSelection Selection(out bool dateValid)
    {
        dateValid = ExpenseDateTime.TryCompose(original.Date, DateInput.SelectedDate?.DateTime, TimeInput.SelectedTime, settings.ShowTime, out var date);
        return new ExpenseSelection(settings.AmountEditable ? (int)(AmountInput.Value ?? AmountInput.Minimum) : original.RawAmount,
            settings.PercentEditable ? (int)(PercentInput.Value ?? 100) : original.Percent, original.Mode, ReasonInput.Text ?? "", settings.RefundEditable ? RefundInput.IsChecked == true : original.Refund, date);
    }
    private void Refresh()
    {
        var selection = Selection(out bool valid); string error = "";
        if (!valid) error = string.Format(GlobalOptions.Instance.CultureInfo, LanguageManager.Instance.GetString("Message_Expense_InvalidDate"), ExpenseDateTime.Minimum, ExpenseDateTime.Maximum);
        // Nullable standard numeric editors can be cleared. Never turn an
        // incomplete financial draft into an accepted minimum/default charge.
        bool incomplete = (settings.AmountEditable && AmountInput.Value == null) ||
            (original.Mode == ExpenseType.Nuyen && settings.PercentEditable && PercentInput.Value == null);
        if (incomplete)
        {
            valid = false; error = LanguageManager.Instance.GetString("Message_Expense_IncompleteAmount"); PreviewText.Text = "";
        }
        else
        {
            try { PreviewText.Text = selection.Mode == ExpenseType.Nuyen ? string.Format(GlobalOptions.Instance.CultureInfo, "{0:N0}¥", selection.Amount) : selection.Amount.ToString(GlobalOptions.Instance.CultureInfo) + " " + LanguageManager.Instance.GetString("String_Karma"); }
            catch (OverflowException) { valid = false; error = LanguageManager.Instance.GetString("Message_Expense_InvalidAmount"); }
        }
        validation.Text = error; validation.IsVisible = !valid; AcceptButton.IsEnabled = valid;
    }
    internal void Accept()
    {
        Refresh(); if (!AcceptButton.IsEnabled) return;
        var selection = Selection(out bool valid); if (valid) Close(selection);
    }
}
