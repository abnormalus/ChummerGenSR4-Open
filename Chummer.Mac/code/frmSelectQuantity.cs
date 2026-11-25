#nullable enable
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
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
        Title = T("Title_AddRemoveQuantity"); Width = 410; MinWidth = 260; MinHeight = 185; MaxHeight = 500;
        SizeToContent = SizeToContent.Height; CanResize = true;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);
        QuantityInput = NativeIntegerInput.Create("gearQuantity", 0, maximum); QuantityInput.Value = 0;
        QuantityInput.ClearValue(NumericUpDown.WidthProperty);
        MarkupInput = NativeIntegerInput.Create("gearQuantityMarkup", -99, 1000); MarkupInput.Value = 0; MarkupInput.IsEnabled = adding;
        MarkupInput.ClearValue(NumericUpDown.WidthProperty);
        FreeInput = new CheckBox { Content = T("Checkbox_Free"), IsEnabled = adding };
        FreeInput.IsCheckedChanged += (_, _) => MarkupInput.IsEnabled = adding && FreeInput.IsChecked != true;
        AcceptButton = new Button { Content = T("String_OK"), IsDefault = true };
        AcceptButton.Click += (_, _) => Accept();
        var cancel = new Button { Content = T("String_Cancel"), IsCancel = true };
        cancel.Click += (_, _) => Close();
        var quantityCaption = new TextBlock { Text = T("Label_AddRemoveQuanity_Quantity") + (adding ? " +" : " −"),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        var markupCaption = new TextBlock { Text = T("Label_SelectGear_Markup") + " (%)", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        Avalonia.Automation.AutomationProperties.SetLabeledBy(QuantityInput, quantityCaption);
        Avalonia.Automation.AutomationProperties.SetLabeledBy(MarkupInput, markupCaption);
        var fields = new StackPanel { Spacing = 8, Children = { quantityCaption, QuantityInput, FreeInput, markupCaption, MarkupInput } };
        Content = NativeControlPresentation.CreatePromptLayout(fields, cancel, AcceptButton);
    }
    internal void Accept() => Close(new GearQuantitySelection((int)(QuantityInput.Value ?? 0), adding ? (int)(MarkupInput.Value ?? 0) : 0, adding && FreeInput.IsChecked == true));
}
