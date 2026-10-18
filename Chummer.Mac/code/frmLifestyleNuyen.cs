#nullable enable
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;

namespace Chummer;

// The roll is a draft. The creation caller owns the final character transaction;
// this dialog only returns an explicitly accepted starting-money calculation.
public sealed class frmLifestyleNuyen : IDisposable
{
    public int Dice { get; set; }
    public int Extra { get; set; }
    public int Multiplier { get; set; }
    public int StartingNuyen { get; private set; }
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    private StartingMoneyWindow? window;
    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Starting-money prompt is already open.");
        StartingNuyen = 0; DialogResult = Forms.DialogResult.Cancel;
        window = new StartingMoneyWindow(new StartingMoneyRoll(Dice, Extra, Multiplier));
        try
        {
            var accepted = await NativeDialogHost.ShowAsync<StartingMoneySelection>(window, owner);
            if (accepted != null) { StartingNuyen = accepted.Amount; DialogResult = Forms.DialogResult.OK; }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() => window?.Close();
}
// The existing monetary formula is independent of controls and formatting.
internal sealed record StartingMoneyRoll(int Dice, int Extra, int Multiplier)
{
    internal int Minimum => Dice;
    internal int Maximum => checked(Dice * 6);
    internal int Total(int roll) => Convert.ToInt32(((decimal)roll + Extra) * Multiplier);
}
internal sealed record StartingMoneySelection(int Amount);
internal sealed class StartingMoneyWindow : Window
{
    internal NumericUpDown RollInput { get; }
    internal TextBlock ResultText { get; } = new();
    private readonly StartingMoneyRoll roll;
    internal int Amount => roll.Total((int)(RollInput.Value ?? RollInput.Minimum));
    internal StartingMoneyWindow(StartingMoneyRoll roll)
    {
        this.roll = roll;
        Title = LanguageManager.Instance.GetString("Title_LifestyleNuyen");
        Width = 500; MinWidth = 360; SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        RollInput = NativeIntegerInput.Create("startingMoneyRoll", roll.Minimum, roll.Maximum);
        RollInput.Value = roll.Minimum; RollInput.HorizontalAlignment = HorizontalAlignment.Left;
        RollInput.ValueChanged += (_, _) => RefreshResult();
        var layout = new StackPanel { Margin = new Thickness(20), Spacing = 14 };
        layout.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString("Label_LifestyleNuyen_Description"), TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        layout.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString("Label_LifestyleNuyen_ResultOf") + " " + roll.Dice + "D6:" });
        layout.Children.Add(RollInput); layout.Children.Add(ResultText);
        ResultText.TextWrapping = Avalonia.Media.TextWrapping.Wrap;
        var cancel = new Button { Content = LanguageManager.Instance.GetString("String_Cancel"), IsCancel = true };
        cancel.Click += (_, _) => Close();
        var accept = new Button { Content = LanguageManager.Instance.GetString("String_OK"), IsDefault = true };
        accept.Click += (_, _) => Accept();
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        buttons.Children.Add(cancel); buttons.Children.Add(accept); layout.Children.Add(buttons); Content = layout;
        Opened += (_, _) => RollInput.Focus(); RefreshResult();
    }
    private void RefreshResult() => ResultText.Text = "(" + (RollInput.Value ?? RollInput.Minimum) + " + " + roll.Extra + ") × " + roll.Multiplier + " = " + string.Format(GlobalOptions.Instance.CultureInfo, "{0:###,###,##0¥}", Amount);
    internal void Accept() => Close(new StartingMoneySelection(Amount));
}
