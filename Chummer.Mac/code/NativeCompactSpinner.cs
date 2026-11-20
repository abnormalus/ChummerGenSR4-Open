#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Layout;
using Avalonia.Styling;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCompactSpinner
{
    internal static void Attach(Forms.Form owner, Forms.NumericUpDown source, Forms.ToolTip tips, string label)
    {
        if (Application.Current == null || source.Parent is not { } parent)
            return;

        var binding = new NativeInlineNumericBinding(source, tips, wholeNumbers: source.DecimalPlaces == 0);
        var input = binding.Input;
        input.Width = double.NaN;
        input.MinHeight = 0;
        input.MinWidth = 0;
        input.Height = double.NaN;
        input.FontSize = 11;
        input.Padding = new Thickness(1, 0);
        AutomationProperties.SetName(input, label);

        var region = new Border { Child = input, ClipToBounds = true };
        NativeControlPresentation.AddTheme(region.Styles);
        region.Styles.Add(new Style(selector => selector.OfType<NumericUpDown>().Template().OfType<TextBox>())
        {
            Setters =
            {
                new Setter(TextBox.MinHeightProperty, 0d),
                new Setter(TextBox.HeightProperty, 20d),
                new Setter(TextBox.MinWidthProperty, 0d),
                new Setter(TextBox.PaddingProperty, new Thickness(1, 0)),
                new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center)
            }
        });
        input.TemplateApplied += (_, _) => Dispatcher.UIThread.Post(() => CompactButtons(input), DispatcherPriority.Loaded);

        bool mdi = owner.MdiParent != null;
        Forms.Control slot = mdi ? new Forms.Panel() : new Forms.NativeControlHost { NativeControl = region };
        slot.Name = input.Name!;
        slot.Bounds = source.Bounds;
        slot.Anchor = mdi ? Forms.AnchorStyles.Top | Forms.AnchorStyles.Left : source.Anchor;
        slot.TabIndex = source.TabIndex;
        slot.TabStop = false;
        parent.Controls.Add(slot);
        parent.Controls.Remove(source);
        source.CreateControl();
        binding.Refresh();

        var presenter = mdi ? new CreationRegionPresenter(owner, slot, region, slot.Name) : null;
        var directHost = slot as Forms.NativeControlHost;
        void QueueHostSync(object? sender, EventArgs args)
        {
            if (directHost != null)
                Dispatcher.UIThread.Post(() =>
                {
                    if (!directHost.IsDisposed)
                        directHost.SyncNativeControl();
                }, DispatcherPriority.Loaded);
        }

        if (directHost != null)
        {
            slot.VisibleChanged += QueueHostSync;
            slot.LocationChanged += QueueHostSync;
            slot.SizeChanged += QueueHostSync;
        }
        void FollowBounds(object? sender, EventArgs args) => slot.Bounds = source.Bounds;
        if (mdi)
        {
            source.LocationChanged += FollowBounds;
            source.SizeChanged += FollowBounds;
        }

        owner.Disposed += (_, _) =>
        {
            slot.VisibleChanged -= QueueHostSync;
            slot.LocationChanged -= QueueHostSync;
            slot.SizeChanged -= QueueHostSync;
            source.LocationChanged -= FollowBounds;
            source.SizeChanged -= FollowBounds;
            presenter?.Dispose();
            binding.Dispose();
            source.Dispose();
        };
    }

    private static void CompactButtons(NumericUpDown spinner)
    {
        foreach (var button in spinner.GetVisualDescendants().OfType<RepeatButton>())
        {
            if (button.Name is not ("PART_IncreaseButton" or "PART_DecreaseButton"))
                continue;

            // Fluent sets the repeat-button minimum directly on the template part.
            button.MinWidth = 0;
            button.Width = 11;
            button.MinHeight = 0;
            button.Height = 18;
            button.Padding = new Thickness(0);
        }
    }

}
