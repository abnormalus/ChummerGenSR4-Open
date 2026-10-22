#nullable enable
using System;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Interactivity;

namespace Chummer;

// Test the visible native command after framework layout/scrolling completes.
internal static class NativeContactInteractions
{
    internal static async Task ClickAsync(CreationContactSection section, Button command, Action<bool, string>? check = null)
    {
        string Geometry() => "[command=" + command.Bounds + "; position=" + command.TranslatePoint(default, section.Viewport)
            + "; visible=" + command.IsEffectivelyVisible + "; enabled=" + command.IsEffectivelyEnabled
            + "; viewportBounds=" + section.Viewport.Bounds + "; viewport=" + section.Viewport.Viewport
            + "; extent=" + section.Viewport.Extent + "; offset=" + section.Viewport.Offset + "]";
        var deadline = DateTime.UtcNow.AddSeconds(20);
        while (command.Bounds.Width <= 0 || section.Viewport.Bounds.Height <= 0)
        { if (DateTime.UtcNow >= deadline) { check?.Invoke(false, "Native contact command was not laid out " + Geometry()); throw new TimeoutException("Native contact command was not laid out."); } await Task.Delay(10); }
        command.BringIntoView();
        while (!(command.IsEffectivelyVisible && command.IsEffectivelyEnabled
            && command.TranslatePoint(default, section.Viewport) is { } point && point.X >= -1 && point.Y >= -1
            && point.X + command.Bounds.Width <= section.Viewport.Bounds.Width + 1
            && point.Y + command.Bounds.Height <= section.Viewport.Bounds.Height + 1))
        { if (DateTime.UtcNow >= deadline) { check?.Invoke(false, "Native contact command inaccessible after framework scrolling " + Geometry()); throw new TimeoutException("Native contact command did not become accessible after scrolling."); } await Task.Delay(10); }
        command.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
    }
}
