#nullable enable
using System;
using System.Drawing;
using Avalonia.Controls;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class AdeptPowerHeader
{
    internal static void Attach(Forms.Form owner, Forms.TabPage page, Forms.Panel powers, Forms.Label heading,
        Forms.Label points, Forms.Button add)
    {
        if (Avalonia.Application.Current == null || owner.IsDisposed)
            return;

        powers.AutoScroll = true;
        var text = GearReadoutsLayout.BindReadout(owner, page, heading, points);
        AttachMeasured(owner, page, powers, "NativeAdeptPowerHeader", text, add, false, heading, points);
    }

    internal static void AttachMeasured(Forms.Form owner, Forms.Control page, Forms.Panel list, string name,
        TextBlock text, Forms.Button add, bool inline, params Forms.Label[] captions) =>
        AttachMeasured(owner, page, list, name, text, add, inline, inline ? 12 : 0, captions);

    internal static void AttachMeasured(Forms.Form owner, Forms.Control page, Forms.Panel list, string name,
        TextBlock text, Forms.Button add, bool inline, int listReserve, params Forms.Label[] captions)
    {
        int inset = inline ? 2 : 6;
        var workspace = owner.MdiParent ?? owner;
        var command = ArmorEquipmentLayout.BindCommand(owner, page, add);
        command.HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Left;
        Panel rows = inline
            ? new WrapPanel { ItemSpacing = 8, LineSpacing = 4, Children = { text, command } }
            : new StackPanel { Spacing = 4, Children = { text, command } };
        var surface = new Border { Child = rows, ClipToBounds = true };
        NativeControlPresentation.AddTheme(surface.Styles);
        list.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
        int headerHeight = 48;
        var presenter = new CreationRegionPresenter(owner, page, surface, name,
            () => new Rectangle(inset, inset,
                Math.Max(0, InventoryTreeLayout.VisiblePageSize(workspace, page).Width - 2 * inset), headerHeight),
            clipToSource: true);
        bool queued = false;
        void ScheduleLayout(object? sender, EventArgs args)
        {
            if (queued)
                return;

            queued = true;
            Dispatcher.UIThread.Post(() =>
            {
                queued = false;
                if (owner.IsDisposed)
                    return;
                var available = InventoryTreeLayout.VisiblePageSize(workspace, page);
                int width = Math.Max(0, available.Width - 2 * inset);
                rows.Measure(new Avalonia.Size(width, double.PositiveInfinity));
                headerHeight = Math.Min(Math.Max(0, available.Height - 3 * inset - listReserve),
                    Math.Max(32, (int)Math.Ceiling(rows.DesiredSize.Height)));
                int top = inset + headerHeight + inset;
                list.Bounds = new Rectangle(inset, top, width, Math.Max(0, available.Height - top - inset));
                presenter.Refresh();
            }, DispatcherPriority.Loaded);
        }

        void TypographyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty ||
                args.Property == Avalonia.Controls.Documents.TextElement.FontFamilyProperty)
                ScheduleLayout(sender, EventArgs.Empty);
        }

        surface.PropertyChanged += TypographyChanged;
        page.SizeChanged += ScheduleLayout;
        page.VisibleChanged += ScheduleLayout;
        workspace.SizeChanged += ScheduleLayout;
        foreach (var caption in captions)
            caption.TextChanged += ScheduleLayout;
        add.TextChanged += ScheduleLayout;
        owner.Disposed += (_, _) =>
        {
            surface.PropertyChanged -= TypographyChanged;
            page.SizeChanged -= ScheduleLayout;
            page.VisibleChanged -= ScheduleLayout;
            workspace.SizeChanged -= ScheduleLayout;
            foreach (var caption in captions)
                caption.TextChanged -= ScheduleLayout;
            add.TextChanged -= ScheduleLayout;
            presenter.Dispose();
        };
        ScheduleLayout(null, EventArgs.Empty);
    }
}
