#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed class CreationActiveSkillRow : IDisposable
{
    private readonly SkillControl controller;
    private readonly NativeInlineNumericBinding rating;
    private readonly NativeInlineChoiceBinding specialization;
    private readonly List<Action> cleanup = new();
    private bool refreshing, disposed;
    internal Grid Layout { get; }
    internal NumericUpDown Rating => rating.Input;
    internal AutoCompleteBox Specialization => specialization.Input;
    internal Button BreakGroup { get; }
    internal SkillSpecializationDisclosure SpecializationDisclosure { get; }
    internal Button? PerceptionDisclosure { get; }
    private readonly TextBlock name = Caption(), attribute = Caption(), pool = Caption();
    private static TextBlock Caption()
    {
        var text = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap,
            VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(3, 2) };
        text.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
        return text;
    }
    internal CreationActiveSkillRow(SkillControl controller, int index)
    {
        this.controller = controller;
        if (controller.Visible) controller.CreateControl();
        rating = new NativeInlineNumericBinding(controller.CreationRatingInput, controller.CreationToolTips, true,
            () => controller.Visible && controller.CreationRatingInput.Visible);
        specialization = new NativeInlineChoiceBinding(controller.CreationSpecializationInput);
        Rating.ClearValue(NumericUpDown.FontSizeProperty);
        Rating.ClearValue(Layoutable.WidthProperty);
        Rating.Name = "nativeCreationSkillRating" + index;
        Specialization.Name = "nativeCreationSkillSpecialization" + index;
        Specialization.PlaceholderText = LanguageManager.Instance.GetString("String_ExpenseSpecialization");
        Layout = new Grid { ColumnDefinitions = new ColumnDefinitions("2*,Auto,2*,Auto,2*"),
            RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnSpacing = 3, RowSpacing = 2, Margin = new Thickness(2) };
        // Mirror the controller's trained-row emphasis with a live theme brush.
        Layout.Styles.Add(new Style(selector => selector.OfType<Grid>().Class("trained"))
        {
            Setters = { new Setter(Panel.BackgroundProperty, new DynamicResourceExtension("SystemControlBackgroundListLowBrush")) }
        });
        var heading = new Grid { ColumnDefinitions = new ColumnDefinitions("Auto,*") };
        if (!controller.SkillIsMeta && controller.SkillName == "Perception")
        {
            PerceptionDisclosure = new Button { MinHeight = 32, Padding = new Thickness(5, 2) };
            Avalonia.Automation.AutomationProperties.SetName(PerceptionDisclosure, "Expand or collapse Perception subcategories");
            EventHandler<RoutedEventArgs> disclose = (_, _) =>
            {
                if (!disposed && controller.Visible && controller.Enabled && PerceptionDisclosure.IsEffectivelyEnabled)
                    SkillDisplayPreferences.PerceptionExpanded = !SkillDisplayPreferences.PerceptionExpanded;
            };
            PerceptionDisclosure.Click += disclose; cleanup.Add(() => PerceptionDisclosure.Click -= disclose);
            heading.Children.Add(PerceptionDisclosure);
        }
        Grid.SetColumn(name, 1);
        heading.Children.Add(name); Layout.Children.Add(heading); Grid.SetColumn(attribute, 1); Layout.Children.Add(attribute);
        Grid.SetColumn(Rating, 2); Layout.Children.Add(Rating); Grid.SetColumn(pool, 3); Layout.Children.Add(pool);
        var actions = new WrapPanel { ItemSpacing = 2, LineSpacing = 2 };
        BreakGroup = Command(controller.CreationBreakButton); actions.Children.Add(BreakGroup);
        actions.Children.Add(Command(controller.CreationDeleteButton)); Grid.SetColumn(actions, 4); Layout.Children.Add(actions);
        var specializationLine = new Grid(); specializationLine.Children.Add(Specialization);
        Grid.SetRow(specializationLine, 1); Grid.SetColumnSpan(specializationLine, 5); Layout.Children.Add(specializationLine);
        SpecializationDisclosure = new SkillSpecializationDisclosure(specializationLine, "Show or hide skill specialization",
            () => controller.Visible && controller.Enabled && controller.CreationSpecializationInput.Visible);
        actions.Children.Add(SpecializationDisclosure.Toggle);
        actions.SizeChanged += (_, args) =>
        {
            if (args.NewSize.Width <= 0) return;
            foreach (var command in actions.Children)
                command.MaxWidth = args.NewSize.Width;
        };
        void Reflow()
        {
            bool stacked = Layout.Bounds.Width < Math.Max(400, Rating.FontSize * 24);
            if (stacked == (Grid.GetRow(Rating) == 1)) return;

            Layout.ColumnDefinitions = new ColumnDefinitions(stacked ? "*,3*,*" : "2*,Auto,2*,Auto,2*");
            if (!stacked) Layout.ColumnDefinitions[2].MinWidth = 104;
            Layout.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto,Auto,Auto" : "Auto,Auto");
            Grid.SetColumnSpan(heading, stacked ? 3 : 1);
            Grid.SetRow(attribute, stacked ? 1 : 0);
            Grid.SetColumn(attribute, stacked ? 0 : 1);
            Grid.SetRow(Rating, stacked ? 1 : 0);
            Grid.SetColumn(Rating, stacked ? 1 : 2);
            Grid.SetRow(pool, stacked ? 1 : 0);
            Grid.SetColumn(pool, stacked ? 2 : 3);
            Grid.SetRow(actions, stacked ? 2 : 0);
            Grid.SetColumn(actions, stacked ? 0 : 4);
            Grid.SetColumnSpan(actions, stacked ? 3 : 1);
            Grid.SetRow(specializationLine, stacked ? 3 : 1);
            Grid.SetColumnSpan(specializationLine, stacked ? 3 : 5);
        }
        Layout.SizeChanged += (_, _) => Reflow();
        Layout.PropertyChanged += (_, args) =>
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty) Reflow();
        };
        Reflow();
        var sources = new Forms.Control[] { controller, controller.CreationNameLabel, controller.CreationAttributeLabel,
            controller.CreationPoolLabel, controller.CreationRatingInput, controller.CreationSpecializationInput };
        EventHandler changed = (_, _) => Refresh();
        Forms.InvalidateEventHandler invalidated = (_, _) => Refresh();
        foreach (var source in sources)
        {
            source.TextChanged += changed; source.VisibleChanged += changed; source.EnabledChanged += changed; source.Invalidated += invalidated;
            source.FontChanged += changed; source.BackColorChanged += changed;
            cleanup.Add(() => { source.TextChanged -= changed; source.VisibleChanged -= changed;
                source.EnabledChanged -= changed; source.Invalidated -= invalidated;
                source.FontChanged -= changed; source.BackColorChanged -= changed; });
        }
        EventHandler<Avalonia.Input.PointerPressedEventArgs> focus = (_, _) => { if (!disposed) Rating.Focus(); };
        name.PointerPressed += focus; cleanup.Add(() => name.PointerPressed -= focus);
        Refresh();
    }
    private Button Command(Forms.Button source)
    {
        var caption = new TextBlock { TextWrapping = TextWrapping.Wrap };
        var target = new Button { MinHeight = 32, Padding = new Thickness(6, 2), Content = caption };
        void Sync()
        {
            if (disposed) return;
            caption.Text = source.Text; target.IsVisible = source.Visible; target.IsEnabled = source.Enabled;
            ToolTip.SetTip(target, controller.CreationToolTips.GetToolTip(source));
        }
        EventHandler changed = (_, _) => Sync();
        EventHandler<RoutedEventArgs> click = (_, _) => { if (!disposed) source.PerformClick(); };
        source.TextChanged += changed; source.VisibleChanged += changed; source.EnabledChanged += changed; target.Click += click;
        cleanup.Add(() => { source.TextChanged -= changed; source.VisibleChanged -= changed;
            source.EnabledChanged -= changed; target.Click -= click; });
        Sync(); return target;
    }
    internal void Refresh()
    {
        if (disposed || refreshing) return;
        refreshing = true;
        try
        {
            if (controller.Visible && !controller.Created) controller.CreateControl();
            Layout.IsVisible = controller.Visible && (!controller.SkillIsMeta || controller.SkillMetaBase != "Perception" || SkillDisplayPreferences.PerceptionExpanded);
            if (PerceptionDisclosure != null) PerceptionDisclosure.Content = SkillDisplayPreferences.PerceptionExpanded ? "⌄" : ">";
            if (controller.SkillIsMeta && controller.SkillMetaBase == "Perception") name.Margin = new Thickness(16, 2, 3, 2);
            name.Text = controller.CreationNameLabel.Text; name.Opacity = controller.CreationRatingInput.Enabled ? 1 : 0.5;
            attribute.Text = controller.CreationAttributeLabel.Text; pool.Text = controller.CreationPoolLabel.Text;
            pool.FontWeight = controller.CreationPoolLabel.Font.Bold ? FontWeight.Bold : FontWeight.Normal;
            // RefreshControl sets bold pool text and row emphasis together.
            // Read that presentation flag: BackColor has already been resolved
            // through the compatibility theme and loses system-color identity.
            Layout.Classes.Set("trained", controller.CreationPoolLabel.Font.Bold);
            ToolTip.SetTip(name, controller.CreationToolTips.GetToolTip(controller.CreationNameLabel));
            ToolTip.SetTip(pool, controller.CreationToolTips.GetToolTip(controller.CreationPoolLabel));
            Avalonia.Automation.AutomationProperties.SetName(Rating, name.Text);
            Avalonia.Automation.AutomationProperties.SetName(Specialization, name.Text + " " + Specialization.PlaceholderText);
            rating.Refresh(); specialization.Refresh(); SpecializationDisclosure.Refresh();
        }
        finally { refreshing = false; }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; SpecializationDisclosure.Dispose(); rating.Dispose(); specialization.Dispose();
        foreach (var action in cleanup) action(); cleanup.Clear(); controller.Dispose();
    }
}

