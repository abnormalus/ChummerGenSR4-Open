#nullable enable
using System;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Forms = Majorsilence.Forms;

namespace Chummer;

// A native command mirrors availability but delegates intent to its owner.
internal sealed class NativeInlineCommandBinding : IDisposable
{
    private readonly Forms.Control source;
    private readonly Forms.ToolTip tips;
    private readonly Func<bool> available;
    private readonly Func<string> caption;
    private readonly Action execute;
    private readonly Forms.InvalidateEventHandler invalidated;
    private bool disposed;
    internal Button Input { get; } = new() { MinHeight = 32, FontSize = 12, Padding = new Thickness(6, 2), Margin = new Thickness(1) };
    internal NativeInlineCommandBinding(Forms.Control source, Forms.ToolTip tips, Func<bool> available, Action execute, Func<string>? caption = null)
    {
        this.source = source; this.tips = tips; this.available = available; this.execute = execute;
        this.caption = caption ?? (() => source.Text);
        invalidated = (_, _) => Refresh();
        source.TextChanged += SourceChanged; source.EnabledChanged += SourceChanged;
        source.VisibleChanged += SourceChanged; source.Invalidated += invalidated;
        Input.Click += Click; Refresh();
    }
    internal void Refresh()
    {
        if (disposed) return;
        Input.Content = caption(); Input.IsEnabled = source.Enabled && available(); Input.IsVisible = source.Visible;
        ToolTip.SetTip(Input, tips.GetToolTip(source));
    }
    private void SourceChanged(object? sender, EventArgs args) => Refresh();
    private void Click(object? sender, RoutedEventArgs args)
    {
        if (disposed || !Input.IsEffectivelyEnabled || !source.Enabled || !source.Visible || !available()) return;
        execute(); Refresh();
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; Input.Click -= Click;
        source.TextChanged -= SourceChanged; source.EnabledChanged -= SourceChanged;
        source.VisibleChanged -= SourceChanged; source.Invalidated -= invalidated;
    }
}
