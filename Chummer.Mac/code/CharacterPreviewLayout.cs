#nullable enable
using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmViewer
{
    private void InitializeNativePreviewLayout()
    {
        if (Application.Current == null)
            return;

        Forms.Button[] commands = [cmdPrint, _savePdf, cmdSaveHTML, cmdSaveXML, _saveJson, _openBrowser];
        Forms.Control[] sources = [.. commands, label1, cboXSLT, _status];
        foreach (var source in sources)
            Controls.Remove(source);

        var actions = new WrapPanel { ItemSpacing = 8, LineSpacing = 4 };
        var header = new StackPanel { Spacing = 8, Children = { actions } };

        foreach (var source in commands)
        {
            var caption = new TextBlock { Text = source.Text, TextWrapping = TextWrapping.Wrap };
            var button = new Button
            {
                Name = "Native" + source.Name, Content = caption, IsEnabled = source.Enabled, IsVisible = source.Visible
            };
            AutomationProperties.SetName(button, source.Text);
            void Refresh()
            {
                caption.Text = source.Text;
                AutomationProperties.SetName(button, source.Text);
                button.IsEnabled = source.Enabled;
                button.IsVisible = source.Visible;
            }
            var observer = new NativeControlObservation(source, Refresh);
            void CaptionChanged(object? sender, EventArgs args) => Refresh();
            source.TextChanged += CaptionChanged;
            button.Click += (_, _) => source.PerformClick();
            actions.Children.Add(button);
            Disposed += (_, _) =>
            {
                source.TextChanged -= CaptionChanged;
                observer.Dispose();
            };
        }

        NativeControlPresentation.ConstrainWrapItems(actions);

        var choice = NativeListSelection.Attach(this, cboXSLT, label1.Text, layoutManaged: true)!;
        var sheet = new StackPanel
        {
            Spacing = 4, Children = { new TextBlock { Text = label1.Text, TextWrapping = TextWrapping.Wrap }, choice }
        };
        var status = new TextBlock
        {
            Name = "NativePreviewStatus", TextWrapping = TextWrapping.Wrap,
            MaxLines = 3, TextTrimming = TextTrimming.CharacterEllipsis
        };
        void RefreshStatus(object? sender, EventArgs args)
        {
            status.Text = _status.Text;
            status.IsVisible = status.Text.Length > 0;
            ToolTip.SetTip(status, status.Text);
        }
        _status.TextChanged += RefreshStatus;
        header.Children.Add(sheet);
        header.Children.Add(status);
        var documentSlot = new Border { IsHitTestVisible = false };
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("Auto,*"), RowSpacing = 12 };
        var headerViewport = new ScrollViewer
        {
            Content = header,
            HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Auto
        };
        layout.Children.Add(headerViewport);
        layout.SizeChanged += (_, args) =>
        {
            if (args.NewSize.Height > 0 && args.NewSize.Height != args.PreviousSize.Height)
                headerViewport.MaxHeight = args.NewSize.Height * 0.6;
        };
        Grid.SetRow(documentSlot, 1);
        layout.Children.Add(documentSlot);
        var surface = new Border { Child = layout };
        NativeControlPresentation.AddTheme(surface.Styles);
        var host = new Forms.NativeControlHost
        {
            Name = "NativeCharacterPreviewHeader", NativeControl = surface
        };
        Controls.Add(host);
        SizeChanged += ResizeSurface;
        Shown += ResizeSurface;
        ResizeSurface(null, EventArgs.Empty);
        webBrowser1.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
        MinimumSize = new System.Drawing.Size(360, 320);
        surface.LayoutUpdated += PositionDocument;
        Disposed += (_, _) =>
        {
            surface.LayoutUpdated -= PositionDocument;
            SizeChanged -= ResizeSurface;
            Shown -= ResizeSurface;
            _status.TextChanged -= RefreshStatus;
            host.NativeControl = null;
            foreach (var source in sources)
                source.Dispose();
        };
        RefreshStatus(null, EventArgs.Empty);

        void ResizeSurface(object? sender, EventArgs args)
        {
            host.Bounds = ClientRectangle;
            host.SyncNativeControl();
        }

        void PositionDocument(object? sender, EventArgs args)
        {
            // The platform webview is an airspace control. Match its bounds to the measured Avalonia row.
            if (documentSlot.TranslatePoint(default, surface) is not { } origin)
                return;

            var size = documentSlot.Bounds.Size;
            int left = host.Left + (int)Math.Ceiling(origin.X);
            int top = host.Top + (int)Math.Ceiling(origin.Y);
            int right = host.Left + (int)Math.Floor(origin.X + size.Width);
            int bottom = host.Top + (int)Math.Floor(origin.Y + size.Height);
            var bounds = new System.Drawing.Rectangle(left, top, Math.Max(0, right - left), Math.Max(0, bottom - top));
            if (webBrowser1.Bounds != bounds)
                webBrowser1.Bounds = bounds;
        }
    }
}
