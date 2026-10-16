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
        Title = T("Details_Editor"); Width = 960; Height = 740; MinWidth = 560; MinHeight = 420;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "branding", "chummer.ico"));
        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,*,Auto"), Margin = new Thickness(16), RowSpacing = 12 };
        var toolbar = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        Button Tool(string key, Action action)
        {
            var button = new Button { Content = T(key) }; button.Click += (_, _) => action(); toolbar.Children.Add(button); return button;
        }
        UndoButton = Tool("Details_Undo", () => ActiveEditor.Undo()); RedoButton = Tool("Details_Redo", () => ActiveEditor.Redo());
        Tool("Details_Smaller", () => ActiveEditor.FontSize = Math.Max(12, ActiveEditor.FontSize - 2));
        Tool("Details_Larger", () => ActiveEditor.FontSize = Math.Min(28, ActiveEditor.FontSize + 2));
        layout.Children.Add(toolbar);
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
        Sections = new TabControl { ItemsSource = labels.Select((key, index) => new TabItem { Header = T(key), Content = Editors[index] }).ToArray(), SelectedIndex = Math.Clamp(section, 0, 3) };
        Grid.SetRow(Sections, 1); layout.Children.Add(Sections);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 8 };
        var cancel = new Button { Content = T("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        AcceptButton = new Button { Content = T("String_OK") };
        AcceptButton.Click += (_, _) => Close(new CharacterDetailsText(Editors[0].Text ?? "", Editors[1].Text ?? "", Editors[2].Text ?? "", Editors[3].Text ?? ""));
        buttons.Children.Add(cancel); buttons.Children.Add(AcceptButton); Grid.SetRow(buttons, 2); layout.Children.Add(buttons);
        Content = layout;
        Opened += (_, _) => ActiveEditor.Focus();
    }
}

internal static class CharacterDetailsEditor
{
    internal static void Attach(Forms.Form owner, Forms.TextBox[] fields, Forms.Label[] labels)
    {
        for (int index = 0; index < fields.Length; index++)
        {
            int section = index;
            var button = new Forms.Button { Name = "expandCharacterDetail" + index, Text = LanguageManager.Instance.GetString("Details_Expand"), Width = 96, Height = 24 };
            var parent = labels[index].Parent ?? throw new InvalidOperationException("Character detail labels must be parented before attaching editors.");
            parent.Controls.Add(button);
            void Position() { button.Left = labels[section].Right + 12; button.Top = labels[section].Top - 2; }
            labels[index].LocationChanged += (_, _) => Position(); labels[index].SizeChanged += (_, _) => Position(); Position();
            button.Click += async (_, _) => {
                var original = new CharacterDetailsText(fields[0].Text, fields[1].Text, fields[2].Text, fields[3].Text);
                var window = new CharacterDetailsWindow(original, section);
                try
                {
                    var result = await NativeDialogHost.ShowAsync<CharacterDetailsText>(window, owner);
                    if (result == null || owner.IsDisposed) return;
                    // Existing TextChanged handlers own model synchronization and dirty state.
                    string[] changed = { result.Description, result.Background, result.Concept, result.Notes };
                    for (int field = 0; field < fields.Length; field++) if (fields[field].Text != changed[field]) fields[field].Text = changed[field];
                }
                catch (Exception error) { AppDiagnostics.Record("character-details.failed", error); if (!owner.IsDisposed) AppErrors.Inform(owner, "Details_Error"); }
            };
        }
    }
}
