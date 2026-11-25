#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Threading;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCharacterStatus
{
    internal static void Attach(Forms.Form editor, Forms.StatusStrip source)
    {
        if (Application.Current == null || source.Parent == null)
            return;

        var fields = new WrapPanel { ItemSpacing = 5, LineSpacing = 2 };
        var bar = new Border { Child = fields, BorderThickness = new Thickness(0, 1, 0, 0), Padding = new Thickness(8, 2), ClipToBounds = true };
        NativeControlPresentation.AddTheme(bar.Styles);
        bar.Bind(Border.BackgroundProperty, new DynamicResourceExtension("ControlFillColorSecondaryBrush"));
        bar.Bind(Border.BorderBrushProperty, new DynamicResourceExtension("ControlStrokeColorDefaultBrush"));

        bool mdi = editor.MdiParent != null;
        Forms.Control slot = mdi ? new Forms.Panel() : new Forms.NativeControlHost { NativeControl = bar };
        slot.Name = "NativeCharacterStatus";
        slot.Bounds = source.Bounds;
        slot.Dock = Forms.DockStyle.Bottom;
        slot.TabStop = false;
        int index = editor.Controls.GetChildIndex(source);
        editor.Controls.Remove(source);
        editor.Controls.Add(slot);
        editor.Controls.SetChildIndex(slot, index);

        bool queued = false;
        void Measure(object? sender, EventArgs args)
        {
            if (queued)
                return;

            queued = true;
            Dispatcher.UIThread.Post(() =>
            {
                queued = false;
                if (editor.IsDisposed || slot.Width <= 0)
                    return;

                fields.Measure(new Size(Math.Max(0, slot.Width - bar.Padding.Left - bar.Padding.Right), double.PositiveInfinity));
                int height = (int)Math.Ceiling(fields.DesiredSize.Height + bar.Padding.Top + bar.Padding.Bottom + bar.BorderThickness.Top);
                slot.Height = Math.Max(source.Height, height);
            }, DispatcherPriority.Loaded);
        }

        void TypographyChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty ||
                args.Property == Avalonia.Controls.Documents.TextElement.FontFamilyProperty)
                Measure(sender, EventArgs.Empty);
        }

        slot.SizeChanged += Measure;
        bar.PropertyChanged += TypographyChanged;
        var bindings = source.Items.OfType<Forms.ToolStripStatusLabel>().Select(item =>
        {
            var label = new TextBlock
            {
                Name = "Native" + item.Name,
                VerticalAlignment = VerticalAlignment.Center,
                TextWrapping = TextWrapping.Wrap,
                Margin = new Thickness(0, 0, item.BorderSides == Forms.ToolStripStatusLabelBorderSides.Right ? 11 : 0, 0)
            };
            fields.Children.Add(label);
            IDisposable? warning = null;
            void Refresh(object? sender, EventArgs args)
            {
                label.Text = item.Text;
                label.IsVisible = item.Visible;
                if (item.ForeColor == System.Drawing.Color.OrangeRed)
                    warning ??= label.Bind(TextBlock.ForegroundProperty, new DynamicResourceExtension("SystemControlErrorTextForegroundBrush"));
                else
                {
                    warning?.Dispose();
                    warning = null;
                }
                Measure(sender, args);
            }
            item.TextChanged += Refresh;
            item.VisibleChanged += Refresh;
            item.ForeColorChanged += Refresh;
            Refresh(null, EventArgs.Empty);
            return (Action)(() =>
            {
                item.TextChanged -= Refresh;
                item.VisibleChanged -= Refresh;
                item.ForeColorChanged -= Refresh;
                warning?.Dispose();
            });
        }).ToArray();

        var presenter = mdi ? new CreationRegionPresenter(editor, slot, bar, slot.Name) : null;
        editor.Disposed += (_, _) =>
        {
            slot.SizeChanged -= Measure;
            bar.PropertyChanged -= TypographyChanged;
            foreach (var unbind in bindings)
                unbind();
            presenter?.Dispose();
            if (slot is Forms.NativeControlHost host)
                host.NativeControl = null;
            source.Dispose();
        };
    }
}
