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
using Avalonia.Markup.Xaml.MarkupExtensions;
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
    protected StackPanel DetailPanel { get; private set; } = null!;
    protected static string T(string key) => LanguageManager.Instance.GetString(key)
        .Replace("&&", "\u0001").Replace("&", "").Replace("\u0001", "&");
    private static Binding Read(string property) => new(property);
    private static Binding Edit(string property) => new(property) { Mode = BindingMode.TwoWay };

    internal CatalogPickerWindow(TModel model, Character character, string titleKey, string namePrefix,
        (string Label, string Property)[] detailFields, string? includedLabel = null,
        bool freeDuringCreation = false, bool showMarkup = true, string? freeVisibilityProperty = null)
    {
        Model = model; DataContext = model;
        Title = T(titleKey); Width = 940; Height = 650; MinWidth = 360; MinHeight = 380;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        Icon = new WindowIcon(Path.Combine(AppContext.BaseDirectory, "branding", "chummer.ico"));
        NativeControlPresentation.AddTheme(Styles);
        var root = new Grid { Margin = new Thickness(18), RowDefinitions = new RowDefinitions("Auto,*,Auto,Auto"), RowSpacing = 12 };
        var filters = new Grid { Name = "CatalogFilters", ColumnDefinitions = new ColumnDefinitions("Auto,2*,Auto,3*"), ColumnSpacing = 10, RowSpacing = 4 };
        bool hasCategories = model.Categories.Count > 0;
        var categoryLabel = new TextBlock { Text = T("Label_Category"), TextWrapping = TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center, IsVisible = hasCategories };
        filters.Children.Add(categoryLabel);
        var categories = new ComboBox { IsVisible = hasCategories, MinWidth = 120, HorizontalAlignment = HorizontalAlignment.Stretch };
        categories.Bind(ItemsControl.ItemsSourceProperty, Read(nameof(model.Categories)));
        categories.Bind(SelectingItemsControl.SelectedItemProperty, Edit(nameof(model.Category)));
        Grid.SetColumn(categories, 1); filters.Children.Add(categories);
        var searchLabel = new TextBlock { Text = T("Label_Search"), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(searchLabel, 2); filters.Children.Add(searchLabel);
        SearchBox = new TextBox { PlaceholderText = T("CatalogPicker_SearchHint"), Name = namePrefix + "Search" };
        SearchBox.Bind(TextBox.TextProperty, Edit(nameof(model.SearchText)));
        if (model.Categories.Count == 0) filters.ColumnDefinitions = new ColumnDefinitions("0,0,Auto,*");
        Grid.SetColumn(SearchBox, 3); filters.Children.Add(SearchBox); root.Children.Add(filters);

        var body = new Grid { Name = "CatalogBody", RowSpacing = 6, ColumnDefinitions = new ColumnDefinitions("2*,Auto,3*"), ColumnSpacing = 10 };
        Grid.SetRow(body, 1); root.Children.Add(body);
        var listPanel = new Grid { RowDefinitions = new RowDefinitions("*,Auto") };
        ItemList = new ListBox { Name = namePrefix + "Choices", IsTextSearchEnabled = true,
            ItemTemplate = new FuncDataTemplate<CatalogEntry>((entry, _) => new TextBlock {
                Text = entry?.DisplayName, TextWrapping = TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center }) };
        ScrollViewer.SetHorizontalScrollBarVisibility(ItemList, ScrollBarVisibility.Disabled);
        ScrollViewer.SetVerticalScrollBarVisibility(ItemList, ScrollBarVisibility.Auto);
        // Inset, rounded selection follows the desktop reference. Logical sizes scale
        // with the display; a minimum (not fixed) height permits larger/wrapped text.
        ItemList.Styles.Add(new Style(selector => selector.OfType<ListBoxItem>())
        {
            Setters = { new Setter(Layoutable.MinHeightProperty, 32.0),
                new Setter(ContentControl.HorizontalContentAlignmentProperty, HorizontalAlignment.Stretch),
                new Setter(ContentControl.VerticalContentAlignmentProperty, VerticalAlignment.Center),
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
        var details = new StackPanel { Spacing = 10 };
        DetailPanel = details;
        var name = new TextBlock { Name = "CatalogSelectedName", FontWeight = FontWeight.SemiBold, TextWrapping = TextWrapping.Wrap };
        name.Bind(TextBlock.TextProperty, Read("SelectedEntry.DisplayName")); details.Children.Add(name);
        if (model is IRatedCatalogPicker)
        {
            var ratingRow = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
            var ratingLabel = new TextBlock { Text = T("Label_Rating"), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
            if (model is ILabeledRatingCatalogPicker) ratingLabel.Bind(TextBlock.TextProperty, Read(nameof(ILabeledRatingCatalogPicker.RatingLabel)));
            ratingRow.Children.Add(ratingLabel);
            var rating = NativeIntegerInput.Create(namePrefix + "Rating", 1, 1);
            rating.Bind(NumericUpDown.MinimumProperty, Read(nameof(IRatedCatalogPicker.MinimumRating)));
            rating.Bind(NumericUpDown.MaximumProperty, Read(nameof(IRatedCatalogPicker.MaximumRating)));
            rating.Bind(NumericUpDown.ValueProperty, Edit(nameof(IRatedCatalogPicker.Rating)));
            rating.Bind(IsEnabledProperty, Read(nameof(IRatedCatalogPicker.IsRatingEnabled)));
            ratingRow.Children.Add(rating); NativeControlPresentation.ConstrainWrapItems(ratingRow); details.Children.Add(ratingRow);
        }
        if (model is IQuantityCatalogPicker)
        {
            var quantityRow = new WrapPanel { ItemSpacing = 12, LineSpacing = 4 };
            quantityRow.Children.Add(new TextBlock { Text = T("Label_Qty"), VerticalAlignment = VerticalAlignment.Center });
            var quantity = NativeIntegerInput.Create(namePrefix + "Quantity", 1, 1000);
            if (model is IRangedQuantityCatalogPicker)
            {
                quantity.Bind(NumericUpDown.MinimumProperty, Read(nameof(IRangedQuantityCatalogPicker.MinimumQuantity)));
                quantity.Bind(NumericUpDown.MaximumProperty, Read(nameof(IRangedQuantityCatalogPicker.MaximumQuantity)));
                quantity.Bind(NumericUpDown.IncrementProperty, Read(nameof(IRangedQuantityCatalogPicker.QuantityIncrement)));
            }
            quantity.Bind(NumericUpDown.ValueProperty, Edit(nameof(IQuantityCatalogPicker.Quantity)));
            quantity.Bind(IsEnabledProperty, Read(nameof(IQuantityCatalogPicker.IsQuantityEnabled)));
            quantityRow.Children.Add(quantity); NativeControlPresentation.ConstrainWrapItems(quantityRow); details.Children.Add(quantityRow);
        }
        var fields = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,3*"), ColumnSpacing = 10, RowSpacing = 8 };
        details.Children.Add(fields);
        void Detail(string labelKey, string property)
        {
            int row = fields.RowDefinitions.Count; fields.RowDefinitions.Add(new RowDefinition(GridLength.Auto));
            var label = new TextBlock { Text = T(labelKey), TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Top };
            var text = new TextBlock { Name = namePrefix + property, TextWrapping = TextWrapping.Wrap };
            text.Bind(TextBlock.TextProperty, Read("Details." + property));
            Grid.SetRow(label, row); Grid.SetRow(text, row); Grid.SetColumn(text, 1);
            fields.Children.Add(label); fields.Children.Add(text);
        }
        foreach (var field in detailFields) Detail(field.Label, field.Property);
        var source = new Button { HorizontalAlignment = HorizontalAlignment.Left };
        source.Bind(ContentProperty, Read("Details.Source")); source.Bind(IsEnabledProperty, Read(nameof(model.CanAccept)));
        source.Bind(ToolTip.TipProperty, Read("Details.SourceTooltip")); details.Children.Add(source);
        var sourceWarning = new TextBlock { Name = namePrefix + "SourceWarning", TextWrapping = TextWrapping.Wrap };
        sourceWarning.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlErrorTextForegroundBrush"));
        details.Children.Add(sourceWarning);
        if (includedLabel != null)
        {
            details.Children.Add(new TextBlock { Text = T(includedLabel), TextWrapping = TextWrapping.Wrap });
            var included = new TextBlock { TextWrapping = TextWrapping.Wrap };
            included.Bind(TextBlock.TextProperty, Read("Details.Included")); details.Children.Add(included);
        }
        var detailScroll = new ScrollViewer { Content = details, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled };
        Grid.SetColumn(detailScroll, 2); body.Children.Add(detailScroll);

        // Use the framework semantic error brush so live owner-theme changes retain contrast.
        var status = new TextBlock { Name = namePrefix + "Status", TextWrapping = TextWrapping.Wrap };
        status.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlErrorTextForegroundBrush"));
        status.Bind(TextBlock.TextProperty, Read(nameof(model.Error))); Grid.SetRow(status, 2); root.Children.Add(status);
        var footer = new Grid { Name = "CatalogFooter", RowSpacing = 5, ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 12 };
        Grid.SetRow(footer, 3); root.Children.Add(footer);
        var purchase = new WrapPanel { ItemSpacing = 8, LineSpacing = 3, VerticalAlignment = VerticalAlignment.Center };
        purchase.IsVisible = model.IsCareer || freeDuringCreation;
        var free = new CheckBox { Name = namePrefix + "FreeCost", Content = T("Checkbox_Free"), VerticalAlignment = VerticalAlignment.Center };
        free.Bind(ToggleButton.IsCheckedProperty, Edit(nameof(model.FreeCost))); purchase.Children.Add(free);
        if (freeVisibilityProperty != null) free.Bind(IsVisibleProperty, Read(freeVisibilityProperty));
        if (showMarkup)
        {
            purchase.Children.Add(new TextBlock { Text = T("Label_SelectGear_Markup"), VerticalAlignment = VerticalAlignment.Center });
            var markup = NativeIntegerInput.Create(namePrefix + "Markup", -99, 1000);
            markup.Bind(NumericUpDown.ValueProperty, Edit(nameof(model.Markup))); purchase.Children.Add(markup);
            purchase.Children.Add(new TextBlock { Text = "%", VerticalAlignment = VerticalAlignment.Center });
        }
        footer.Children.Add(purchase);
        var buttons = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        var cancel = new Button { Content = T("String_Cancel"), IsCancel = true }; cancel.Click += (_, _) => Close();
        AddMoreButton = new Button { Content = T("String_AddMore") };
        AcceptButton = new Button { Content = T("String_OK"), IsDefault = true };
        AddMoreButton.Bind(IsEnabledProperty, Read(nameof(model.CanAccept))); AcceptButton.Bind(IsEnabledProperty, Read(nameof(model.CanAccept)));
        AddMoreButton.Click += (_, _) => Accept(true); AcceptButton.Click += (_, _) => Accept(false);
        buttons.Children.Add(cancel); buttons.Children.Add(AddMoreButton); buttons.Children.Add(AcceptButton);
        Grid.SetColumn(buttons, 1); footer.Children.Add(buttons);
        var wrapCaption = new FuncDataTemplate<string>((text, _) =>
            new AccessText { Text = text, TextWrapping = TextWrapping.Wrap });
        cancel.ContentTemplate = AddMoreButton.ContentTemplate = AcceptButton.ContentTemplate = free.ContentTemplate = wrapCaption;
        source.ContentTemplate = wrapCaption;
        NativeControlPresentation.ConstrainWrapItems(purchase);
        NativeControlPresentation.ConstrainWrapItems(buttons);
        bool narrow = false;
        void Reflow()
        {
            double font = Avalonia.Controls.Documents.TextElement.GetFontSize(root);
            name.FontSize = font * 1.45;
            bool stack = root.Bounds.Width < Math.Max(640, font * 43);
            if (stack == narrow) return;

            narrow = stack;
            filters.ColumnDefinitions = new ColumnDefinitions(narrow ? "2*,3*" :
                hasCategories ? "Auto,2*,Auto,3*" : "0,0,Auto,*");
            filters.RowDefinitions = new RowDefinitions(narrow && hasCategories ? "Auto,Auto" : "Auto");
            Grid.SetRow(searchLabel, narrow && hasCategories ? 1 : 0);
            Grid.SetRow(SearchBox, narrow && hasCategories ? 1 : 0);
            Grid.SetColumn(searchLabel, narrow ? 0 : 2);
            Grid.SetColumn(SearchBox, narrow ? 1 : 3);
            body.ColumnDefinitions = new ColumnDefinitions(narrow ? "*" : "2*,Auto,3*");
            body.RowDefinitions = new RowDefinitions(narrow ? "*,*" : "*");
            Grid.SetRow(detailScroll, narrow ? 1 : 0);
            Grid.SetColumn(detailScroll, narrow ? 0 : 2);
            splitter.IsVisible = !narrow;
            footer.ColumnDefinitions = new ColumnDefinitions(narrow ? "*" : "*,Auto");
            footer.RowDefinitions = new RowDefinitions(narrow ? "Auto,Auto" : "Auto");
            Grid.SetRow(buttons, narrow ? 1 : 0);
            Grid.SetColumn(buttons, narrow ? 0 : 1);
        }
        root.SizeChanged += (_, _) => Reflow();
        root.PropertyChanged += (_, args) =>
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty) Reflow();
        };
        Reflow();
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
