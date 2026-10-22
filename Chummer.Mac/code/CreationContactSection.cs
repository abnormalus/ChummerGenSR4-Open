#nullable enable
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Avalonia.Threading;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed class CreationContactRow : IDisposable
{
    private readonly ContactControl controller;
    private readonly Func<bool> sectionAvailable;
    private readonly NativeInlineTextBinding name;
    private readonly NativeInlineNumericBinding connection, loyalty;
    private readonly NativeInlineCommandBinding group, notes, link, delete;
    private readonly List<Action> cleanup = new();
    private readonly Grid statistics = new() { ColumnSpacing = 4, RowSpacing = 3 };
    private readonly StackPanel connectionCell = new(), loyaltyCell = new();
    private readonly WrapPanel actions = new();
    private readonly TextBlock connectionLabel = Caption(), loyaltyLabel = Caption();
    private readonly ContextMenu linkMenu = new();
    private readonly MenuItem attachItem = new(), openItem = new(), removeItem = new();
    private bool disposed, syncing;
    private int layoutMode = -1;
    internal Grid Layout { get; } = new() { RowSpacing = 2, ColumnSpacing = 6, Margin = new Thickness(2) };
    internal Border Frame { get; }
    internal TextBox Name => name.Input;
    internal NumericUpDown Connection => connection.Input;
    internal NumericUpDown Loyalty => loyalty.Input;
    internal Button Group => group.Input;
    internal Button Notes => notes.Input;
    internal Button Link => link.Input;
    internal Button Delete => delete.Input;
    internal ContextMenu LinkMenu => linkMenu;
    private bool Available() => !disposed && !controller.IsDisposed && controller.Visible && controller.Enabled && sectionAvailable();
    private static TextBlock Caption()
    {
        var text = new TextBlock { FontSize = 12, VerticalAlignment = VerticalAlignment.Center, TextWrapping = TextWrapping.Wrap };
        text.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush")); return text;
    }
    internal CreationContactRow(ContactControl controller, Forms.Form? editor, Func<bool> sectionAvailable, int index)
    {
        this.controller = controller; this.sectionAvailable = sectionAvailable;
        controller.CreationViewportOwner = editor; controller.CreateControl();
        name = new NativeInlineTextBinding(controller.CreationNameInput, Available);
        connection = new NativeInlineNumericBinding(controller.CreationConnectionInput, controller.CreationToolTips, true, Available);
        loyalty = new NativeInlineNumericBinding(controller.CreationLoyaltyInput, controller.CreationToolTips, true, Available);
        Connection.Width = Loyalty.Width = 104;
        // Match the established compact attribute editor: the normal Common
        // column must show a complete row in its shorter enemy viewport.
        Name.MinHeight = Name.Height = 26; Name.Padding = new Thickness(2, 0);
        foreach (var input in new[] { Connection, Loyalty })
        { input.MinHeight = input.Height = 26; input.Padding = new Thickness(2, 0); }
        Layout.Styles.Add(new Style(selector => selector.OfType<NumericUpDown>().Template().OfType<TextBox>())
        {
            Setters = {
                new Setter(TextBox.MinHeightProperty, 0d),
                new Setter(TextBox.HeightProperty, 26d),
                new Setter(TextBox.PaddingProperty, new Thickness(2, 0)),
                new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center)
            }
        });
        string identity = controller.ContactObject.EntityType == ContactType.Enemy ? "nativeCreationEnemy" : "nativeCreationContact";
        Name.Name = identity + "Name" + index;
        Connection.Name = identity + "Connection" + index; Loyalty.Name = identity + "Loyalty" + index;
        group = new NativeInlineCommandBinding(controller.CreationGroupButton, controller.CreationToolTips, Available,
            controller.CreationGroupButton.PerformClick, () => controller.CreationGroupLabel.Text.TrimEnd(':') + " " + controller.CreationGroupValue.Text);
        notes = new NativeInlineCommandBinding(controller.CreationNotesImage, controller.CreationToolTips, Available,
            controller.EditCreationNotes, () => LanguageManager.Instance.GetString("Tab_Notes"));
        link = new NativeInlineCommandBinding(controller.CreationLinkImage, controller.CreationToolTips, Available,
            OpenLinkMenu, () => "↗");
        delete = new NativeInlineCommandBinding(controller.CreationDeleteButton, controller.CreationToolTips, Available,
            controller.CreationDeleteButton.PerformClick);
        foreach (var command in new[] { Group, Notes, Link, Delete }) actions.Children.Add(command);
        connectionCell.Children.Add(connectionLabel); connectionCell.Children.Add(Connection);
        loyaltyCell.Children.Add(loyaltyLabel); loyaltyCell.Children.Add(Loyalty);
        statistics.Children.Add(connectionCell); statistics.Children.Add(loyaltyCell);
        Layout.Children.Add(Name); Layout.Children.Add(statistics); Layout.Children.Add(actions);
        Frame = new Border { Child = Layout, Margin = new Thickness(0, 0, 0, 5) };
        linkMenu.ItemsSource = new[] { attachItem, openItem, removeItem }; Link.ContextMenu = linkMenu;
        BindLink(attachItem, () => !controller.HasLinkedCharacter, controller.AttachCreationCharacter);
        BindLink(openItem, () => controller.HasLinkedCharacter, controller.OpenCreationCharacter);
        BindLink(removeItem, () => controller.HasLinkedCharacter, controller.RemoveCreationCharacter);
        EventHandler changed = (_, _) => Refresh(); Forms.InvalidateEventHandler invalidated = (_, _) => Refresh();
        foreach (var source in new Forms.Control[] { controller, controller.CreationConnectionLabel,
            controller.CreationLoyaltyLabel, controller.CreationGroupLabel, controller.CreationGroupValue })
        {
            source.TextChanged += changed; source.EnabledChanged += changed; source.VisibleChanged += changed;
            source.BackColorChanged += changed; source.Invalidated += invalidated;
            cleanup.Add(() => { source.TextChanged -= changed; source.EnabledChanged -= changed;
                source.VisibleChanged -= changed; source.BackColorChanged -= changed; source.Invalidated -= invalidated; });
        }
        EventHandler<SizeChangedEventArgs> resized = (_, _) => Arrange(); Layout.SizeChanged += resized;
        cleanup.Add(() => Layout.SizeChanged -= resized);
        Avalonia.Automation.AutomationProperties.SetName(Name, "Contact or enemy name");
        Avalonia.Automation.AutomationProperties.SetName(Link, "Linked character actions");
        Arrange(); Refresh();
    }
    private void BindLink(MenuItem target, Func<bool> applicable, Action execute)
    {
        EventHandler<RoutedEventArgs> clicked = (_, _) =>
        { if (Available() && applicable()) { execute(); Refresh(); } };
        target.Click += clicked; cleanup.Add(() => target.Click -= clicked);
    }
    private void OpenLinkMenu()
    {
        if (!Available()) return;
        RefreshLinkMenu(); linkMenu.Open(Link);
    }
    private void RefreshLinkMenu()
    {
        attachItem.Header = LanguageManager.Instance.GetString("MenuItem_AttachCharacter");
        openItem.Header = LanguageManager.Instance.GetString("MenuItem_OpenCharacter");
        removeItem.Header = LanguageManager.Instance.GetString("MenuItem_RemoveCharacter");
        attachItem.IsVisible = !controller.HasLinkedCharacter;
        openItem.IsVisible = removeItem.IsVisible = controller.HasLinkedCharacter;
        attachItem.IsEnabled = openItem.IsEnabled = removeItem.IsEnabled = Available();
    }
    private void Arrange()
    {
        int mode = Layout.Bounds.Width >= 620 ? 2 : Layout.Bounds.Width >= 226 ? 1 : 0;
        if (mode == layoutMode) return; layoutMode = mode;
        Layout.ColumnDefinitions = new ColumnDefinitions(mode == 2 ? "*,Auto,Auto" : "*");
        Layout.RowDefinitions = new RowDefinitions(mode == 2 ? "Auto" : "Auto,Auto,Auto");
        Grid.SetColumn(statistics, mode == 2 ? 1 : 0); Grid.SetRow(statistics, mode == 2 ? 0 : 1);
        Grid.SetColumn(actions, mode == 2 ? 2 : 0); Grid.SetRow(actions, mode == 2 ? 0 : 2);
        statistics.ColumnDefinitions = new ColumnDefinitions(mode == 0 ? "*" : "Auto,Auto");
        statistics.RowDefinitions = new RowDefinitions(mode == 0 ? "Auto,Auto" : "Auto");
        Grid.SetColumn(loyaltyCell, mode == 0 ? 0 : 1); Grid.SetRow(loyaltyCell, mode == 0 ? 1 : 0);
        Name.VerticalAlignment = statistics.VerticalAlignment = actions.VerticalAlignment = VerticalAlignment.Center;
    }
    internal void Refresh()
    {
        if (disposed || syncing) return; syncing = true;
        try
        {
            Layout.IsVisible = controller.Visible;
            connectionLabel.Text = controller.CreationConnectionLabel.Text; loyaltyLabel.Text = controller.CreationLoyaltyLabel.Text;
            Avalonia.Automation.AutomationProperties.SetName(Connection, connectionLabel.Text);
            Avalonia.Automation.AutomationProperties.SetName(Loyalty, loyaltyLabel.Text);
            name.Refresh(); connection.Refresh(); loyalty.Refresh();
            Connection.IsEnabled = controller.CreationConnectionInput.Enabled && Available();
            Loyalty.IsEnabled = controller.CreationLoyaltyInput.Enabled && Available();
            group.Refresh(); notes.Refresh(); link.Refresh(); delete.Refresh(); RefreshLinkMenu();
            // Stored custom group color remains a visible accent without reducing
            // contrast inside the theme-owned native text/numeric editors.
            var color = controller.ContactObject.Colour;
            bool custom = color.A > 0 && !color.IsEmpty && color.Name != "White" && color.Name != "Black";
            Frame.BorderThickness = custom ? new Thickness(3, 0, 0, 0) : new Thickness(0);
            Frame.BorderBrush = custom ? new SolidColorBrush(Color.FromArgb(color.A, color.R, color.G, color.B)) : null;
        }
        finally { syncing = false; }
    }
    public void Dispose()
    {
        if (disposed) return; disposed = true;
        name.Dispose(); connection.Dispose(); loyalty.Dispose();
        group.Dispose(); notes.Dispose(); link.Dispose(); delete.Dispose();
        foreach (var action in cleanup) action(); cleanup.Clear();
        linkMenu.Close(); Link.ContextMenu = null; controller.CreationViewportOwner = null;
        // The creation owner, not the view, owns controller disposal.
    }
}

