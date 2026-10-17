#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;
namespace Chummer;

// Existing rule and purchase callers retain an integer-result boundary. An initial
// value is a draft; only explicit acceptance makes SelectedValue available.
public sealed class frmSelectNumber : IDisposable
{
    private int minimum, maximum = 10000000, initialValue, selectedValue;
    private NumberPromptWindow? window;
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    public int SelectedValue { get => selectedValue; set => initialValue = value; }
    public int Minimum { set { minimum = value; if (maximum < minimum) maximum = minimum; } }
    public int Maximum { set { maximum = value; if (minimum > maximum) minimum = maximum; } }
    public string Description { private get; set; } = "";
    public string Text { private get; set; } = LanguageManager.Instance.GetString("Title_SelectNumber");
    public bool AllowCancel { private get; set; } = true;
    public Forms.DialogResult ShowDialog(Forms.Form? owner = null)
    {
        owner ??= Forms.Form.ActiveForm ?? Forms.Application.OpenForms.Cast<Forms.Form>().LastOrDefault(form => form.Visible);
        if (owner == null) throw new InvalidOperationException("A number prompt requires an open owner window.");
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Number prompt is already open.");
        selectedValue = 0; DialogResult = Forms.DialogResult.Cancel;
        window = new NumberPromptWindow(Text, Description, minimum, maximum, Math.Clamp(initialValue, minimum, maximum), AllowCancel);
        try
        {
            var accepted = await NativeDialogHost.ShowAsync<NumberPromptSelection>(window, owner);
            if (accepted != null) { selectedValue = accepted.Value; DialogResult = Forms.DialogResult.OK; }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.CancelForShutdown(); }
}
internal sealed record NumberPromptSelection(int Value);
internal sealed class NumberPromptWindow : Window, INativeDialogCancellation
{
    internal NumericUpDown Input { get; }
    internal Button CancelButton { get; }
    internal Button AcceptButton { get; }
    private bool accepted, shutdownCancellation;
    internal NumberPromptWindow(string title, string description, int minimum, int maximum, int initialValue, bool allowCancel)
    {
        Title = title; Width = 440; MinWidth = 340; SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        Input = NativeIntegerInput.Create("selectedNumber", minimum, maximum);
        Input.Value = initialValue; Input.HorizontalAlignment = HorizontalAlignment.Left;
        CancelButton = new Button { Content = LanguageManager.Instance.GetString("String_Cancel"), IsEnabled = allowCancel, IsCancel = allowCancel };
        CancelButton.Click += (_, _) => Close();
        AcceptButton = new Button { Content = LanguageManager.Instance.GetString("String_OK"), IsDefault = true };
        AcceptButton.Click += (_, _) => Accept();
        // Mandatory choices block ordinary close/Escape, but explicit application
        // shutdown must unwind the modal without manufacturing an accepted value.
        Closing += (_, args) => { if (!allowCancel && !accepted && !shutdownCancellation) args.Cancel = true; };
        Opened += (_, _) => Input.Focus();
        var layout = new StackPanel { Margin = new Thickness(20), Spacing = 14 };
        layout.Children.Add(new TextBlock { Text = description, TextWrapping = Avalonia.Media.TextWrapping.Wrap });
        layout.Children.Add(Input);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        buttons.Children.Add(CancelButton); buttons.Children.Add(AcceptButton); layout.Children.Add(buttons); Content = layout;
    }
    internal void Accept() { accepted = true; Close(new NumberPromptSelection((int)(Input.Value ?? Input.Minimum))); }
    public void CancelForShutdown() { shutdownCancellation = true; Close(); }
}
