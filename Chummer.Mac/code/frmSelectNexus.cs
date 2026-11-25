#nullable enable
using System;
using System.Collections.Generic;
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
        Width = 600; MinWidth = 280; MinHeight = 200; MaxHeight = 540; SizeToContent = SizeToContent.Height; WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);
        var root = new StackPanel { Spacing = 10 };
        var fields = new Grid { ColumnSpacing = 8, RowSpacing = 6 };
        var rows = new List<(TextBlock Caption, NumericUpDown Input, TextBlock? Availability)>();
        var availabilityHeading = new TextBlock { Text = LanguageManager.Instance.GetString("Label_Avail"),
            TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        Grid.SetColumn(availabilityHeading, 2);
        fields.Children.Add(availabilityHeading);
        NumericUpDown Add(string key, string property, int minimum, int maximum, string? availability = null)
        {
            var label = new TextBlock { Text = LanguageManager.Instance.GetString(key), VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = Avalonia.Media.TextWrapping.Wrap };
            var input = NativeIntegerInput.Create("nexus" + property, minimum, maximum);
            input.Bind(NumericUpDown.ValueProperty, new Binding(property) { Mode = BindingMode.TwoWay });
            Avalonia.Automation.AutomationProperties.SetLabeledBy(input, label);
            fields.Children.Add(label);
            fields.Children.Add(input);
            TextBlock? value = null;
            if (availability != null)
            {
                value = new TextBlock { VerticalAlignment = VerticalAlignment.Center, TextWrapping = Avalonia.Media.TextWrapping.Wrap };
                value.Bind(TextBlock.TextProperty, new Binding(availability));
                fields.Children.Add(value);
            }
            rows.Add((label, input, value));
            return input;
        }
        ProcessorInput = Add("Label_SelectNexus_Processor", nameof(model.Processor), 10, 50);
        SystemInput = Add("Label_System", nameof(model.System), 1, 10, nameof(model.SystemAvailability));
        ResponseInput = Add("Label_Response", nameof(model.Response), 1, 10, nameof(model.ResponseAvailability));
        FirewallInput = Add("Label_Firewall", nameof(model.Firewall), 1, 10, nameof(model.FirewallAvailability));
        SignalInput = Add("Label_Signal", nameof(model.Signal), 1, 10);
        PersonaInput = Add("Label_SelectNexus_PersonaLimit", nameof(model.Persona), 3, 50);
        PersonaInput.Bind(NumericUpDown.MinimumProperty, new Binding(nameof(model.PersonaMinimum)));
        bool? stacked = null;
        void Reflow()
        {
            double font = Avalonia.Controls.Documents.TextElement.GetFontSize(fields);
            bool narrow = fields.Bounds.Width < Math.Max(380, font * 26);
            if (stacked == narrow)
                return;
            stacked = narrow;
            fields.ColumnDefinitions = new ColumnDefinitions(narrow ? "Auto,*" : "*,Auto,*");
            Grid.SetColumn(availabilityHeading, narrow ? 1 : 2);
            fields.RowDefinitions.Clear();
            fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            foreach (var (caption, input, availability) in rows)
            {
                int row = fields.RowDefinitions.Count;
                fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                if (narrow)
                    fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                Grid.SetRow(caption, row);
                Grid.SetColumnSpan(caption, narrow ? 2 : 1);
                Grid.SetRow(input, narrow ? row + 1 : row);
                Grid.SetColumn(input, narrow ? 0 : 1);
                if (availability != null)
                {
                    Grid.SetRow(availability, narrow ? row + 1 : row);
                    Grid.SetColumn(availability, narrow ? 1 : 2);
                }
            }
        }
        fields.SizeChanged += (_, _) => Reflow();
        fields.PropertyChanged += (_, change) =>
        {
            if (change.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty) Reflow();
        };
        Reflow();
        root.Children.Add(fields);
        var price = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 14 };
        price.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString("Label_Cost") });
        var cost = new TextBlock(); cost.Bind(TextBlock.TextProperty, new Binding(nameof(model.Cost))); price.Children.Add(cost); root.Children.Add(price);
        var free = new CheckBox { Content = LanguageManager.Instance.GetString("Checkbox_Free"), IsVisible = model.IsCareer };
        free.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(model.Free)) { Mode = BindingMode.TwoWay }); root.Children.Add(free);
        var cancel = new Button { Content = LanguageManager.Instance.GetString("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        AcceptButton = new Button { Content = LanguageManager.Instance.GetString("String_OK"), IsDefault = true };
        AcceptButton.Bind(IsEnabledProperty, new Binding(nameof(model.CanAccept))); AcceptButton.Click += (_, _) => Accept();
        Content = NativeControlPresentation.CreatePromptLayout(root, cancel, AcceptButton);
    }
    internal void Accept() { if (Model.Selection is { } accepted) Close(accepted); }
}