internal sealed class CreationContactSection : IDisposable
{
    private readonly Forms.Form? editor;
    private readonly Forms.Panel source;
    private readonly StackPanel rows = new();
    private readonly CreationRegionPresenter? presenter;
    private readonly List<Action> cleanup = new();
    private bool disposed;
    private int nextIndex;
    internal ScrollViewer Viewport { get; }
    internal Dictionary<ContactControl, CreationContactRow> Rows { get; } = new();
    private bool Available() => !disposed && !source.IsDisposed && source.Visible && source.Enabled
        && (editor == null || (!editor.IsDisposed && editor.Visible && editor.Enabled
            && (editor.MdiParent == null || editor.MdiParent.ActiveMdiChild == editor)));
    internal CreationContactSection(Forms.Form? editor, Forms.Panel source, IReadOnlyList<ContactControl> controllers, string name, bool attachPresenter = true)
    {
        this.editor = editor; this.source = source;
        Viewport = new ScrollViewer { Content = rows, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        NativeControlPresentation.AddTheme(Viewport.Styles); source.AutoScroll = false;
        foreach (var controller in controllers) Add(controller);
        EventHandler changed = (_, _) => Refresh(); source.VisibleChanged += changed; source.EnabledChanged += changed;
        cleanup.Add(() => { source.VisibleChanged -= changed; source.EnabledChanged -= changed; });
        if (editor != null)
        {
            // Rows are created during Load, before the editor becomes the active
            // MDI child. Refresh the native availability after that transition,
            // and on later character switches; execution still checks Available.
            editor.EnabledChanged += changed; editor.VisibleChanged += changed; editor.Shown += changed;
            cleanup.Add(() => { editor.EnabledChanged -= changed; editor.VisibleChanged -= changed; editor.Shown -= changed; });
            if (editor.MdiParent is { } container)
            {
                container.MdiChildActivate += changed;
                cleanup.Add(() => container.MdiChildActivate -= changed);
            }
            if (attachPresenter) presenter = new CreationRegionPresenter(editor, source, Viewport, name);
        }
    }
    internal void Add(ContactControl controller, bool reveal = false)
    {
        if (disposed) throw new ObjectDisposedException(nameof(CreationContactSection));
        source.Controls.Remove(controller);
        var row = new CreationContactRow(controller, editor, Available, nextIndex++);
        Rows.Add(controller, row); rows.Children.Add(row.Frame);
        if (reveal) Dispatcher.UIThread.Post(() => { if (!disposed && Rows.ContainsKey(controller)) row.Frame.BringIntoView(); });
    }
    internal void Remove(ContactControl controller)
    {
        if (!Rows.Remove(controller, out var row)) return;
        rows.Children.Remove(row.Frame); row.Dispose();
    }
    internal void Refresh() { if (!disposed) foreach (var row in Rows.Values) row.Refresh(); }
    public void Dispose()
    {
        if (disposed) return; disposed = true; presenter?.Dispose();
        foreach (var action in cleanup) action(); cleanup.Clear();
        foreach (var row in Rows.Values) row.Dispose(); Rows.Clear(); rows.Children.Clear();
    }
}
