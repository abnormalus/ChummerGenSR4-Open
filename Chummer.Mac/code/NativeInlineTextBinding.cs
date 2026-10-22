#nullable enable
using System;
using Avalonia;
using Avalonia.Controls;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Single-line literal text uses the original controller event boundary.
internal sealed class NativeInlineTextBinding : IDisposable
{
    private readonly Forms.TextBox source;
    private readonly Func<bool> available;
    private readonly Forms.InvalidateEventHandler invalidated;
    private bool syncing, disposed;
    internal TextBox Input { get; } = new() { MinHeight = 32, FontSize = 12 };
    internal NativeInlineTextBinding(Forms.TextBox source, Func<bool> available)
    {
        this.source = source; this.available = available;
        invalidated = (_, _) => Refresh();
        source.TextChanged += SourceChanged; source.EnabledChanged += SourceChanged;
        source.VisibleChanged += SourceChanged; source.Invalidated += invalidated;
        Input.PropertyChanged += NativeChanged; Refresh();
    }
    internal void Refresh()
    {
        if (disposed || syncing) return;
        syncing = true;
        try { Input.Text = source.Text; Input.IsEnabled = source.Enabled && available(); Input.IsVisible = source.Visible; }
        finally { syncing = false; }
    }
    private void SourceChanged(object? sender, EventArgs args) => Refresh();
    private void NativeChanged(object? sender, AvaloniaPropertyChangedEventArgs args)
    {
        if (disposed || syncing || args.Property != TextBox.TextProperty) return;
        if (!source.Enabled || !source.Visible || !available()) { Refresh(); return; }
        syncing = true;
        try { source.Text = Input.Text ?? ""; }
        finally { syncing = false; Refresh(); }
    }
    public void Dispose()
    {
        if (disposed) return;
        disposed = true; Input.PropertyChanged -= NativeChanged;
        source.TextChanged -= SourceChanged; source.EnabledChanged -= SourceChanged;
        source.VisibleChanged -= SourceChanged; source.Invalidated -= invalidated;
    }
}
