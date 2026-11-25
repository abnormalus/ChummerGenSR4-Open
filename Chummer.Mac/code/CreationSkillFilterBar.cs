#nullable enable
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Media;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private readonly Dictionary<string, ToggleButton> creationSkillFilterButtons = new();
    private void SelectCreationSkillFilter(string value)
    {
        if (IsDisposed) return;
        // Reuse the original filter handler and its canonical values. Filtering
        // changes presentation only, including controller visibility/edit guards.
        if (cboSkillFilter.SelectedValue?.ToString() == value)
            cboSkillFilter_SelectedIndexChanged(cboSkillFilter, EventArgs.Empty);
        else
            cboSkillFilter.SelectedValue = value;
    }
    private void InitializeCreationSkillFilterBar()
    {
        int height = 32;
        bool pending = false;
        splitSkills.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
        var bar = new WrapPanel { ItemSpacing = 4, LineSpacing = 4 };
        NativeControlPresentation.AddTheme(bar.Styles);
        var values = new[] { "0", "BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG", "MAG", "RES", "1" };
        for (int index = 0; index < values.Length; index++)
        {
            string value = values[index];
            string label = value == "0" ? LanguageManager.Instance.GetString("String_SkillFilterAllShort") :
                value == "1" ? LanguageManager.Instance.GetString("String_SkillFilterRankedShort") : LanguageManager.Instance.GetString("String_Attribute" + value + "Short");
            var button = new ToggleButton
            {
                Content = new TextBlock { Text = label, TextWrapping = TextWrapping.Wrap }
            };
            Avalonia.Automation.AutomationProperties.SetName(button, label);
            button.Click += (_, _) => { SelectCreationSkillFilter(value); RefreshButtons(); };
            creationSkillFilterButtons.Add(value, button);
            bar.Children.Add(button);
        }
        void RefreshButtons()
        {
            string? selected = cboSkillFilter.SelectedValue?.ToString();
            foreach (var pair in creationSkillFilterButtons) pair.Value.IsChecked = pair.Key == selected;
        }
        EventHandler changed = (_, _) => RefreshButtons();
        cboSkillFilter.SelectedIndexChanged += changed;
        Forms.Form workspace = MdiParent ?? this;
        var presenter = new CreationRegionPresenter(this, tabSkills, new Border { Child = bar }, "nativeCreationSkillFilters",
            () => new System.Drawing.Rectangle(0, 0, AvailableWidth(), height), clipToSource: true);
        bar.PropertyChanged += TypographyChanged;
        tabSkills.SizeChanged += QueueArrange;
        tabSkills.VisibleChanged += QueueArrange;
        workspace.SizeChanged += QueueArrange;
        Disposed += (_, _) =>
        {
            cboSkillFilter.SelectedIndexChanged -= changed;
            bar.PropertyChanged -= TypographyChanged;
            tabSkills.SizeChanged -= QueueArrange;
            tabSkills.VisibleChanged -= QueueArrange;
            workspace.SizeChanged -= QueueArrange;
            presenter.Dispose();
        };
        RefreshButtons();
        QueueArrange(null, EventArgs.Empty);

        void TypographyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty ||
                args.Property == Avalonia.Controls.Documents.TextElement.FontFamilyProperty)
                QueueArrange(sender, EventArgs.Empty);
        }

        void QueueArrange(object? sender, EventArgs args)
        {
            if (pending)
                return;
            pending = true;
            Dispatcher.UIThread.Post(ArrangeFilters, DispatcherPriority.Loaded);
        }

        void ArrangeFilters()
        {
            pending = false;
            if (IsDisposed || !tabSkills.Visible)
                return;
            int width = AvailableWidth();
            if (width <= 0)
                return;

            foreach (var button in creationSkillFilterButtons.Values)
                button.MaxWidth = width;
            bar.Measure(new Size(width, double.PositiveInfinity));
            height = Math.Max(32, (int)Math.Ceiling(bar.DesiredSize.Height) + 4);

            // Derive the split area from the current tab size on every resize.
            // The split container still owns the divider and pane minimums.
            var client = tabSkills.ClientSize;
            var bounds = new System.Drawing.Rectangle(0, height, client.Width, Math.Max(0, client.Height - height));
            if (splitSkills.Bounds != bounds)
                splitSkills.Bounds = bounds;
            presenter.Refresh();
        }

        int AvailableWidth()
        {
            var origin = workspace.PointToClient(tabSkills.PointToScreen(System.Drawing.Point.Empty));
            return Math.Clamp(workspace.ClientSize.Width - origin.X, 0, tabSkills.ClientSize.Width);
        }
    }
}
