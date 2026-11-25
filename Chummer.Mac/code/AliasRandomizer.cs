using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer
{
    internal static class AliasRandomizer
    {
        internal static void Attach(Forms.Form owner, Character character, Forms.TextBox alias, Forms.Label caption, Forms.Label metatype)
        {
            owner.Load += (_, _) =>
            {
                if (Application.Current != null)
                    Mount(owner, character, alias, caption, metatype);
            };
        }

        private static void Mount(Forms.Form owner, Character character, Forms.TextBox alias, Forms.Label caption, Forms.Label metatype)
        {
            var page = alias.Parent!;
            page.Controls.Remove(alias);
            page.Controls.Remove(caption);
            var binding = new NativeInlineTextBinding(alias, () => true);
            binding.Input.Name = "NativeAlias";
            AutomationProperties.SetName(binding.Input, caption.Text);
            var culture = new MenuItem { Header = LanguageManager.Instance.GetString("Alias_WithCulture") };
            var five = new MenuItem { Header = LanguageManager.Instance.GetString("Alias_GenerateFive") };
            var undo = new MenuItem { Header = LanguageManager.Instance.GetString("Alias_Undo"), IsEnabled = false };
            var menu = new MenuFlyout();
            menu.Items.Add(culture);
            menu.Items.Add(five);
            menu.Items.Add(undo);
            string randomize = LanguageManager.Instance.GetString("Alias_Randomize");
            var buttonCaption = new TextBlock { Text = randomize, TextTrimming = TextTrimming.CharacterEllipsis, MaxWidth = 125 };
            var button = new Avalonia.Controls.SplitButton
            {
                Name = "NativeRandomizeAlias", Flyout = menu,
                Content = buttonCaption
            };
            AutomationProperties.SetName(button, randomize);
            ToolTip.SetTip(button, randomize);
            var label = new TextBlock
            {
                Text = caption.Text, VerticalAlignment = Avalonia.Layout.VerticalAlignment.Center,
                MaxWidth = 100, TextTrimming = TextTrimming.CharacterEllipsis
            };
            ToolTip.SetTip(label, caption.Text);
            var layout = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*,Auto"), ColumnSpacing = 6 };
            layout.Children.Add(label);
            Grid.SetColumn(binding.Input, 1);
            layout.Children.Add(binding.Input);
            Grid.SetColumn(button, 2);
            layout.Children.Add(button);
            layout.SizeChanged += (_, args) =>
            {
                bool compact = args.NewSize.Width < 285;
                label.MaxWidth = compact ? 36 : 100;
                buttonCaption.MaxWidth = compact ? 36 : 125;
            };
            NativeControlPresentation.AddTheme(layout.Styles);
            var presenter = new CreationRegionPresenter(owner, page, layout, "NativeCommonAlias",
                () =>
                {
                    int width = InventoryTreeLayout.VisiblePageSize(owner.MdiParent ?? owner, page).Width;
                    int right = Math.Min(width, metatype.Left);
                    return new System.Drawing.Rectangle(caption.Left, 0, Math.Max(0, right - caption.Left - 8), 32);
                },
                clipToSource: true);
            void Refresh(object sender, EventArgs args) => presenter.Refresh();
            void UpdateCaption(object sender, EventArgs args)
            {
                label.Text = caption.Text;
                ToolTip.SetTip(label, caption.Text);
                AutomationProperties.SetName(binding.Input, caption.Text);
            }
            caption.TextChanged += UpdateCaption;
            caption.LocationChanged += Refresh;
            metatype.LocationChanged += Refresh;
            owner.Disposed += (_, _) =>
            {
                caption.TextChanged -= UpdateCaption;
                caption.LocationChanged -= Refresh;
                metatype.LocationChanged -= Refresh;
                presenter.Dispose();
                binding.Dispose();
                alias.Dispose();
                caption.Dispose();
            };

            bool editing = false;
            string previousAlias = null, generatedAlias = null;
            var preferences = new StreetNamePreferences();
            void Apply(string name)
            {
                previousAlias = alias.Text; generatedAlias = name; alias.Text = name; undo.IsEnabled = true;
            }
            undo.Click += (_, _) => { AppDiagnostics.Action("alias.undo"); if (alias.Text == generatedAlias) alias.Text = previousAlias; undo.IsEnabled = false; };
            alias.TextChanged += (_, _) => { if (alias.Text != generatedAlias) undo.IsEnabled = false; };
            button.Click += (_, _) =>
            {
                try
                {
                    AppDiagnostics.Action("alias.randomize");
                    var results = StreetNameEngine.Load().Suggest(character, new StreetNamePreferences { CharacterAware = true, Strength = "None" }, 1);
                    if (results.Count > 0) Apply(results[0].Name); else AppErrors.Inform(owner, "Alias_NoResults");
                }
                catch (Exception error) { AppDiagnostics.Record("alias-generation-failed", error); AppErrors.Inform(owner, "Alias_Error"); }
            };
            async void Options(object sender, Avalonia.Interactivity.RoutedEventArgs args)
            {
                if (editing) return; editing = true;
                try
                {
                    AppDiagnostics.Action("alias.culture-options");
                    var choice = await AliasGeneratorDialog.ShowAsync(owner, character, StreetNameEngine.Load(), preferences);
                    if (choice != null && Forms.Application.OpenForms.Contains(owner)) { preferences = choice.Preferences; Apply(choice.Name); }
                }
                catch (Exception error) { AppDiagnostics.Record("alias-generation-dialog-failed", error); AppErrors.Inform(owner, "Alias_Error"); }
                finally { editing = false; }
            }
            culture.Click += Options; five.Click += Options;
        }
    }
}
