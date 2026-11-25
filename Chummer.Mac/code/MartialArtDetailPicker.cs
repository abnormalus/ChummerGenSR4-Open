#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed class MartialArtDetailPicker(Character character, MartialArtDetailPicker.Kind kind, string? martialArt = null) : IDisposable
{
    internal enum Kind { Art, Maneuver, Advantage }

    private MartialArtDetailWindow? window;
    internal string ForcedArt { private get; set; } = "";
    internal string SelectedItem { get; private set; } = "";
    internal bool AddAgain { get; private set; }

    internal Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner);
        if (!pending.IsCompleted)
            Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }

    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null)
            throw new InvalidOperationException("The martial art detail picker is already open.");

        SelectedItem = "";
        AddAgain = false;
        var document = XmlManager.Instance.Load("martialarts.xml");
        var books = character.Options.BookXPath();
        const string artPath = "/chummer/martialarts/martialart";
        var nodes = kind switch
        {
            Kind.Art when ForcedArt.Length > 0 => document.SelectNodes(artPath)!.Cast<XmlNode>()
                .Where(node => node["name"]?.InnerText == ForcedArt),
            Kind.Art => document.SelectNodes(artPath + "[" + books + "]")!.Cast<XmlNode>(),
            Kind.Maneuver => document.SelectNodes("/chummer/maneuvers/maneuver[" + books + "]")!.Cast<XmlNode>(),
            Kind.Advantage => document.SelectNodes(artPath + "[" + books + "]")!.Cast<XmlNode>()
                .Where(node => node["name"]?.InnerText == martialArt)
                .SelectMany(node => node.SelectNodes("advantages/advantage")!.Cast<XmlNode>()),
            _ => throw new ArgumentOutOfRangeException(nameof(kind))
        };
        var choices = nodes.Select(node => new MartialArtDetailChoice(
                node["name"]!.InnerText, node.Attributes?["translate"]?.InnerText ?? node["translate"]?.InnerText ?? node["name"]!.InnerText,
                node["source"]?.InnerText, (node["altpage"] ?? node["page"])?.InnerText))
            .OrderBy(choice => choice.Label, StringComparer.CurrentCulture).ToArray();
        if (kind == Kind.Art && choices.Length == 1)
        {
            SelectedItem = choices[0].Name;
            return Forms.DialogResult.OK;
        }
        if (kind == Kind.Art && ForcedArt.Length > 0 && choices.Length == 0)
            return Forms.DialogResult.Cancel;

        window = new MartialArtDetailWindow(character, choices, kind);
        try
        {
            var result = await NativeDialogHost.ShowAsync<MartialArtDetailSelection>(window, owner);
            if (result == null)
                return Forms.DialogResult.Cancel;
            SelectedItem = result.Name;
            AddAgain = result.AddAgain;
            return Forms.DialogResult.OK;
        }
        finally { window = null; }
    }

    public void Dispose() => window?.Close();
}

internal sealed record MartialArtDetailChoice(string Name, string Label, string? Book, string? Page)
{
    public override string ToString() => Label;
}
internal sealed record MartialArtDetailSelection(string Name, bool AddAgain);

internal sealed class MartialArtDetailWindow : Window
{
    internal ListBox Choices { get; }
    internal Button SourceButton { get; }
    internal Button AcceptButton { get; }
    internal Button AddButton { get; }
    internal Button CancelButton { get; }

    internal MartialArtDetailWindow(Character character, MartialArtDetailChoice[] choices, MartialArtDetailPicker.Kind kind)
    {
        var language = LanguageManager.Instance;
        Title = language.GetString(kind switch
        {
            MartialArtDetailPicker.Kind.Art => "Title_SelectMartialArt",
            MartialArtDetailPicker.Kind.Maneuver => "Title_SelectMartialArtManeuver",
            _ => "Title_SelectMartialArtAdvantage"
        });
        Width = 410;
        Height = 430;
        MinWidth = 260;
        MinHeight = 220;
        ShowInTaskbar = false;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);

        Choices = new ListBox
        {
            ItemsSource = choices,
            IsTextSearchEnabled = true,
            ItemTemplate = new FuncDataTemplate<MartialArtDetailChoice>((choice, _) =>
                new TextBlock { Text = choice?.Label ?? "", TextWrapping = TextWrapping.Wrap })
        };
        Avalonia.Automation.AutomationProperties.SetName(Choices, Title);
        ScrollViewer.SetHorizontalScrollBarVisibility(Choices, Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
        Choices.DoubleTapped += (_, args) =>
        {
            if (args.Source is Visual visual && (visual is ListBoxItem || visual.FindAncestorOfType<ListBoxItem>() != null))
                Accept(false);
        };

        SourceButton = new Button { IsVisible = false };
        SourceButton.Click += (_, _) =>
        {
            if (SourceButton.Content is string source)
                new CommonFunctions(character).OpenPDF(source);
        };
        AcceptButton = new Button { Content = language.GetString("String_OK"), IsDefault = true, IsEnabled = false };
        AcceptButton.Click += (_, _) => Accept(false);
        AddButton = new Button { Content = language.GetString("String_AddMore"), IsEnabled = false };
        AddButton.Click += (_, _) => Accept(true);
        CancelButton = new Button { Content = language.GetString("String_Cancel"), IsCancel = true };
        CancelButton.Click += (_, _) => Close();
        Choices.SelectionChanged += (_, _) =>
        {
            var choice = Choices.SelectedItem as MartialArtDetailChoice;
            AcceptButton.IsEnabled = AddButton.IsEnabled = choice != null;
            SourceButton.IsVisible = kind != MartialArtDetailPicker.Kind.Advantage && choice?.Book != null && choice.Page != null;
            if (choice?.Book is { } book && choice.Page is { } page)
            {
                SourceButton.Content = character.Options.LanguageBookShort(book) + " " + page;
                ToolTip.SetTip(SourceButton, character.Options.LanguageBookLong(book) + " " + language.GetString("String_Page") + " " + page);
            }
        };
        Choices.SelectedIndex = choices.Length == 0 ? -1 : 0;

        var actions = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        actions.Children.Add(AddButton);
        actions.Children.Add(AcceptButton);
        actions.Children.Add(CancelButton);
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("*,Auto,Auto"), RowSpacing = 8 };
        layout.Children.Add(Choices);
        Grid.SetRow(SourceButton, 1);
        layout.Children.Add(SourceButton);
        Grid.SetRow(actions, 2);
        layout.Children.Add(actions);
        var wrappedCaption = new FuncDataTemplate<string>((text, _) =>
            new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
        var buttons = new[] { SourceButton, AddButton, AcceptButton, CancelButton };
        foreach (var button in buttons)
            button.ContentTemplate = wrappedCaption;
        layout.SizeChanged += (_, args) =>
        {
            if (args.NewSize.Width <= 0) return;
            foreach (var button in buttons)
                button.MaxWidth = args.NewSize.Width;
        };
        Content = layout;
    }

    private void Accept(bool addAgain)
    {
        if (Choices.SelectedItem is MartialArtDetailChoice selected)
            Close(new MartialArtDetailSelection(selected.Name, addAgain));
    }
}
