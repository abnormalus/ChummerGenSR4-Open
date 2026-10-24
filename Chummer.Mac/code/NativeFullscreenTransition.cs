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
    internal static async Task<DateTime> RequestAsync(Window window)
    {
        // Normalization and fullscreen readiness share the original operation
        // budget. Callers retain their strict actual-screen and complete-row gates.
        DateTime deadline = DateTime.UtcNow.AddSeconds(20);
        window.WindowState = WindowState.Normal;
        DateTime? stableSince = null;
        Size previous = default;
        while (DateTime.UtcNow < deadline)
        {
            bool normal = window.WindowState == WindowState.Normal &&
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
                    window.WindowState = WindowState.FullScreen;
                    return deadline;
                }
            }
            else stableSince = null;
            previous = window.ClientSize;
            await Task.Delay(10);
        }
        throw new TimeoutException("Native fullscreen test could not establish the preceding normal window state.");
    }
}
