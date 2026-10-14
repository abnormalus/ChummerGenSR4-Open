using System;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer
{
    internal sealed class AliasChoice
    {
        internal string Name;
        internal StreetNamePreferences Preferences;
    }
    internal static class AliasGeneratorDialog
    {
        private static string T(string key) => LanguageManager.Instance.GetString(key);
        internal static async Task<AliasChoice> ShowAsync(Forms.Form owner, Character character, StreetNameEngine engine, StreetNamePreferences previous)
        {
            var window = new Window { Title = T("Alias_Title"), Width = 780, Height = 790, MinWidth = 650, MinHeight = 600,
                WindowStartupLocation = WindowStartupLocation.CenterOwner,
                Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "branding", "chummer.ico")) };
            window.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
            var root = new Grid { Margin = new Thickness(20), RowDefinitions = new RowDefinitions("Auto,*,Auto"), RowSpacing = 12 };
            root.Children.Add(new TextBlock { Text = T("Alias_Instructions"), TextWrapping = TextWrapping.Wrap });
            var panel = new StackPanel { Spacing = 10 };
            var scroll = new ScrollViewer { Content = panel }; Grid.SetRow(scroll, 1); root.Children.Add(scroll);
            var fields = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowSpacing = 8, ColumnSpacing = 12 };
            panel.Children.Add(fields);
            int row = 0;
            void Field(string key, Control control)
            {
                fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
                var label = new TextBlock { Text = T(key), VerticalAlignment = VerticalAlignment.Center };
                Grid.SetRow(label, row); fields.Children.Add(label);
                Grid.SetColumn(control, 1); Grid.SetRow(control, row++); fields.Children.Add(control);
            }
            ComboBox Select(string key, object[] choices, object selected)
            {
                var combo = new ComboBox { ItemsSource = choices, HorizontalAlignment = HorizontalAlignment.Stretch, MinHeight = 32 };
                combo.SelectedItem = choices.FirstOrDefault(item => Equals(item, selected) || item.ToString() == selected?.ToString()) ?? choices.First();
                Field(key, combo); return combo;
            }
            var mode = Select("Alias_Mode", new object[] { "Character-Aware", "Random" }, previous.CharacterAware ? "Character-Aware" : "Random");
            var region = Select("Alias_Region", new object[] { "None" }.Concat(engine.Config.Regions.Cast<object>()).ToArray(), previous.Region);
            var cultures = new[] { new StreetCulture { Id = "", Label = T("Alias_None") } }.Concat(engine.Config.Cultures).ToArray();
            var primary = Select("Alias_Primary", cultures.Cast<object>().ToArray(), cultures.FirstOrDefault(c => c.Id == previous.Primary));
            var secondary = Select("Alias_Secondary", cultures.Cast<object>().ToArray(), cultures.FirstOrDefault(c => c.Id == previous.Secondary));
            var strength = Select("Alias_Strength", engine.Config.Strengths.Keys.Cast<object>().ToArray(), previous.Strength);
            fields = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), RowSpacing = 8, ColumnSpacing = 12 };
            row = 0;
            panel.Children.Add(new Expander { Header = T("Alias_Advanced"), Content = fields, HorizontalAlignment = HorizontalAlignment.Stretch });
            var tone = Select("Alias_Tone", new object[] { "Any", "Serious", "Humorous", "Intimidating", "Technical", "Mystical" }, previous.Tone);
            var length = Select("Alias_Length", new object[] { "Any", "One Word", "Multiple Words" }, previous.Length);
            var theme = Select("Alias_Theme", new object[] { "Automatic" }.Concat(engine.Config.Words.SelectMany(w => w.Themes).Distinct().OrderBy(x => x).Cast<object>()).ToArray(), previous.Theme);
            var origin = Select("Alias_Origin", new object[] { "Automatic" }.Concat(engine.Config.Origins.Keys.Cast<object>()).ToArray(), previous.Origin);
            var locked = new TextBox { Text = previous.Locked, Watermark = T("Alias_Optional"), MaxLength = 16 };
            Field("Alias_Locked", locked);
            var position = Select("Alias_Position", new object[] { "First", "Last" }, previous.LockPosition);
            var used = new TextBox { Text = previous.UsedNames, Watermark = T("Alias_UsedHint"), AcceptsReturn = true, MinHeight = 48 };
            Field("Alias_Used", used);
            var note = new TextBlock { TextWrapping = TextWrapping.Wrap }; panel.Children.Add(note);
            var regenerate = new Button { Content = T("Alias_GenerateFive") }; panel.Children.Add(regenerate);
            var names = new ListBox { MinHeight = 130, MaxHeight = 170 }; panel.Children.Add(names);
            var hook = new TextBlock { TextWrapping = TextWrapping.Wrap }; panel.Children.Add(hook);
            var draft = new TextBox { Text = character.Alias, Watermark = T("Alias_Editable"), MaxLength = 80 }; panel.Children.Add(draft);
            StreetNamePreferences Read() => new StreetNamePreferences {
                CharacterAware = mode.SelectedItem.ToString() == "Character-Aware",
                Region = region.SelectedItem.ToString() == "None" ? "" : region.SelectedItem.ToString(),
                Primary = ((StreetCulture)primary.SelectedItem).Id, Secondary = ((StreetCulture)secondary.SelectedItem).Id,
                Strength = strength.SelectedItem.ToString(), Tone = tone.SelectedItem.ToString(), Length = length.SelectedItem.ToString(),
                Theme = theme.SelectedItem.ToString(), Origin = origin.SelectedItem.ToString(), Locked = locked.Text ?? "",
                LockPosition = position.SelectedItem.ToString(), UsedNames = used.Text ?? "" };
            void Generate()
            {
                try
                {
                    var prefs = Read();
                    var results = engine.Suggest(character, prefs);
                    names.ItemsSource = results;
                    note.Text = string.Join(" ", new[] { ((StreetCulture)primary.SelectedItem).Note, ((StreetCulture)secondary.SelectedItem).Note }.Where(s => !string.IsNullOrEmpty(s)));
                    if (results.Count == 0) note.Text += " " + T("Alias_NoResults");
                    else if (results.Count < 5) note.Text += " " + T("Alias_FewerResults");
                    names.SelectedIndex = results.Count > 0 ? 0 : -1;
                    if (results.Count > 0) draft.Text = results[0].Name;
                }
                catch (Exception error) { AppDiagnostics.Record("alias-generation-failed", error); note.Text = T("Alias_Error"); }
            }
            names.SelectionChanged += (_, _) =>
            {
                if (names.SelectedItem is StreetNameSuggestion selected)
                {
                    draft.Text = selected.Name;
                    hook.Text = T("Alias_OriginSuggestion") + " " + selected.Origin +
                        (selected.Conflict ? " • " + T("Alias_DuplicateWarning") : "") +
                        (string.IsNullOrEmpty(selected.Pronunciation) ? "" : " • " + selected.Pronunciation);
                }
            };
            regenerate.Click += (_, _) => Generate();
            var footer = new StackPanel { Orientation = Orientation.Horizontal, HorizontalAlignment = HorizontalAlignment.Right, Spacing = 10 };
            var cancel = new Button { Content = T("String_Cancel") }; cancel.Click += (_, _) => window.Close(null);
            var apply = new Button { Content = T("Alias_Use") };
            apply.Click += (_, _) => { if (!string.IsNullOrWhiteSpace(draft.Text)) window.Close(new AliasChoice { Name = draft.Text.Trim(), Preferences = Read() }); };
            footer.Children.Add(cancel); footer.Children.Add(apply); Grid.SetRow(footer, 2); root.Children.Add(footer);
            window.Content = root;
            window.KeyDown += (_, e) => { if (e.Key == Key.Escape) { e.Handled = true; window.Close(null); } };
            Generate();
            return await window.ShowDialog<AliasChoice>(Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner.MdiParent ?? owner));
        }
    }
}
