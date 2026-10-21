#nullable enable
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal sealed class CreationSkillGroupSection : IDisposable
{
    private readonly List<Action> cleanup = new();
    private readonly IReadOnlyList<SkillGroupControl> controllers;
    private bool disposed;
    internal Dictionary<SkillGroupControl, NumericUpDown> Inputs { get; } = new();
    internal ScrollViewer Viewport { get; }
    internal Dictionary<SkillGroupControl, Button> GroupButtons { get; } = new();

    internal CreationSkillGroupSection(Forms.Form? editor, Forms.Panel source, IReadOnlyList<SkillGroupControl> controllers, Action<string>? selectGroup = null)
    {
        this.controllers = controllers;
        var rows = new StackPanel { Spacing = 2 };
        Viewport = new ScrollViewer { Content = rows,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        Viewport.Styles.Add(new Avalonia.Themes.Fluent.FluentTheme());
        int index = 0;
        foreach (var controller in controllers)
        {
            source.Controls.Remove(controller);
            // Preserve the existing UserControl.Load initialization, including
            // broken-group availability, before retiring its drawn presentation.
            controller.CreateControl();
            var binding = new NativeInlineNumericBinding(controller.CreationRatingInput, controller.CreationToolTips, true);
            binding.Input.Name = "nativeCreationGroup" + index++;
            Inputs.Add(controller, binding.Input); cleanup.Add(binding.Dispose);
            var label = new TextBlock { TextWrapping = Avalonia.Media.TextWrapping.Wrap,
                VerticalAlignment = VerticalAlignment.Center, Margin = new Thickness(4, 2), FontSize = 12 };
            label.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlForegroundBaseHighBrush"));
            void Refresh()
            {
                if (disposed) return;
                label.Text = controller.CreationNameInput.Text;
                Avalonia.Automation.AutomationProperties.SetName(binding.Input, label.Text);
                label.Opacity = controller.CreationRatingInput.Enabled ? 1 : 0.5;
                ToolTip.SetTip(label, controller.CreationToolTips.GetToolTip(controller.CreationNameInput));
            }
            EventHandler changed = (_, _) => Refresh();
            controller.CreationNameInput.TextChanged += changed;
            controller.CreationRatingInput.EnabledChanged += changed;
            cleanup.Add(() => { controller.CreationNameInput.TextChanged -= changed;
                controller.CreationRatingInput.EnabledChanged -= changed; });
            var groupButton = new Button { Content = label, HorizontalAlignment = HorizontalAlignment.Stretch,
                HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(2), MinHeight = 28 };
            EventHandler<Avalonia.Interactivity.RoutedEventArgs> select = (_, _) => selectGroup?.Invoke(controller.GroupName);
            groupButton.Click += select;
            GroupButtons.Add(controller, groupButton);
            cleanup.Add(() => groupButton.Click -= select);
            var row = new Grid { ColumnDefinitions = new ColumnDefinitions("*,120"), MinHeight = 32 };
            row.Children.Add(groupButton); Grid.SetColumn(binding.Input, 1); row.Children.Add(binding.Input);
            rows.Children.Add(row); Refresh();
        }
        source.AutoScroll = false; // The native viewport owns all scrolling now.
        if (editor != null)
        {
            var presenter = new CreationRegionPresenter(editor, source, Viewport, "nativeCreationSkillGroups");
            cleanup.Add(presenter.Dispose);
        }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        foreach (var action in cleanup) action(); cleanup.Clear();
        foreach (var controller in controllers) controller.Dispose();
    }
}

public partial class frmCreate
{
    private readonly List<SkillGroupControl> creationSkillGroups = new();
    private CreationSkillGroupSection? creationSkillGroupSection;
    private bool creationSkillGroupEventsAttached;
    private void AddCreationSkillGroup(SkillGroupControl controller)
    {
        creationSkillGroups.Add(controller);
        panSkillGroups.Controls.Add(controller);
    }
    private void InitializeCreationSkillGroups()
    {
        if (Avalonia.Application.Current == null || creationSkillGroupEventsAttached) return;
        creationSkillGroupEventsAttached = true;
        void ShowGroups(object? sender, EventArgs args)
        {
            if (IsDisposed || creationSkillGroupSection != null || !tabSkills.Visible) return;
            creationSkillGroupSection = new CreationSkillGroupSection(this, panSkillGroups, creationSkillGroups, group => SelectCreationSkillFilter("GROUP:" + group));
        }
        tabSkills.VisibleChanged += ShowGroups;
        Shown += ShowGroups;
        Disposed += (_, _) => {
            tabSkills.VisibleChanged -= ShowGroups; Shown -= ShowGroups;
            creationSkillGroupSection?.Dispose();
        };
        ShowGroups(this, EventArgs.Empty);
    }
}
