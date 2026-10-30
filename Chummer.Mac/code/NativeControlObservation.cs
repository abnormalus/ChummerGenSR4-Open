#nullable enable
using System;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Owns the common presentation subscriptions used by native controller adapters.
// The adapter still owns value/text events, feedback guards and domain validation;
// this observer never writes to or disposes the legacy control.
internal sealed class NativeControlObservation : IDisposable
{
    private readonly Forms.Control source;
    private readonly Action refresh;
    private readonly Forms.InvalidateEventHandler invalidated;
    private bool disposed;

    internal NativeControlObservation(Forms.Control source, Action refresh)
    {
        this.source = source;
        this.refresh = refresh;
        invalidated = (_, _) => Refresh();
        source.EnabledChanged += SourceChanged;
        source.VisibleChanged += SourceChanged;
        source.Invalidated += invalidated;
        // Initial synchronization remains explicit in the adapter, after its
        // native input and input-event handlers have been fully initialized.
    }

    private void SourceChanged(object? sender, EventArgs args) => Refresh();
    private void Refresh()
    {
        if (!disposed) refresh();
    }

    public void Dispose()
    {
        if (disposed) return;
        // Mark first so a callback already captured by an event cannot refresh a
        // detached adapter. Disposal is safe when its owner closes more than once.
        disposed = true;
        source.EnabledChanged -= SourceChanged;
        source.VisibleChanged -= SourceChanged;
        source.Invalidated -= invalidated;
    }
}
