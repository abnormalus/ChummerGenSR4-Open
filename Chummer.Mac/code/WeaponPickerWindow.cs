#nullable enable
using System;
using System.IO;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Styling;

namespace Chummer;

// Standard Avalonia controls own layout, selection, scrolling, text search and redraw.
// Bindings consume the observable model; this window contains no weapon rules or inventory writes.
internal sealed class WeaponPickerWindow : Window
{
    internal WeaponPickerModel Model { get; }
    internal ListBox WeaponList { get; }
    internal TextBox SearchBox { get; }
    internal Button AcceptButton { get; }
    internal Button AddMoreButton { get; }
    private static string T(string key) => LanguageManager.Instance.GetString(key)
        .Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&");
    private static Binding Read(string property) => new(property);
    private static Binding Edit(string property) => new(property) { Mode = BindingMode.TwoWay };

    internal WeaponPickerWindow(WeaponPickerModel model, Character character)
    {
        Model = model; DataContext = model;
        Title = T("Title_SelectWeapon"); Width = 940; Height = 650; MinWidth = 740; MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "branding", "chummer.ico"));
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        var root = new Grid { Margin = new Thickness(18), RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"), RowSpacing = 12 };
        var filters = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,2*,Auto,3*"), ColumnSpacing = 10 };
        filters.Children.Add(new TextBlock { Text = T("Label_Category"), VerticalAlignment = VerticalAlignment.Center });
        var categories = new ComboBox { MinWidth = 180, HorizontalAlignment = HorizontalAlignment.Stretch };
        categories.Bind(ItemsControl.ItemsSourceProperty, Read(nameof(model.Categories)));
        categories.Bind(SelectingItemsControl.SelectedItemProperty, Edit(nameof(model.Category)));
        Grid.SetColumn(categories, 1); filters.Children.Add(categories);
        var searchLabel = new TextBlock { Text = T("Label_Search"), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(searchLabel, 2); filters.Children.Add(searchLabel);
        SearchBox = new TextBox { PlaceholderText = T("WeaponPicker_SearchHint"), Name = "weaponSearch" };
        SearchBox.Bind(TextBox.TextProperty, Edit(nameof(model.SearchText)));
        Grid.SetColumn(SearchBox, 3); filters.Children.Add(SearchBox); root.Children.Add(filters);

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,Auto,3*"), ColumnSpacing = 10 };
        Grid.SetRow(body, 1); root.Children.Add(body);
        var listPanel = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), MinWidth = 220 };
        WeaponList = new ListBox { Name = "weaponChoices", IsTextSearchEnabled = true,
            ItemTemplate = new FuncDataTemplate<WeaponCatalogEntry>((entry, _) => new TextBlock {
                Text = entry?.DisplayName, TextWrapping = TextWrapping.Wrap }) };
        // Compact desktop rows; minimum size rather than fixed height keeps wrapped
        // names and larger accessibility fonts readable.
        WeaponList.Styles.Add(new Style(selector => selector.OfType<ListBoxItem>())
        {
            Setters = { new Setter(Layoutable.MinHeightProperty, 26.0),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(6, 2)) }
        });
        WeaponList.Bind(ItemsControl.ItemsSourceProperty, Read(nameof(model.Items)));
        WeaponList.Bind(SelectingItemsControl.SelectedItemProperty, Edit(nameof(model.SelectedEntry)));
        TextSearch.SetTextBinding(WeaponList, Read(nameof(WeaponCatalogEntry.DisplayName)));
        listPanel.Children.Add(WeaponList);
        var empty = new TextBlock { Text = T("WeaponPicker_NoMatches"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) };
        empty.Bind(IsVisibleProperty, Read(nameof(model.IsEmpty))); Grid.SetRow(empty, 1); listPanel.Children.Add(empty);
        body.Children.Add(listPanel);
        var splitter = new GridSplitter { Width = 6, ResizeDirection = GridResizeDirection.Columns, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumn(splitter, 1); body.Children.Add(splitter);
        var details = new StackPanel { Spacing = 10, MinWidth = 260 };
        var name = new TextBlock { FontSize = 20, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
        name.Bind(TextBlock.TextProperty, Read("SelectedEntry.DisplayName")); details.Children.Add(name);
        var fields = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 14, RowSpacing = 8 };
        details.Children.Add(fields);
        void Detail(string labelKey, string property)
        {
            int row = fields.RowDefinitions.Count; fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = new TextBlock { Text = T(labelKey), VerticalAlignment = VerticalAlignment.Top };
            var text = new TextBlock { Name = "weapon" + property, TextWrapping = TextWrapping.Wrap };
            text.Bind(TextBlock.TextProperty, Read("Details." + property));
            Grid.SetRow(label, row); Grid.SetRow(text, row); Grid.SetColumn(text, 1);
            fields.Children.Add(label); fields.Children.Add(text);
        }
        Detail("Label_Damage", "Damage"); Detail("Label_AP", "ArmorPenetration"); Detail("Label_Reach", "Reach");
        Detail("Label_Mode", "Mode"); Detail("Label_RC", "RecoilCompensation"); Detail("Label_Ammo", "Ammo");
        Detail("Label_Avail", "Availability"); Detail("Label_Cost", "Cost"); Detail("Label_Test", "AvailabilityTest");
        var source = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        source.Bind(ContentProperty, Read("Details.Source")); source.Bind(IsEnabledProperty, Read(nameof(model.CanAccept)));
        source.Bind(ToolTip.TipProperty, Read("Details.SourceTooltip")); details.Children.Add(source);
        var sourceWarning = new TextBlock { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap };
        details.Children.Add(sourceWarning);
        details.Children.Add(new TextBlock { Text = T("Label_SelectWeapon_IncludedItems"), TextWrapping = TextWrapping.Wrap });
        var included = new TextBlock { TextWrapping = TextWrapping.Wrap }; included.Bind(TextBlock.TextProperty, Read("Details.Included")); details.Children.Add(included);
        var detailScroll = new ScrollViewer { Content = details, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetColumn(detailScroll, 2); body.Children.Add(detailScroll);

        var status = new TextBlock { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap };
        status.Bind(TextBlock.TextProperty, Read(nameof(model.Error))); Grid.SetRow(status, 2); root.Children.Add(status);
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        Grid.SetRow(footer, 3); root.Children.Add(footer);
        var purchase = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        purchase.IsVisible = model.IsCareer;
        var free = new CheckBox { Name = "weaponFreeCost", Content = T("Checkbox_Free"), VerticalAlignment = VerticalAlignment.Center };
        free.Bind(ToggleButton.IsCheckedProperty, Edit(nameof(model.FreeCost))); purchase.Children.Add(free);
        purchase.Children.Add(new TextBlock { Text = T("Label_SelectGear_Markup"), VerticalAlignment = VerticalAlignment.Center });
        var markup = new NumericUpDown { Name = "weaponMarkup", Minimum = -99, Maximum = 1000, Increment = 1, Width = 100, FormatString = "0" };
        markup.Bind(NumericUpDown.ValueProperty, Edit(nameof(model.Markup))); purchase.Children.Add(markup);
        purchase.Children.Add(new TextBlock { Text = "%", VerticalAlignment = VerticalAlignment.Center }); footer.Children.Add(purchase);
        var buttons = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8 };
        var cancel = new Button { Content = T("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        AddMoreButton = new Button { Content = T("String_AddMore") };
        AcceptButton = new Button { Content = T("String_OK"), IsDefault = true };
        AddMoreButton.Bind(IsEnabledProperty, Read(nameof(model.CanAccept))); AcceptButton.Bind(IsEnabledProperty, Read(nameof(model.CanAccept)));
        AddMoreButton.Click += (_, _) => Accept(true); AcceptButton.Click += (_, _) => Accept(false);
        buttons.Children.Add(cancel); buttons.Children.Add(AddMoreButton); buttons.Children.Add(AcceptButton);
        Grid.SetColumn(buttons, 1); footer.Children.Add(buttons);
        Content = root;
        WeaponList.DoubleTapped += (_, _) => Accept(false);
        SearchBox.KeyDown += (_, args) =>
        {
            if (args.Key is not (Key.Up or Key.Down) || model.Items.Count == 0) return;
            int current = model.Items.ToList().IndexOf(model.SelectedEntry!);
            int next = (current + (args.Key == Key.Down ? 1 : -1) + model.Items.Count) % model.Items.Count;
            model.SelectedEntry = model.Items[next]; WeaponList.ScrollIntoView(model.SelectedEntry); args.Handled = true;
        };
        source.Click += (_, _) =>
        {
            if (model.Details == null) return;
            sourceWarning.Text = "";
            IsEnabled = false;
            try { SourcebookReader.Open(model.Details.Source, character); }
            catch (Exception error) { AppDiagnostics.Record("weapon-picker.source-failed", error); sourceWarning.Text = T("Message_PdfOpenFailed"); }
            finally { IsEnabled = true; }
        };
        Opened += (_, _) => SearchBox.Focus();
        Activated += (_, _) => model.Refresh();
    }

    internal void Accept(bool addAgain)
    {
        var selection = Model.Accept(addAgain);
        if (selection != null) Close(selection);
    }
}
