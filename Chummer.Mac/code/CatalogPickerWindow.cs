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
internal class CatalogPickerWindow<TModel, TDetails> : Window
    where TModel : CatalogPickerModel<TDetails>
    where TDetails : class, ICatalogPickerDetails
{
    internal TModel Model { get; }
    internal ListBox ItemList { get; }
    internal TextBox SearchBox { get; }
    internal Button AcceptButton { get; }
    internal Button AddMoreButton { get; }
    private static string T(string key) => LanguageManager.Instance.GetString(key)
        .Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&");
    private static Binding Read(string property) => new(property);
    private static Binding Edit(string property) => new(property) { Mode = BindingMode.TwoWay };

    internal CatalogPickerWindow(TModel model, Character character, string titleKey, string namePrefix,
        (string Label, string Property)[] detailFields, string? includedLabel = null)
    {
        Model = model; DataContext = model;
        Title = T(titleKey); Width = 940; Height = 650; MinWidth = 740; MinHeight = 460;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "branding", "chummer.ico"));
        Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        var root = new Grid { Margin = new Thickness(18), RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"), RowSpacing = 12 };
        var filters = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,2*,Auto,3*"), ColumnSpacing = 10 };
        filters.Children.Add(new TextBlock { Text = T("Label_Category"), VerticalAlignment = VerticalAlignment.Center, IsVisible = model.Categories.Count > 0 });
        var categories = new ComboBox { IsVisible = model.Categories.Count > 0, MinWidth = 180, HorizontalAlignment = HorizontalAlignment.Stretch };
        categories.Bind(ItemsControl.ItemsSourceProperty, Read(nameof(model.Categories)));
        categories.Bind(SelectingItemsControl.SelectedItemProperty, Edit(nameof(model.Category)));
        Grid.SetColumn(categories, 1); filters.Children.Add(categories);
        var searchLabel = new TextBlock { Text = T("Label_Search"), VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(searchLabel, 2); filters.Children.Add(searchLabel);
        SearchBox = new TextBox { PlaceholderText = T("CatalogPicker_SearchHint"), Name = namePrefix + "Search" };
        SearchBox.Bind(TextBox.TextProperty, Edit(nameof(model.SearchText)));
        if (model.Categories.Count == 0) filters.ColumnDefinitions = new ColumnDefinitions("0,0,Auto,*");
        Grid.SetColumn(SearchBox, 3); filters.Children.Add(SearchBox); root.Children.Add(filters);

        var body = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,Auto,3*"), ColumnSpacing = 10 };
        Grid.SetRow(body, 1); root.Children.Add(body);
        var listPanel = new Grid { RowDefinitions = new RowDefinitions("*,Auto"), MinWidth = 220 };
        ItemList = new ListBox { Name = namePrefix + "Choices", IsTextSearchEnabled = true,
            ItemTemplate = new FuncDataTemplate<CatalogEntry>((entry, _) => new TextBlock {
                Text = entry?.DisplayName, TextWrapping = TextWrapping.Wrap }) };
        // Inset, rounded selection follows the desktop reference. Logical sizes scale
        // with the display; a minimum (not fixed) height permits larger/wrapped text.
        ItemList.Styles.Add(new Style(selector => selector.OfType<ListBoxItem>())
        {
            Setters = { new Setter(Layoutable.MinHeightProperty, 32.0),
                new Setter(TemplatedControl.PaddingProperty, new Thickness(10, 6)),
                new Setter(Layoutable.MarginProperty, new Thickness(5, 1)),
                new Setter(TemplatedControl.CornerRadiusProperty, new CornerRadius(7)) }
        });
        ItemList.Styles.Add(new Style(selector => selector.OfType<ListBoxItem>().Class(":selected"))
        {
            Setters = { new Setter(TemplatedControl.FontWeightProperty, FontWeight.SemiBold) }
        });
        // Scope selection colors to this list and retain light/dark text contrast.
        // Fluent uses these brushes for selected, hovered-selected and pressed rows.
        foreach (var theme in new[] { (ThemeVariant.Dark, "#484848", "#525252", "#5C5C5C"),
            (ThemeVariant.Light, "#DCDCDC", "#D0D0D0", "#C4C4C4") })
        {
            var resources = new ResourceDictionary
            {
                ["SystemControlHighlightListAccentLowBrush"] = new SolidColorBrush(Color.Parse(theme.Item2)),
                ["SystemControlHighlightListAccentMediumBrush"] = new SolidColorBrush(Color.Parse(theme.Item3)),
                ["SystemControlHighlightListAccentHighBrush"] = new SolidColorBrush(Color.Parse(theme.Item4))
            };
            ItemList.Resources.ThemeDictionaries[theme.Item1] = resources;
        }
        ItemList.Bind(ItemsControl.ItemsSourceProperty, Read(nameof(model.Items)));
        ItemList.Bind(SelectingItemsControl.SelectedItemProperty, Edit(nameof(model.SelectedEntry)));
        TextSearch.SetTextBinding(ItemList, Read(nameof(CatalogEntry.DisplayName)));
        listPanel.Children.Add(ItemList);
        var empty = new TextBlock { Text = T("CatalogPicker_NoMatches"), TextWrapping = TextWrapping.Wrap, Margin = new Thickness(4) };
        empty.Bind(IsVisibleProperty, Read(nameof(model.IsEmpty))); Grid.SetRow(empty, 1); listPanel.Children.Add(empty);
        body.Children.Add(listPanel);
        var splitter = new GridSplitter { Width = 6, ResizeDirection = GridResizeDirection.Columns, HorizontalAlignment = HorizontalAlignment.Center };
        Grid.SetColumn(splitter, 1); body.Children.Add(splitter);
        var details = new StackPanel { Spacing = 10, MinWidth = 260 };
        var name = new TextBlock { FontSize = 20, FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
        name.Bind(TextBlock.TextProperty, Read("SelectedEntry.DisplayName")); details.Children.Add(name);
        if (model is IRatedCatalogPicker)
        {
            var ratingRow = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            ratingRow.Children.Add(new TextBlock { Text = T("Label_Rating"), VerticalAlignment = VerticalAlignment.Center });
            var rating = NativeIntegerInput.Create(namePrefix + "Rating", 1, 1);
            rating.Bind(NumericUpDown.MinimumProperty, Read(nameof(IRatedCatalogPicker.MinimumRating)));
            rating.Bind(NumericUpDown.MaximumProperty, Read(nameof(IRatedCatalogPicker.MaximumRating)));
            rating.Bind(NumericUpDown.ValueProperty, Edit(nameof(IRatedCatalogPicker.Rating)));
            rating.Bind(IsEnabledProperty, Read(nameof(IRatedCatalogPicker.IsRatingEnabled)));
            ratingRow.Children.Add(rating); details.Children.Add(ratingRow);
        }
        var fields = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*"), ColumnSpacing = 14, RowSpacing = 8 };
        details.Children.Add(fields);
        void Detail(string labelKey, string property)
        {
            int row = fields.RowDefinitions.Count; fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = new TextBlock { Text = T(labelKey), VerticalAlignment = VerticalAlignment.Top };
            var text = new TextBlock { Name = namePrefix + property, TextWrapping = TextWrapping.Wrap };
            text.Bind(TextBlock.TextProperty, Read("Details." + property));
            Grid.SetRow(label, row); Grid.SetRow(text, row); Grid.SetColumn(text, 1);
            fields.Children.Add(label); fields.Children.Add(text);
        }
        foreach (var field in detailFields) Detail(field.Label, field.Property);
        var source = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        source.Bind(ContentProperty, Read("Details.Source")); source.Bind(IsEnabledProperty, Read(nameof(model.CanAccept)));
        source.Bind(ToolTip.TipProperty, Read("Details.SourceTooltip")); details.Children.Add(source);
        var sourceWarning = new TextBlock { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap };
        details.Children.Add(sourceWarning);
        if (includedLabel != null)
        {
            details.Children.Add(new TextBlock { Text = T(includedLabel), TextWrapping = TextWrapping.Wrap });
            var included = new TextBlock { TextWrapping = TextWrapping.Wrap };
            included.Bind(TextBlock.TextProperty, Read("Details.Included")); details.Children.Add(included);
        }
        var detailScroll = new ScrollViewer { Content = details, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetColumn(detailScroll, 2); body.Children.Add(detailScroll);

        var status = new TextBlock { Foreground = Brushes.Firebrick, TextWrapping = TextWrapping.Wrap };
        status.Bind(TextBlock.TextProperty, Read(nameof(model.Error))); Grid.SetRow(status, 2); root.Children.Add(status);
        var footer = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        Grid.SetRow(footer, 3); root.Children.Add(footer);
        var purchase = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 8, VerticalAlignment = VerticalAlignment.Center };
        purchase.IsVisible = model.IsCareer;
        var free = new CheckBox { Name = namePrefix + "FreeCost", Content = T("Checkbox_Free"), VerticalAlignment = VerticalAlignment.Center };
        free.Bind(ToggleButton.IsCheckedProperty, Edit(nameof(model.FreeCost))); purchase.Children.Add(free);
        purchase.Children.Add(new TextBlock { Text = T("Label_SelectGear_Markup"), VerticalAlignment = VerticalAlignment.Center });
        var markup = NativeIntegerInput.Create(namePrefix + "Markup", -99, 1000);
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
        ItemList.DoubleTapped += (_, _) => Accept(false);
        SearchBox.KeyDown += (_, args) =>
        {
            if (args.Key is not (Key.Up or Key.Down) || model.Items.Count == 0) return;
            int current = model.Items.ToList().IndexOf(model.SelectedEntry!);
            int next = (current + (args.Key == Key.Down ? 1 : -1) + model.Items.Count) % model.Items.Count;
            model.SelectedEntry = model.Items[next]; ItemList.ScrollIntoView(model.SelectedEntry); args.Handled = true;
        };
        source.Click += (_, _) =>
        {
            if (model.Details == null) return;
            sourceWarning.Text = "";
            IsEnabled = false;
            try { SourcebookReader.Open(model.Details.Source, character); }
            catch (Exception error) { AppDiagnostics.Record("catalog-picker.source-failed", error); sourceWarning.Text = T("Message_PdfOpenFailed"); }
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
