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
    internal static void Attach(Forms.Form editor, Forms.NumericUpDown source, Forms.ToolTip? tips, string label)
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
                new Setter(TextBox.ClipToBoundsProperty, true),
                new Setter(TextBox.PaddingProperty, new Thickness(1, 0)),
                new Setter(TextBox.VerticalContentAlignmentProperty, VerticalAlignment.Center)
            }
        });
        input.TemplateApplied += (_, _) => Dispatcher.UIThread.Post(() => CompactButtons(input), DispatcherPriority.Loaded);

        bool mdi = editor.MdiParent != null;
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

        var presenter = mdi ? new CreationRegionPresenter(editor, slot, region, slot.Name) : null;

        void SyncEnabled(object? sender, EventArgs args) => region.IsEnabled = slot.Enabled;
        slot.EnabledChanged += SyncEnabled;
        SyncEnabled(null, EventArgs.Empty);
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
        source.LocationChanged += FollowBounds;
        source.SizeChanged += FollowBounds;

        void DisposeBinding(object? sender, EventArgs args)
        {
            slot.EnabledChanged -= SyncEnabled;
            slot.VisibleChanged -= QueueHostSync;
            slot.LocationChanged -= QueueHostSync;
            slot.SizeChanged -= QueueHostSync;
            source.LocationChanged -= FollowBounds;
            source.SizeChanged -= FollowBounds;
            presenter?.Dispose();
            if (directHost != null)
                directHost.NativeControl = null;
            binding.Dispose();
            source.Dispose();
        }
        editor.Disposed += DisposeBinding;
    }

    private static void CompactButtons(NumericUpDown spinner)
    {
        // Fluent joins the editor and button borders. Keep the value inside its own space.
        var editor = spinner.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
        if (editor != null)
            editor.Margin = new Thickness(0, 0, 1, 0);

        foreach (var button in spinner.GetVisualDescendants().OfType<RepeatButton>())
        {
            if (button.Name is not ("PART_IncreaseButton" or "PART_DecreaseButton"))
                continue;

            // Fluent sets the repeat-button minimum directly on the template part.
            button.MinWidth = 0;
            button.Width = 10;
            button.MinHeight = 0;
            button.Height = 18;
            button.Padding = new Thickness(0);
        }
    }

}
