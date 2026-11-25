#nullable enable
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;

namespace Chummer;

internal sealed class NativeChoiceWindow : Window
{
    internal ComboBox ChoiceInput { get; }
    internal Button AcceptButton { get; }
    internal Button CancelButton { get; }

    internal NativeChoiceWindow(IReadOnlyList<ListItem> choices, string title, string description, string label, int defaultIndex = 0)
    {
        var language = LanguageManager.Instance;
        Title = title;
        Width = 420;
        MinWidth = 260;
        MinHeight = 160;
        MaxHeight = 500;
        SizeToContent = SizeToContent.Height;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);

        ChoiceInput = new ComboBox
        {
            ItemsSource = choices,
            ItemTemplate = new FuncDataTemplate<ListItem>((choice, _) => new TextBlock
            {
                Text = choice?.Name ?? "",
                TextTrimming = TextTrimming.CharacterEllipsis
            }),
            SelectedIndex = choices.Count == 0 ? -1 : Math.Max(0, defaultIndex),
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(ChoiceInput, label);

        AcceptButton = new Button { Content = Caption(language.GetString("String_OK")), IsDefault = true, IsEnabled = choices.Count > 0 };
        AcceptButton.Click += (_, _) =>
        {
            if (ChoiceInput.SelectedItem is ListItem selected)
                Close(selected);
        };
        CancelButton = new Button { Content = Caption(language.GetString("String_Cancel")), IsCancel = true };
        CancelButton.Click += (_, _) => Close();

        var heading = Caption(label);
        AutomationProperties.SetLabeledBy(ChoiceInput, heading);
        var fields = new StackPanel { Spacing = 10, Children = { Caption(description), heading, ChoiceInput } };
        Content = NativeControlPresentation.CreatePromptLayout(fields, AcceptButton, CancelButton);

        static TextBlock Caption(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