internal sealed class CreationActiveSkillSection : IDisposable
{
    private readonly Forms.Panel source;
    private readonly StackPanel rows = new() { Spacing = 4 };
    private readonly CreationRegionPresenter? presenter;
    private bool disposed;
    internal Dictionary<SkillControl, CreationActiveSkillRow> Rows { get; } = new();
    internal ScrollViewer Viewport { get; }
    internal CreationActiveSkillSection(Forms.Form? editor, Forms.Panel source, IReadOnlyList<SkillControl> controllers)
    {
        this.source = source;
        Viewport = new ScrollViewer { Content = rows,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        NativeControlPresentation.AddTheme(Viewport.Styles);
        foreach (var controller in controllers) Add(controller);
        SkillDisplayPreferences.Changed += RefreshDisclosure;
        source.AutoScroll = false;
        if (editor != null) presenter = new CreationRegionPresenter(editor, source, Viewport, "nativeCreationActiveSkills");
    }
    private void RefreshDisclosure() { foreach (var row in Rows.Values) row.Refresh(); }
    internal void Add(SkillControl controller, bool reveal = false)
    {
        if (disposed) throw new ObjectDisposedException(nameof(CreationActiveSkillSection));
        source.Controls.Remove(controller);
        var row = new CreationActiveSkillRow(controller, Rows.Count);
        Rows.Add(controller, row); rows.Children.Add(row.Layout);
        if (reveal) Dispatcher.UIThread.Post(() => { if (!disposed) ControlExtensions.BringIntoView(row.Layout); });
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; SkillDisplayPreferences.Changed -= RefreshDisclosure; presenter?.Dispose();
        foreach (var row in Rows.Values) row.Dispose();
    }
}
