#nullable enable
using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed record CharacterDetailsText(string Description, string Background, string Concept, string Notes);

// A bounded standard-control editor. Plain strings remain the persistence contract;
// the dialog edits a private draft and only returns a value on explicit acceptance.
internal sealed class CharacterDetailsWindow : Window
{
    internal TextBox[] Editors { get; }
    internal TabControl Sections { get; }
    internal Button AcceptButton { get; }
    internal Button UndoButton { get; }
    internal Button RedoButton { get; }
    private TextBox ActiveEditor => Editors[Math.Clamp(Sections.SelectedIndex, 0, Editors.Length - 1)];
    private static string T(string key) => LanguageManager.Instance.GetString(key).Replace("&", "").TrimEnd(':');
    internal CharacterDetailsWindow(CharacterDetailsText original, int section)
    {
        Title = T("Details_Editor"); Width = 960; Height = 740; MinWidth = 320; MinHeight = 340;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);
        Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "branding", "chummer.ico"));
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(16), RowSpacing = 12 };
        var toolbar = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        Button Tool(string key, Action action)
        {
            var button = new Button { Content = Caption(key) }; button.Click += (_, _) => action(); toolbar.Children.Add(button); return button;
        }
        UndoButton = Tool("Details_Undo", () => ActiveEditor.Undo()); RedoButton = Tool("Details_Redo", () => ActiveEditor.Redo());
        double initialFont = Avalonia.Controls.Documents.TextElement.GetFontSize(layout);
        var editorSizes = new[] { 16d, 16d, 16d, 16d };
        Tool("Details_Smaller", () => ChangeTextSize(-2));
        Tool("Details_Larger", () => ChangeTextSize(2));
        var toolbarViewport = new ScrollViewer
        {
            Content = toolbar, MaxHeight = 110,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        layout.Children.Add(toolbarViewport);
        string[] text = { original.Description, original.Background, original.Concept, original.Notes };
        string[] labels = { "Label_Description", "Label_Background", "Label_Concept", "Label_Notes" };
        Editors = text.Select((value, index) => {
            var editor = new TextBox { Name = "characterDetail" + index, Text = value, AcceptsReturn = true,
                TextWrapping = TextWrapping.Wrap, FontSize = 16, LineHeight = 26, Padding = new Thickness(18),
                HorizontalAlignment = HorizontalAlignment.Stretch, VerticalAlignment = VerticalAlignment.Stretch,
                MaxWidth = 920, UndoLimit = 100 };
            ScrollViewer.SetVerticalScrollBarVisibility(editor, Avalonia.Controls.Primitives.ScrollBarVisibility.Auto);
            ScrollViewer.SetHorizontalScrollBarVisibility(editor, Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled);
            editor.PropertyChanged += (_, change) => { if (change.Property == TextBox.FontSizeProperty) editor.LineHeight = editor.FontSize * 1.625; };
            return editor;
        }).ToArray();
        Sections = new TabControl
        {
            HorizontalContentAlignment = HorizontalAlignment.Stretch,
            VerticalContentAlignment = VerticalAlignment.Stretch,
            ItemsPanel = new Avalonia.Controls.Templates.FuncTemplate<Panel?>(() => new WrapPanel()),
            ItemsSource = labels.Select((key, index) => new TabItem { Header = Caption(key), Content = Editors[index] }).ToArray(),
            SelectedIndex = Math.Clamp(section, 0, 3)
        };
        foreach (var tab in Sections.Items.OfType<TabItem>())
            NativeEditorTabStrip.BindHeaderTypography(Sections, tab);
        layout.PropertyChanged += (_, args) =>
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty)
                ApplyTextSizes();
        };
        ApplyTextSizes();
        Grid.SetRow(Sections, 1); layout.Children.Add(Sections);
        var buttons = new WrapPanel { HorizontalAlignment = HorizontalAlignment.Right, ItemSpacing = 8, LineSpacing = 4 };
        var cancel = new Button { Content = Caption("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        AcceptButton = new Button { Content = Caption("String_OK") };
        AcceptButton.Click += (_, _) => Close(new CharacterDetailsText(Editors[0].Text ?? "", Editors[1].Text ?? "", Editors[2].Text ?? "", Editors[3].Text ?? ""));
        buttons.Children.Add(cancel); buttons.Children.Add(AcceptButton); Grid.SetRow(buttons, 2); layout.Children.Add(buttons);
        Content = layout;
        Opened += (_, _) => ActiveEditor.Focus();

        void ChangeTextSize(int delta)
        {
            int index = Math.Clamp(Sections!.SelectedIndex, 0, Editors!.Length - 1);
            editorSizes[index] = Math.Clamp(editorSizes[index] + delta, 12, 28);
            ApplyTextSizes();
        }

        void ApplyTextSizes()
        {
            double scale = Avalonia.Controls.Documents.TextElement.GetFontSize(layout) / initialFont;
            for (int index = 0; index < Editors.Length; index++)
                Editors[index].FontSize = editorSizes[index] * scale;
        }

        static TextBlock Caption(string key) => new() { Text = T(key), TextWrapping = TextWrapping.Wrap };
    }
}

internal static class CharacterDetailsEditor
{
    internal static Button CreateExpandButton(Forms.Form owner, Forms.TextBox[] fields, int section)
    {
        var button = new Button
        {
            Name = "expandCharacterDetail" + section,
            HorizontalAlignment = HorizontalAlignment.Right,
            Content = new TextBlock
            {
                Text = LanguageManager.Instance.GetString("Details_Expand"),
                TextWrapping = TextWrapping.Wrap
            }
        };
        button.Click += async (_, _) =>
        {
            var original = new CharacterDetailsText(fields[0].Text, fields[1].Text, fields[2].Text, fields[3].Text);
            var window = new CharacterDetailsWindow(original, section);
            try
            {
                var result = await NativeDialogHost.ShowAsync<CharacterDetailsText>(window, owner);
                if (result == null || owner.IsDisposed) return;

                string[] changed = { result.Description, result.Background, result.Concept, result.Notes };
                for (int field = 0; field < fields.Length; field++)
                    if (fields[field].Text != changed[field]) fields[field].Text = changed[field];
            }
            catch (Exception error)
            {
                AppDiagnostics.Record("character-details.failed", error);
                if (!owner.IsDisposed) AppErrors.Inform(owner, "Details_Error");
            }
        };
        return button;
    }
}
