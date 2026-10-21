#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Templates;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using Avalonia.Styling;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

// One native viewport; the owned SkillControl remains the only rules/event boundary.
internal sealed class CreationKnowledgeSkillRow : IDisposable
{
    private readonly SkillControl controller;
    private readonly NativeInlineNumericBinding rating;
    private readonly NativeInlineChoiceBinding name, specialization;
    private readonly List<Action> cleanup = new();
    private readonly TextBlock pool = Caption(), attribute = Caption();
    private bool syncing, disposed;
    internal Grid Layout { get; }
    internal ComboBox Type { get; }
    internal AutoCompleteBox Name => name.Input;
    internal AutoCompleteBox Specialization => specialization.Input;
    internal NumericUpDown Rating => rating.Input;
    internal Button Delete { get; }
    internal Button AddSpecialty { get; }
    internal SkillSpecializationDisclosure SpecializationDisclosure { get; }
    private static TextBlock Caption()
    {
        var text = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center };
        text.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
        return text;
    }
    internal CreationKnowledgeSkillRow(SkillControl controller, int index, Forms.Form? editor)
    {
        this.controller = controller;
        controller.CreationViewportOwner = editor;
        if (controller.Visible) controller.CreateControl();
        var sourceType = controller.CreationKnowledgeTypeInput;
        Type = new ComboBox { MinHeight = 32, HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = new FuncDataTemplate<object>((item, _) => new TextBlock { Text = sourceType.GetItemText(item) }) };
        name = new NativeInlineChoiceBinding(controller.CreationKnowledgeNameInput);
        specialization = new NativeInlineChoiceBinding(controller.CreationSpecializationInput);
        rating = new NativeInlineNumericBinding(controller.CreationRatingInput, controller.CreationToolTips, true,
            () => controller.Visible && controller.CreationRatingInput.Visible);
        Rating.Width = 104;
        Type.Name = "nativeCreationKnowledgeType" + index; Name.Name = "nativeCreationKnowledgeName" + index;
        Rating.Name = "nativeCreationKnowledgeRating" + index; Specialization.Name = "nativeCreationKnowledgeSpecialization" + index;
        Specialization.PlaceholderText = LanguageManager.Instance.GetString("String_ExpenseSpecialization");
        Layout = new Grid { ColumnDefinitions = new ColumnDefinitions("140,*,104,40,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnSpacing = 4, RowSpacing = 3, Margin = new Thickness(3) };
        Layout.Styles.Add(new Style(selector => selector.OfType<Grid>().Class("trained"))
        { Setters = { new Setter(Panel.BackgroundProperty, new DynamicResourceExtension("SystemControlBackgroundListLowBrush")) } });
        Layout.Children.Add(Type); Grid.SetColumn(Name, 1); Layout.Children.Add(Name);
        Grid.SetColumn(Rating, 2); Layout.Children.Add(Rating); Grid.SetColumn(pool, 3); Layout.Children.Add(pool);
        Delete = Command(controller.CreationDeleteButton);
        var actions = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 2 };
        actions.Children.Add(Delete); Grid.SetColumn(actions, 4); Layout.Children.Add(actions);
        var specializationLine = new Grid { ColumnDefinitions = new ColumnDefinitions("*,40,Auto"), ColumnSpacing = 4 };
        Grid.SetRow(specializationLine, 1); Grid.SetColumnSpan(specializationLine, 5); Layout.Children.Add(specializationLine);
        specializationLine.Children.Add(Specialization);
        Grid.SetColumn(attribute, 1); specializationLine.Children.Add(attribute);
        AddSpecialty = Command(controller.CreationKnowledgeSpecialtyButton);
        Grid.SetColumn(AddSpecialty, 2); specializationLine.Children.Add(AddSpecialty);
        SpecializationDisclosure = new SkillSpecializationDisclosure(specializationLine, "Show or hide knowledge skill specialization",
            () => controller.Visible && controller.Enabled && controller.CreationSpecializationInput.Visible);
        actions.Children.Add(SpecializationDisclosure.Toggle);
        Avalonia.Automation.AutomationProperties.SetName(Type, "Knowledge skill type");
        Avalonia.Automation.AutomationProperties.SetName(Name, "Knowledge skill name");
        Avalonia.Automation.AutomationProperties.SetName(Rating, "Knowledge skill rating");
        Avalonia.Automation.AutomationProperties.SetName(Specialization, "Knowledge skill specialization");
        EventHandler<SelectionChangedEventArgs> selected = (_, _) =>
        {
            if (disposed || syncing) return;
            if (!controller.Enabled || !controller.Visible || !sourceType.Enabled || !sourceType.Visible) { Refresh(); return; }
            if (Type.SelectedItem != null && sourceType.Items.Contains(Type.SelectedItem))
                sourceType.SelectedItem = Type.SelectedItem;
            Refresh();
        };
        Type.SelectionChanged += selected; cleanup.Add(() => Type.SelectionChanged -= selected);
        EventHandler changed = (_, _) => Refresh(); Forms.InvalidateEventHandler invalidated = (_, _) => Refresh();
        var sources = new Forms.Control[] { controller, sourceType, controller.CreationKnowledgeNameInput,
            controller.CreationRatingInput, controller.CreationSpecializationInput, controller.CreationPoolLabel, controller.CreationAttributeLabel };
        foreach (var source in sources)
        {
            source.TextChanged += changed; source.VisibleChanged += changed; source.EnabledChanged += changed; source.Invalidated += invalidated; source.FontChanged += changed; source.BackColorChanged += changed;
            cleanup.Add(() => { source.TextChanged -= changed; source.VisibleChanged -= changed;
                source.EnabledChanged -= changed; source.Invalidated -= invalidated; source.FontChanged -= changed; source.BackColorChanged -= changed; });
        }
        sourceType.SelectedIndexChanged += changed; cleanup.Add(() => sourceType.SelectedIndexChanged -= changed);
        Refresh();
    }
    private Button Command(Forms.Button source)
    {
        var target = new Button { MinHeight = 32, Padding = new Thickness(6, 2) };
        void Sync()
        {
            if (disposed) return;
            target.Content = source.Text; target.IsVisible = source.Visible; target.IsEnabled = controller.Enabled && source.Enabled;
            ToolTip.SetTip(target, controller.CreationToolTips.GetToolTip(source));
        }
        EventHandler changed = (_, _) => Sync();
        EventHandler<RoutedEventArgs> click = (_, _) =>
        { if (!disposed && controller.Visible && controller.Enabled && source.Visible && source.Enabled) source.PerformClick(); };
        source.TextChanged += changed; source.VisibleChanged += changed; source.EnabledChanged += changed; target.Click += click;
        cleanup.Add(() => { source.TextChanged -= changed; source.VisibleChanged -= changed;
            source.EnabledChanged -= changed; target.Click -= click; });
        Sync(); return target;
    }
    private void Refresh()
    {
        if (disposed || syncing) return;
        syncing = true;
        try
        {
            var source = controller.CreationKnowledgeTypeInput;
            var choices = source.Items.Cast<object>().ToArray();
            if (Type.ItemsSource is not object[] old || !old.SequenceEqual(choices)) Type.ItemsSource = choices;
            Type.SelectedItem = source.SelectedItem; Type.IsVisible = source.Visible; Type.IsEnabled = controller.Enabled && source.Enabled;
            Layout.IsVisible = controller.Visible;
            pool.FontWeight = controller.CreationPoolLabel.Font.Bold ? FontWeight.Bold : FontWeight.Normal;
            Layout.Classes.Set("trained", controller.CreationPoolLabel.Font.Bold);
            pool.Text = controller.CreationPoolLabel.Text; attribute.Text = controller.CreationAttributeLabel.Text;
            ToolTip.SetTip(pool, controller.CreationToolTips.GetToolTip(controller.CreationPoolLabel));
            rating.Refresh(); name.Refresh(); specialization.Refresh(); SpecializationDisclosure.Refresh();
        }
        finally { syncing = false; }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; SpecializationDisclosure.Dispose(); rating.Dispose(); name.Dispose(); specialization.Dispose();
        foreach (var action in cleanup) action(); cleanup.Clear(); controller.CreationViewportOwner = null; controller.Dispose();
    }
}

