#nullable enable
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CharacterSummaryLayout
{
    internal static void Attach(Forms.Form owner, Forms.TabPage page, string hostName, Forms.ToolTip tips,
        params (Forms.Label Caption, Forms.Control Value)[] fields)
    {
        if (Application.Current == null)
            return;

        var workspace = owner.MdiParent ?? owner;
        var rows = new StackPanel { Spacing = 6, Margin = new Thickness(8) };
        var scroll = new ScrollViewer
        {
            Content = rows,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled
        };
        foreach (var (caption, controller) in fields)
        {
            page.Controls.Remove(caption);
            page.Controls.Remove(controller);
            var label = Text(caption.Text);
            void RefreshCaption(object? sender, EventArgs args)
            {
                label.Text = caption.Text;
                ToolTip.SetTip(label, tips.GetToolTip(caption) is { Length: > 0 } tip ? tip : caption.Text);
            }
            caption.TextChanged += RefreshCaption;
            RefreshCaption(null, EventArgs.Empty);
            var row = new Grid { ColumnSpacing = 6, ColumnDefinitions = new ColumnDefinitions("3*,2*") };
            Control value;
            if (controller is Forms.ComboBox choice)
            {
                // The active commlink name needs the full sidebar width.
                row.ColumnDefinitions.Clear();
                row.RowDefinitions = new RowDefinitions("Auto,Auto");
                row.RowSpacing = 3;
                value = NativeListSelection.Attach(owner, choice, caption.Text, layoutManaged: true)!;
                Grid.SetRow(value, 1);
            }
            else
            {
                var text = Text(controller.Text);
                text.Name = "Native" + controller.Name;
                void RefreshText(object? sender, EventArgs args)
                {
                    text.Text = controller.Text;
                    RefreshTip();
                }
                void RefreshTip() => ToolTip.SetTip(text,
                    tips.GetToolTip(controller) is { Length: > 0 } detail ? detail : controller.Text);
                text.PointerEntered += (_, _) => RefreshTip();
                controller.TextChanged += RefreshText;
                RefreshText(null, EventArgs.Empty);
                owner.Disposed += (_, _) => controller.TextChanged -= RefreshText;
                value = text;
                Grid.SetColumn(value, 1);
                scroll.SizeChanged += (_, args) =>
                {
                    bool stacked = args.NewSize.Width < 210;
                    if (stacked == (Grid.GetRow(value) == 1))
                        return;

                    row.ColumnDefinitions = new ColumnDefinitions(stacked ? "*" : "3*,2*");
                    row.RowDefinitions = new RowDefinitions(stacked ? "Auto,Auto" : "Auto");
                    row.RowSpacing = 2;
                    Grid.SetColumn(value, stacked ? 0 : 1);
                    Grid.SetRow(value, stacked ? 1 : 0);
                };
            }
            row.Children.Add(label);
            row.Children.Add(value);
            rows.Children.Add(row);
            void Refresh() => row.IsVisible = caption.Visible && controller.Visible;
            var captionState = new NativeControlObservation(caption, Refresh);
            var valueState = new NativeControlObservation(controller, Refresh);
            Refresh();
            owner.Disposed += (_, _) =>
            {
                caption.TextChanged -= RefreshCaption;
                captionState.Dispose();
                valueState.Dispose();
                controller.Dispose();
                caption.Dispose();
            };
        }

        NativeControlPresentation.AddTheme(scroll.Styles);
        var presenter = new CreationRegionPresenter(owner, page, scroll, hostName,
            () => new System.Drawing.Rectangle(System.Drawing.Point.Empty, InventoryTreeLayout.VisiblePageSize(workspace, page)),
            clipToSource: true);
        owner.Disposed += (_, _) => presenter.Dispose();

        static TextBlock Text(string value) => new() { Text = value, TextWrapping = TextWrapping.Wrap };
    }
}
