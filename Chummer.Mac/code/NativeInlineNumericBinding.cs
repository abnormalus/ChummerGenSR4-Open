#nullable enable
using System;
using System.Globalization;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Forms = Majorsilence.Forms;

namespace Chummer;

// A controller adapter for bounded native sections. The caller owns presentation
// and source lifetime; existing controller events continue to own domain rules.
internal sealed class NativeInlineNumericBinding : IDisposable
{
    private readonly Forms.NumericUpDown source;
    private readonly Forms.ToolTip tips;
    private readonly bool wholeNumbers;
    private readonly Func<bool>? available;
    private readonly Forms.InvalidateEventHandler invalidated;
    private bool syncing, disposed;
    internal NumericUpDown Input { get; }

    internal NativeInlineNumericBinding(Forms.NumericUpDown source, Forms.ToolTip tips, bool wholeNumbers, Func<bool>? available = null)
    {
        this.source = source; this.tips = tips; this.wholeNumbers = wholeNumbers; this.available = available;
        Input = new NumericUpDown { Name = "native" + source.Name, Width = 120, MinHeight = 32, FontSize = 12,
            Padding = new Thickness(2), ClipValueToMinMax = true,
            ParsingNumberStyle = wholeNumbers ? NumberStyles.Integer : NumberStyles.Number };
        invalidated = (_, _) => Refresh();
        source.ValueChanged += SourceChanged; source.EnabledChanged += SourceChanged;
        source.VisibleChanged += SourceChanged; source.Invalidated += invalidated;
        Input.ValueChanged += NativeChanged; Input.LostFocus += NativeLostFocus;
        Refresh();
    }
    internal void Refresh()
    {
        if (disposed || syncing) return;
        syncing = true;
        try
        {
            Input.Maximum = Math.Max(Input.Maximum, source.Maximum);
            Input.Minimum = source.Minimum; Input.Maximum = source.Maximum;
            Input.Increment = source.Increment;
            Input.FormatString = source.DecimalPlaces == 0 ? "0" : "F" + source.DecimalPlaces;
            Input.Value = source.Value; Input.IsEnabled = source.Enabled; Input.IsVisible = source.Visible;
            ToolTip.SetTip(Input, tips.GetToolTip(source));
        }
        finally { syncing = false; }
    }
    private void SourceChanged(object? sender, EventArgs args) => Refresh();
    private void NativeChanged(object? sender, NumericUpDownValueChangedEventArgs args)
    {
        if (disposed || syncing) return;
        if (!source.Enabled || available?.Invoke() == false) { Refresh(); return; }
        if (Input.Value is not decimal value) return;
        if (wholeNumbers && decimal.Truncate(value) != value) { Refresh(); return; }
        syncing = true;
        try { source.Value = Math.Clamp(value, source.Minimum, source.Maximum); }
        finally { syncing = false; Refresh(); }
    }
    private void NativeLostFocus(object? sender, RoutedEventArgs args)
    { if (!disposed && !Input.Value.HasValue) Refresh(); }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true;
        source.ValueChanged -= SourceChanged; source.EnabledChanged -= SourceChanged;
        source.VisibleChanged -= SourceChanged; source.Invalidated -= invalidated;
        Input.ValueChanged -= NativeChanged; Input.LostFocus -= NativeLostFocus;
    }
}