internal sealed class CreationKnowledgeSkillSection : IDisposable
{
    private readonly Forms.Panel source;
    private readonly Forms.Form? editor;
    private readonly StackPanel rows = new() { Spacing = 4 };
    private readonly CreationRegionPresenter? presenter;
    private bool disposed;
    private int nextIndex;
    internal Dictionary<SkillControl, CreationKnowledgeSkillRow> Rows { get; } = new();
    internal ScrollViewer Viewport { get; }
    internal CreationKnowledgeSkillSection(Forms.Form? editor, Forms.Panel source, IReadOnlyList<SkillControl> controllers)
    {
        this.source = source; this.editor = editor;
        Viewport = new ScrollViewer { Content = rows, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        NativeControlPresentation.AddTheme(Viewport.Styles);
        foreach (var controller in controllers) Add(controller);
        source.AutoScroll = false;
        if (editor != null) presenter = new CreationRegionPresenter(editor, source, Viewport, "nativeCreationKnowledgeSkills");
    }
    internal void Add(SkillControl controller, bool reveal = false)
    {
        if (disposed) throw new ObjectDisposedException(nameof(CreationKnowledgeSkillSection));
        source.Controls.Remove(controller);
        var row = new CreationKnowledgeSkillRow(controller, nextIndex++, editor);
        Rows.Add(controller, row); rows.Children.Add(row.Layout);
        if (reveal) Dispatcher.UIThread.Post(() => { if (!disposed && Rows.ContainsKey(controller)) ControlExtensions.BringIntoView(row.Layout); });
    }
    internal void Remove(SkillControl controller)
    {
        if (!Rows.Remove(controller, out var row)) return;
        rows.Children.Remove(row.Layout); row.Dispose();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; presenter?.Dispose();
        foreach (var row in Rows.Values) row.Dispose(); Rows.Clear(); rows.Children.Clear();
    }
}
