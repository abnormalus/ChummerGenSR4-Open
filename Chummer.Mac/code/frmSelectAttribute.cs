#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public sealed class frmSelectAttribute : IDisposable
{
    private readonly List<string> attributes = ["BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG"];
    private string? singleAttribute;
    private AttributePickerWindow? window;

    public string Description { private get; set; } = "";
    public bool ShowMetatypeMaximum { private get; set; }
    public string SelectedAttribute { get; private set; } = "";
    public bool DoNotAffectMetatypeMaximum { get; private set; }
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;

    public void AddMAG() => AddAttribute("MAG");
    public void AddRES() => AddAttribute("RES");
    public void SingleAttribute(string value) => singleAttribute = value;

    public void LimitToList(List<string> values)
    {
        singleAttribute = null;
        attributes.Clear();
        attributes.AddRange(values);
    }

    public void RemoveFromList(List<string> values)
    {
        singleAttribute = null;
        foreach (var value in values)
            attributes.Remove(value);
    }

    private void AddAttribute(string value)
    {
        singleAttribute = null;
        attributes.Add(value);
    }

    public Forms.DialogResult ShowDialog(Forms.Form? owner = null)
    {
        var pending = ShowDialogAsync(owner);
        if (!pending.IsCompleted)
            Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }

    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form? owner = null)
    {
        if (window != null)
            throw new InvalidOperationException("The attribute picker is already open.");

        SelectedAttribute = "";
        DoNotAffectMetatypeMaximum = false;
        DialogResult = Forms.DialogResult.Cancel;
        var values = singleAttribute is { } value ? new[] { value } : attributes.ToArray();
        if (values.Length == 0)
            return DialogResult;
        if (values.Length == 1)
        {
            SelectedAttribute = values[0];
            return DialogResult = Forms.DialogResult.OK;
        }

        owner ??= Forms.Form.ActiveForm ?? Forms.Application.OpenForms.Cast<Forms.Form>().LastOrDefault(form => form.Visible)
            ?? throw new InvalidOperationException("An attribute picker requires an open owner window.");

        window = new AttributePickerWindow(Description, values, ShowMetatypeMaximum);
        try
        {
            var selection = await NativeDialogHost.ShowAsync<AttributePickerSelection>(window, owner);
            if (selection != null)
            {
                SelectedAttribute = selection.Attribute;
                DoNotAffectMetatypeMaximum = selection.DoNotAffectMaximum;
                DialogResult = Forms.DialogResult.OK;
            }
            return DialogResult;
        }
        finally { window = null; }
    }

    public void Dispose() => window?.Close();
}

internal sealed record AttributePickerSelection(string Attribute, bool DoNotAffectMaximum);

internal sealed class AttributePickerWindow : Window
{
    internal ComboBox AttributeInput { get; }
    internal CheckBox MaximumInput { get; }
    internal Button AcceptButton { get; }
    internal Button CancelButton { get; }

    internal AttributePickerWindow(string description, string[] values, bool showMetatypeMaximum)
    {
        var language = LanguageManager.Instance;
        Title = language.GetString("Title_SelectAttribute");
        Width = 400;
        MinWidth = 260;
        MinHeight = 160;
        MaxHeight = 500;
        SizeToContent = SizeToContent.Height;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        ShowInTaskbar = false;
        NativeControlPresentation.AddTheme(Styles);

        AttributeInput = new ComboBox
        {
            ItemsSource = values.Select(value => language.GetString("String_Attribute" + value + "Short")).ToArray(),
            SelectedIndex = 0,
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        Avalonia.Automation.AutomationProperties.SetName(AttributeInput, Title);
        MaximumInput = new CheckBox
        {
            IsVisible = showMetatypeMaximum,
            Content = new TextBlock { Text = language.GetString("Checkbox_DoNotAffectMaximum"), TextWrapping = TextWrapping.Wrap }
        };
        AcceptButton = new Button { Content = language.GetString("String_OK"), IsDefault = true };
        AcceptButton.Click += (_, _) =>
        {
            if (AttributeInput.SelectedIndex >= 0)
                Close(new AttributePickerSelection(values[AttributeInput.SelectedIndex], MaximumInput.IsChecked == true));
        };
        CancelButton = new Button { Content = language.GetString("String_Cancel"), IsCancel = true };
        CancelButton.Click += (_, _) => Close();

        var prompt = new TextBlock { Text = description, TextWrapping = TextWrapping.Wrap };
        Content = NativeControlPresentation.CreatePromptLayout(
            new StackPanel { Spacing = 10, Children = { prompt, AttributeInput, MaximumInput } }, AcceptButton, CancelButton);
    }
}
