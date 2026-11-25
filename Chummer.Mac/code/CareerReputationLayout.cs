#nullable enable
using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private StackPanel CreateReputationLayout()
    {
        var rows = new StackPanel { Name = "NativeCareerReputation", Spacing = 5 };
        foreach (var (caption, controller, total) in new[]
        {
            (lblStreetCred, nudStreetCred, lblStreetCredTotal),
            (lblNotoriety, nudNotoriety, lblNotorietyTotal),
            (lblPublicAware, nudPublicAware, lblPublicAwareTotal)
        })
        {
            tabCharacterInfo.Controls.Remove(caption);
            tabCharacterInfo.Controls.Remove(controller);
            tabCharacterInfo.Controls.Remove(total);
            var value = new NativeInlineNumericBinding(controller, tipTooltip, wholeNumbers: true);
            value.Input.ClearValue(Layoutable.WidthProperty);
            value.Input.ClearValue(NumericUpDown.FontSizeProperty);
            AutomationProperties.SetName(value.Input, caption.Text);
            var label = Caption(caption.Text);
            ToolTip.SetTip(label, tipTooltip.GetToolTip(caption));
            AutomationProperties.SetLabeledBy(value.Input, label);
            void RefreshCaption(object? sender, EventArgs args) => label.Text = caption.Text;
            caption.TextChanged += RefreshCaption;
            var summary = Caption(total.Text);
            summary.Name = "Native" + total.Name;
            void Refresh(object? sender, EventArgs args)
            {
                summary.Text = total.Text;
                Dispatcher.UIThread.Post(() =>
                {
                    if (!IsDisposed)
                        ToolTip.SetTip(summary, tipTooltip.GetToolTip(total));
                }, DispatcherPriority.Loaded);
            }
            total.TextChanged += Refresh;
            Refresh(null, EventArgs.Empty);
            var text = new StackPanel { VerticalAlignment = VerticalAlignment.Center, Children = { label, summary } };
            var row = new Grid { RowSpacing = 3, ColumnSpacing = 6, Children = { text, value.Input } };
            bool? stacked = null;
            void Reflow()
            {
                bool narrow = row.Bounds.Width < Math.Max(250, label.FontSize * 20);
                if (stacked == narrow)
                    return;
                stacked = narrow;
                row.ColumnDefinitions = new ColumnDefinitions(narrow ? "*" : "3*,2*");
                row.RowDefinitions = new RowDefinitions(narrow ? "Auto,Auto" : "Auto");
                Grid.SetColumn(value.Input, narrow ? 0 : 1);
                Grid.SetRow(value.Input, narrow ? 1 : 0);
                value.Input.VerticalAlignment = VerticalAlignment.Center;
            }
            row.SizeChanged += (_, args) =>
            {
                if (args.NewSize.Width != args.PreviousSize.Width)
                    Reflow();
            };
            label.PropertyChanged += (_, args) =>
            {
                if (args.Property == TextBlock.FontSizeProperty)
                    Reflow();
            };
            Reflow();
            rows.Children.Add(row);
            Disposed += (_, _) =>
            {
                caption.TextChanged -= RefreshCaption;
                total.TextChanged -= Refresh;
                value.Dispose();
                controller.Dispose();
                caption.Dispose();
                total.Dispose();
            };
        }

        tabCharacterInfo.Controls.Remove(cmdBurnStreetCred);
        var burn = new Button { Name = "NativecmdBurnStreetCred", Content = Caption(cmdBurnStreetCred.Text) };
        AutomationProperties.SetName(burn, cmdBurnStreetCred.Text);
        ToolTip.SetTip(burn, tipTooltip.GetToolTip(cmdBurnStreetCred));
        burn.Click += (_, _) => cmdBurnStreetCred.PerformClick();
        void RefreshBurn() => burn.IsEnabled = cmdBurnStreetCred.Enabled;
        var burnState = new NativeControlObservation(cmdBurnStreetCred, RefreshBurn);
        RefreshBurn();
        rows.Children.Add(burn);
        Disposed += (_, _) =>
        {
            burnState.Dispose();
            cmdBurnStreetCred.Dispose();
        };
        return rows;

        static TextBlock Caption(string text) => new() { Text = text, TextWrapping = TextWrapping.Wrap };
    }
}
