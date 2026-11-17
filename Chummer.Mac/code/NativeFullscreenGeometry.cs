#nullable enable
using System;
using System.Runtime.InteropServices;
using Avalonia;
using Avalonia.Controls;

namespace Chummer;

/// Native test geometry comes from the display, never from the window under test.
/// AppKit's standard fullscreen transition places content within NSScreen.safeAreaInsets
/// on camera-housing displays (developer.apple.com/documentation/appkit/nsscreen/safeareainsets).
internal static class NativeFullscreenGeometry
{
    [StructLayout(LayoutKind.Sequential)]
    private struct Insets { public double Top, Left, Bottom, Right; }

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr Selector(string name);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern IntPtr SendObject(IntPtr receiver, IntPtr selector);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern Insets SendInsetsArm64(IntPtr receiver, IntPtr selector);
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend_stret")]
    private static extern void SendInsetsX64(out Insets value, IntPtr receiver, IntPtr selector);

    internal static Size ExpectedClientSize(Window window)
    {
        var screen = window.Screens.ScreenFromWindow(window)
            ?? throw new InvalidOperationException("Native fullscreen display is unavailable.");
        Size size = screen.Bounds.Size.ToSize(screen.Scaling);
        if (!System.OperatingSystem.IsMacOS()) return size;
        var handle = window.TryGetPlatformHandle();
        if (handle == null || handle.Handle == IntPtr.Zero || handle.HandleDescriptor != "NSWindow")
            throw new InvalidOperationException("Native fullscreen requires its AppKit window handle.");
        IntPtr nativeScreen = SendObject(handle.Handle, Selector("screen"));
        if (nativeScreen == IntPtr.Zero) throw new InvalidOperationException("AppKit fullscreen display is unavailable.");
        Insets insets;
        if (RuntimeInformation.ProcessArchitecture == Architecture.Arm64)
            insets = SendInsetsArm64(nativeScreen, Selector("safeAreaInsets"));
        else if (RuntimeInformation.ProcessArchitecture == Architecture.X64)
            SendInsetsX64(out insets, nativeScreen, Selector("safeAreaInsets"));
        else throw new PlatformNotSupportedException("Unsupported AppKit structure-return ABI.");
        return ApplyInsets(size, new Thickness(insets.Left, insets.Top, insets.Right, insets.Bottom));
    }

    internal static Size ApplyInsets(Size display, Thickness insets)
    {
        if (!double.IsFinite(insets.Left) || !double.IsFinite(insets.Top) || !double.IsFinite(insets.Right) || !double.IsFinite(insets.Bottom)
            || insets.Left < 0 || insets.Top < 0 || insets.Right < 0 || insets.Bottom < 0
            || insets.Left + insets.Right >= display.Width || insets.Top + insets.Bottom >= display.Height)
            throw new InvalidOperationException("Invalid native display safe area.");
        return new Size(display.Width - insets.Left - insets.Right, display.Height - insets.Top - insets.Bottom);
    }
}
