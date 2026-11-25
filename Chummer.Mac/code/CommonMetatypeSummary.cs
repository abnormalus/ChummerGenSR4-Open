#nullable enable
using System;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class CommonMetatypeSummary
{
    internal static System.Drawing.Rectangle Bounds(Forms.Form owner, Forms.Control page, Forms.Label caption)
    {
        var viewport = InventoryTreeLayout.VisiblePageSize(owner.MdiParent ?? owner, page);
        return new System.Drawing.Rectangle(caption.Left, 2,
            Math.Max(0, viewport.Width - caption.Left - 6), viewport.Height / 8);
    }

    internal static void Attach(Forms.Form owner, Forms.Label caption, Forms.Label metatype, Forms.Label sourceCaption,
        Forms.Label source, Forms.ToolTip tips, Action openSource, Forms.Label? possessed = null)
    {
        owner.Load += (_, _) =>
        {
            if (Application.Current != null)
                Mount(owner, caption, metatype, sourceCaption, source, tips, openSource, possessed);
        };
    }

    private static void Mount(Forms.Form owner, Forms.Label caption, Forms.Label metatype, Forms.Label sourceCaption,
        Forms.Label source, Forms.ToolTip tips, Action openSource, Forms.Label? possessed)
    {
        var page = caption.Parent!;
        var controllers = new[] { caption, metatype, sourceCaption, source, possessed };
        foreach (var controller in controllers)
            if (controller != null)
                page.Controls.Remove(controller);

        var identity = new TextBlock
        {
            Name = "NativeMetatypeIdentity", TextWrapping = TextWrapping.Wrap
        };
        var reference = new Button
        {
            Name = "NativeMetatypeSource", HorizontalAlignment = HorizontalAlignment.Stretch,
            HorizontalContentAlignment = HorizontalAlignment.Left, Padding = new Thickness(0)
        };
        var referenceText = new TextBlock { TextWrapping = TextWrapping.Wrap };
        reference.Content = referenceText;
        reference.Click += (_, _) => openSource();

        var layout = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), RowSpacing = 2 };
        layout.Children.Add(identity);
        Grid.SetRow(reference, 1);
        layout.Children.Add(reference);
        var scroll = new ScrollViewer
        {
            Content = layout, HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto
        };
        NativeControlPresentation.AddTheme(scroll.Styles);
        var presenter = new CreationRegionPresenter(owner, page, scroll, "NativeCommonMetatype",
            () => Bounds(owner, page, caption),
            clipToSource: true);

        void Refresh(object? sender, EventArgs args)
        {
            string value = caption.Text + " " + metatype.Text;
            if (possessed is { Visible: true } && !string.IsNullOrWhiteSpace(possessed.Text))
                value += " · " + possessed.Text;
            identity.Text = value;
            ToolTip.SetTip(identity, value);
            string citation = sourceCaption.Text + " " + source.Text;
            referenceText.Text = citation;
            reference.IsEnabled = !string.IsNullOrWhiteSpace(source.Text);
            AutomationProperties.SetName(reference, citation);
            // The editor assigns the source tooltip immediately after updating its label.
            Dispatcher.UIThread.Post(() =>
            {
                if (!owner.IsDisposed)
                    ToolTip.SetTip(reference, tips.GetToolTip(source) is { Length: > 0 } tip ? tip : citation);
            }, DispatcherPriority.Loaded);
        }

        caption.TextChanged += Refresh;
        sourceCaption.TextChanged += Refresh;
        metatype.TextChanged += Refresh;
        source.TextChanged += Refresh;
        if (possessed != null)
        {
            possessed.TextChanged += Refresh;
            possessed.VisibleChanged += Refresh;
        }
        void Reposition(object? sender, EventArgs args) => presenter.Refresh();
        caption.LocationChanged += Reposition;
        owner.Disposed += (_, _) =>
        {
            caption.TextChanged -= Refresh;
            sourceCaption.TextChanged -= Refresh;
            metatype.TextChanged -= Refresh;
            source.TextChanged -= Refresh;
            if (possessed != null)
            {
                possessed.TextChanged -= Refresh;
                possessed.VisibleChanged -= Refresh;
            }
            caption.LocationChanged -= Reposition;
            presenter.Dispose();
            foreach (var controller in controllers)
                controller?.Dispose();
        };
        Refresh(null, EventArgs.Empty);
    }
}
