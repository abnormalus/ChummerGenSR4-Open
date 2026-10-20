#nullable enable
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;
namespace Chummer;

public sealed class frmSelectNexus : IDisposable
{
    private readonly Character character;
    private readonly bool career;
    private NexusWindow? window;
    private Gear? selected;
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    public Gear SelectedNexus => selected ?? throw new InvalidOperationException("No nexus was accepted.");
    public bool FreeCost { get; private set; }
    public frmSelectNexus(Character objCharacter, bool blnCareer = false) { character = objCharacter; career = blnCareer; }
    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending); return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Nexus builder is already open.");
        selected = null; FreeCost = false; DialogResult = Forms.DialogResult.Cancel; window = new NexusWindow(new NexusDraft(career));
        try
        {
            var accepted = await NativeDialogHost.ShowAsync<NexusConfiguration>(window, owner);
            if (accepted != null) { selected = accepted.CreateGear(character); FreeCost = accepted.Free; DialogResult = Forms.DialogResult.OK; }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.Close(); window = null; }
}
internal sealed class NexusWindow : Window
{
    internal NexusDraft Model { get; }
    internal NumericUpDown ProcessorInput { get; }
    internal NumericUpDown SystemInput { get; }
    internal NumericUpDown ResponseInput { get; }
    internal NumericUpDown FirewallInput { get; }
    internal NumericUpDown PersonaInput { get; }
    internal NumericUpDown SignalInput { get; }
    internal Button AcceptButton { get; }
    internal NexusWindow(NexusDraft model)
    {
        Model = model; DataContext = model; Title = LanguageManager.Instance.GetString("Title_SelectNexus");
        Width = 600; MinWidth = 480; SizeToContent = SizeToContent.Height; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        var root = new StackPanel { Margin = new Thickness(20), Spacing = 14 };
        var fields = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,Auto,*"), ColumnSpacing = 14, RowSpacing = 10 };
        fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
        var availabilityHeading = new TextBlock { Text = LanguageManager.Instance.GetString("Label_Avail") };
        Grid.SetColumn(availabilityHeading, 2); fields.Children.Add(availabilityHeading);
        NumericUpDown Add(string key, string property, int minimum, int maximum, string? availability = null)
        {
            int row = fields.RowDefinitions.Count; fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = new TextBlock { Text = LanguageManager.Instance.GetString(key), VerticalAlignment = VerticalAlignment.Center };
            var input = NativeIntegerInput.Create("nexus" + property, minimum, maximum);
            input.Bind(NumericUpDown.ValueProperty, new Binding(property) { Mode = BindingMode.TwoWay });
            Grid.SetRow(label, row); Grid.SetRow(input, row); Grid.SetColumn(input, 1); fields.Children.Add(label); fields.Children.Add(input);
            if (availability != null)
            {
                var value = new TextBlock { VerticalAlignment = VerticalAlignment.Center };
                value.Bind(TextBlock.TextProperty, new Binding(availability)); Grid.SetColumn(value, 2); Grid.SetRow(value, row); fields.Children.Add(value);
            }
            return input;
        }
        ProcessorInput = Add("Label_SelectNexus_Processor", nameof(model.Processor), 10, 50);
        SystemInput = Add("Label_System", nameof(model.System), 1, 10, nameof(model.SystemAvailability));
        ResponseInput = Add("Label_Response", nameof(model.Response), 1, 10, nameof(model.ResponseAvailability));
        FirewallInput = Add("Label_Firewall", nameof(model.Firewall), 1, 10, nameof(model.FirewallAvailability));
        SignalInput = Add("Label_Signal", nameof(model.Signal), 1, 10);
        PersonaInput = Add("Label_SelectNexus_PersonaLimit", nameof(model.Persona), 3, 50);
        PersonaInput.Bind(NumericUpDown.MinimumProperty, new Binding(nameof(model.PersonaMinimum)));
        root.Children.Add(fields);
        var price = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        price.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString("Label_Cost") });
        var cost = new TextBlock(); cost.Bind(TextBlock.TextProperty, new Binding(nameof(model.Cost))); price.Children.Add(cost); root.Children.Add(price);
        var free = new CheckBox { Content = LanguageManager.Instance.GetString("Checkbox_Free"), IsVisible = model.IsCareer };
        free.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(model.Free)) { Mode = BindingMode.TwoWay }); root.Children.Add(free);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 10, HorizontalAlignment = HorizontalAlignment.Right };
        var cancel = new Button { Content = LanguageManager.Instance.GetString("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        AcceptButton = new Button { Content = LanguageManager.Instance.GetString("String_OK"), IsDefault = true };
        AcceptButton.Bind(IsEnabledProperty, new Binding(nameof(model.CanAccept))); AcceptButton.Click += (_, _) => Accept();
        buttons.Children.Add(cancel); buttons.Children.Add(AcceptButton); root.Children.Add(buttons); Content = root;
    }
    internal void Accept() { if (Model.Selection is { } accepted) Close(accepted); }
}
