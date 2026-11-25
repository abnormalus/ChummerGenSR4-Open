#nullable enable
using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.Markup.Xaml.MarkupExtensions;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeEditorSplitter
{
    internal static void Attach(Forms.Form editor, Forms.SplitContainer source, string label)
    {
        if (Application.Current == null || editor.MdiParent == null)
            return;

        // The compatibility splitter still lays out the panes. The native thumb owns its visible track.
        source.SplitterColor = System.Drawing.Color.Transparent;
        bool vertical = source.Orientation == Forms.Orientation.Vertical;
        var handle = new Thumb
        {
            Name = "Native" + source.Name,
            Focusable = true,
            Cursor = new Cursor(vertical ? StandardCursorType.SizeWestEast : StandardCursorType.SizeNorthSouth),
            Template = new FuncControlTemplate<Thumb>((_, _) =>
            {
                var track = new Border();
                track.Bind(Border.BackgroundProperty, new DynamicResourceExtension("ControlStrokeColorDefaultBrush"));
                return track;
            })
        };
        AutomationProperties.SetName(handle, label);
        NativeControlPresentation.AddTheme(handle.Styles);
        var presenter = new CreationRegionPresenter(editor, source, handle, handle.Name,
            TrackBounds, clipToSource: true);

        System.Drawing.Rectangle TrackBounds()
        {
            var first = source.Panel1.Bounds;
            var second = source.Panel2.Bounds;
            int end = vertical ? first.Right : first.Bottom;
            int next = vertical ? second.Left : second.Top;
            int thickness = Math.Clamp(next - end, 0, source.SplitterWidth);
            return vertical
                ? new System.Drawing.Rectangle(end, 0, thickness, source.ClientSize.Height)
                : new System.Drawing.Rectangle(0, end, source.ClientSize.Width, thickness);
        }

        TopLevel? dragRoot = null;
        double start = 0;
        int initialDistance = 0;
        double Axis(Point point) => vertical ? point.X : point.Y;

        handle.AddHandler(InputElement.PointerPressedEvent, (_, args) =>
        {
            if (!args.GetCurrentPoint(handle).Properties.IsLeftButtonPressed || source.IsSplitterFixed)
                return;

            dragRoot = TopLevel.GetTopLevel(handle);
            if (dragRoot == null)
                return;

            start = Axis(args.GetPosition(dragRoot));
            initialDistance = source.SplitterDistance;
            handle.Focus();
        }, RoutingStrategies.Bubble, handledEventsToo: true);
        handle.AddHandler(InputElement.PointerMovedEvent, (_, args) =>
        {
            if (dragRoot != null && args.GetCurrentPoint(handle).Properties.IsLeftButtonPressed && !source.IsSplitterFixed)
                source.SplitterDistance = Math.Max(0, initialDistance + (int)Math.Round(Axis(args.GetPosition(dragRoot)) - start));
        }, RoutingStrategies.Bubble, handledEventsToo: true);
        handle.DragCompleted += (_, _) => dragRoot = null;
        handle.PointerCaptureLost += (_, _) => dragRoot = null;
        handle.KeyDown += (_, args) =>
        {
            if (source.IsSplitterFixed)
                return;

            int sign = args.Key == (vertical ? Key.Left : Key.Up) ? -1 :
                args.Key == (vertical ? Key.Right : Key.Down) ? 1 : 0;
            if (sign == 0)
                return;

            source.SplitterDistance = Math.Max(0, source.SplitterDistance + sign * 8);
            args.Handled = true;
        };

        bool queued = false, disposed = false;
        void LayoutChanged(object? sender, EventArgs args)
        {
            if (queued || disposed)
                return;

            queued = true;
            Dispatcher.UIThread.Post(() =>
            {
                queued = false;
                if (!disposed)
                {
                    handle.IsEnabled = source.Enabled && !source.IsSplitterFixed;
                    handle.IsVisible = !source.Panel1Collapsed && !source.Panel2Collapsed;
                    presenter.Refresh();
                }
            }, DispatcherPriority.Loaded);
        }

        void RefreshPanelGeometry(object? sender, EventArgs args) => presenter.Refresh();
        source.Panel1.SizeChanged += RefreshPanelGeometry;
        source.Panel2.LocationChanged += RefreshPanelGeometry;
        source.Layout += LayoutChanged;
        source.EnabledChanged += LayoutChanged;
        source.SplitterMoved += LayoutChanged;
        editor.Disposed += (_, _) =>
        {
            disposed = true;
            source.Panel1.SizeChanged -= RefreshPanelGeometry;
            source.Panel2.LocationChanged -= RefreshPanelGeometry;
            source.Layout -= LayoutChanged;
            source.EnabledChanged -= LayoutChanged;
            source.SplitterMoved -= LayoutChanged;
            presenter.Dispose();
        };
        LayoutChanged(null, EventArgs.Empty);
    }
}
