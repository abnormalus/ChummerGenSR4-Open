#nullable enable
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;

namespace Chummer;

// Native test driver only. A requested FullScreen property can outlive the OS
// transition when a preceding fixture restores normal dimensions. Reassigning
// that same property value does not request another native transition.
internal static class NativeFullscreenTransition
{
    internal static async Task<DateTime> RequestAsync(Window window, Action<bool, string> check)
    {
        // Normalization and fullscreen readiness share the original operation
        // budget. Callers retain their strict actual-screen and complete-row gates.
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        // Preserve an already completed native fullscreen transition. Repeated
        // fixtures share this window; exiting it solely to re-enter queues an
        // unnecessary native transition and can invalidate the next capture.
        var screen = window.Screens.ScreenFromWindow(window);
        bool IsActualFullscreen()
        {
            if (screen == null || window.WindowState != WindowState.FullScreen) return false;
            Size expected = screen.Bounds.Size.ToSize(screen.Scaling);
            if (Math.Abs(window.ClientSize.Width - expected.Width) <= 1 &&
                Math.Abs(window.ClientSize.Height - expected.Height) <= 1 &&
                window.FrameSize is { } fullscreenFrame &&
                Math.Abs(fullscreenFrame.Width - expected.Width) <= 1 &&
                Math.Abs(fullscreenFrame.Height - expected.Height) <= 1)
                return true;
            return false;
        }
        if (IsActualFullscreen()) return deadline;

        bool needsNativeExit = System.OperatingSystem.IsMacOS() && window.WindowState != WindowState.Normal;
        var clock = System.Diagnostics.Stopwatch.StartNew();
        var trace = new System.Collections.Generic.List<string>();
        int synchronousResizes = 0, asynchronousResizes = 0;
        void Trace(string reason)
        {
            if (trace.Count < 32)
                trace.Add(FormattableString.Invariant($"{clock.ElapsedMilliseconds}ms:{reason}:state={window.WindowState}:client={window.ClientSize}:requested={window.Width}x{window.Height}:frame={window.FrameSize}:position={window.Position}"));
        }
        Trace("before-normal");
        bool normalRequestReturned = false;
        bool nativeExitAcknowledged = !needsNativeExit;
        EventHandler<WindowResizedEventArgs> acknowledgeExit = (_, _) =>
        {
            // SetWindowState synchronously reports provisional dimensions on Mac.
            // Only a later native-loop resize acknowledges the OS exit request.
            if (normalRequestReturned) asynchronousResizes++; else synchronousResizes++;
            Trace(normalRequestReturned ? "async-resize" : "sync-resize");
            if (normalRequestReturned && window.WindowState == WindowState.Normal)
                nativeExitAcknowledged = true;
        };
        window.Resized += acknowledgeExit;
        try
        {
            window.WindowState = WindowState.Normal;
            normalRequestReturned = true;
            Trace("normal-setter-returned");
            DateTime? stableSince = null;
            Size previous = default;
            while (DateTime.UtcNow < deadline)
            {
                // Native state reconciliation can complete fullscreen while a
                // normal request is pending. Preserve that completed geometry;
                // callers still require their original stable fullscreen gate.
                if (IsActualFullscreen())
                {
                    Trace("native-fullscreen-reconciled");
                    return deadline;
                }
                bool normal = nativeExitAcknowledged && window.WindowState == WindowState.Normal &&
                    window.ClientSize.Width > 0 && window.ClientSize.Height > 0;
                // The Mac host uses native title-bar decorations in normal mode.
                // Their frame must have returned before requesting a new fullscreen
                // transition; the property alone is only requested state.
                if (System.OperatingSystem.IsMacOS() && window.WindowDecorations == WindowDecorations.Full)
                    normal &= window.FrameSize is { } frame && frame.Height > window.ClientSize.Height + 1;
                if (normal && window.ClientSize == previous)
                {
                    stableSince ??= DateTime.UtcNow;
                    if (DateTime.UtcNow - stableSince.Value >= TimeSpan.FromMilliseconds(120))
                    {
                        Trace("normal-acknowledged");
                        window.WindowState = WindowState.FullScreen;
                        return deadline;
                    }
                }
                else stableSince = null;
                previous = window.ClientSize;
                await Task.Delay(10);
            }
            Trace("normal-timeout");
            check(false, FormattableString.Invariant($"Native fullscreen preceding normal transition acknowledged [needsExit={needsNativeExit}; acknowledged={nativeExitAcknowledged}; syncResizes={synchronousResizes}; asyncResizes={asynchronousResizes}; decorations={window.WindowDecorations}; trace={string.Join("|", trace)}]"));
            throw new TimeoutException("Native fullscreen test could not acknowledge the preceding native normal transition.");
        }
        finally { window.Resized -= acknowledgeExit; }
    }
}
