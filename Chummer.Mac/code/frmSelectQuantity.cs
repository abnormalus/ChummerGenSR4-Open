#nullable enable
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Keep purchase accounting in the caller; the native dialog returns an accepted draft only.
public sealed class frmSelectQuantity : IDisposable
{
    public enum Operation { Add, Remove }
    private readonly int maximum;
    private GearQuantityWindow? window;
    private GearQuantitySelection? selection;
    public Operation Op { get; }
    public int Quantity => selection?.Quantity ?? 0;
    public int Markup => selection?.Markup ?? 0;
    public bool FreeCost => selection?.FreeCost ?? false;
    public frmSelectQuantity(Operation operation, int maximum)
    { Op = operation; this.maximum = Math.Max(0, maximum); }
    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner);
        Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Quantity dialog is already open.");
        selection = null; window = new GearQuantityWindow(Op, maximum);
        try
        {
            selection = await NativeDialogHost.ShowAsync<GearQuantitySelection>(window, owner);
            return selection == null ? Forms.DialogResult.Cancel : Forms.DialogResult.OK;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.Close(); }
}
internal sealed record GearQuantitySelection(int Quantity, int Markup, bool FreeCost);
internal sealed class GearQuantityWindow : Window
{
    internal NumericUpDown QuantityInput { get; }
    internal NumericUpDown MarkupInput { get; }
    internal CheckBox FreeInput { get; }
    internal Button AcceptButton { get; }
    private readonly bool adding;
    private static string T(string key) => LanguageManager.Instance.GetString(key).Replace("&", "");
    internal GearQuantityWindow(frmSelectQuantity.Operation operation, int maximum)
    {
        adding = operation == frmSelectQuantity.Operation.Add;
        Title = T("Title_AddRemoveQuantity"); Width = 410; MinWidth = 360;
        SizeToContent = SizeToContent.Height; CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        QuantityInput = NativeIntegerInput.Create("gearQuantity", 0, maximum); QuantityInput.Value = 0;
        MarkupInput = NativeIntegerInput.Create("gearQuantityMarkup", -99, 1000); MarkupInput.Value = 0; MarkupInput.IsEnabled = adding;
        FreeInput = new CheckBox { Content = T("Checkbox_Free"), IsEnabled = adding };
        FreeInput.IsCheckedChanged += (_, _) => MarkupInput.IsEnabled = adding && FreeInput.IsChecked != true;
        AcceptButton = new Button { Content = T("String_OK"), IsDefault = true };
        AcceptButton.Click += (_, _) => Accept();
        var cancel = new Button { Content = T("String_Cancel"), IsCancel = true };
        cancel.Click += (_, _) => Close();
        var layout = new StackPanel { Margin = new Thickness(20), Spacing = 14 };
        layout.Children.Add(new TextBlock { Text = T("Label_AddRemoveQuanity_Quantity") + (adding ? " +" : " −") });
        layout.Children.Add(QuantityInput); layout.Children.Add(FreeInput);
        layout.Children.Add(new TextBlock { Text = T("Label_SelectGear_Markup") + " (%)" }); layout.Children.Add(MarkupInput);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
        buttons.Children.Add(cancel); buttons.Children.Add(AcceptButton); layout.Children.Add(buttons); Content = layout;
    }
    internal void Accept() => Close(new GearQuantitySelection((int)(QuantityInput.Value ?? 0), adding ? (int)(MarkupInput.Value ?? 0) : 0, adding && FreeInput.IsChecked == true));
}
