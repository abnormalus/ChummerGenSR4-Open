#nullable enable
using System;
using System.Diagnostics;
using System.IO;
using System.Linq;
using System.Runtime.InteropServices;
using System.Text.Json;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Majorsilence.Forms;
using ApplicationWindow = Avalonia.Controls.Window;

namespace Chummer;

internal static class NativeFullscreenProbe
{
    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "sel_registerName")]
    private static extern IntPtr Selector(string name);

    [DllImport("/usr/lib/libobjc.A.dylib", EntryPoint = "objc_msgSend")]
    private static extern void SendAction(IntPtr receiver, IntPtr selector, IntPtr sender);

    internal static async Task RunAsync(frmMain main, Character fixture, string output, string scenario)
    {
        if (!System.OperatingSystem.IsMacOS())
            throw new PlatformNotSupportedException("The fullscreen probe requires AppKit.");

        using var writer = new StreamWriter(output) { AutoFlush = true };
        var workspace = main.ToAvaloniaWindow();
        bool nativeToggle = scenario.EndsWith("-native", StringComparison.Ordinal);
        bool activate = !scenario.EndsWith("-inactive", StringComparison.Ordinal);
        if (scenario is "handoff-managed" or "handoff-settled" or "handoff-retry")
        {
            var preceding = new ApplicationWindow { Title = "Fullscreen handoff probe", Width = 932, Height = 545 };
            preceding.Show();
            try
            {
                await MeasureAsync(preceding, "preceding-window", activate: true, nativeToggle: true);
                if (scenario == "handoff-settled")
                {
                    var precedingHandle = preceding.TryGetPlatformHandle()!.Handle;
                    SendAction(precedingHandle, Selector("toggleFullScreen:"), IntPtr.Zero);
                    for (int i = 0; i < 40 && (AppKitFullscreenNotifications.IsFullscreen(precedingHandle) ||
                        preceding.WindowState != WindowState.Normal || preceding.ClientSize.Width != 932); i++)
                        await Task.Delay(75);
                    if (AppKitFullscreenNotifications.IsFullscreen(precedingHandle) || preceding.WindowState != WindowState.Normal ||
                        preceding.ClientSize.Width != 932)
                        throw new InvalidOperationException("The preceding window did not finish exiting fullscreen.");
                }
            }
            finally { preceding.Close(); }
            await MeasureAsync(workspace, scenario, activate: true, nativeToggle: false);
            if (scenario == "handoff-retry")
            {
                var handle = workspace.TryGetPlatformHandle()!.Handle;
                if (workspace.WindowState != WindowState.Normal || AppKitFullscreenNotifications.IsFullscreen(handle))
                    throw new InvalidOperationException("Rejected fullscreen did not restore the normal window state.");
                await MeasureAsync(workspace, "retry", activate: true, nativeToggle: false);
                if (workspace.WindowState != WindowState.FullScreen || !AppKitFullscreenNotifications.IsFullscreen(handle) ||
                    Math.Abs(workspace.ClientSize.Width - NativeFullscreenGeometry.ExpectedClientSize(workspace).Width) > 1 ||
                    Math.Abs(workspace.ClientSize.Height - NativeFullscreenGeometry.ExpectedClientSize(workspace).Height) > 1)
                    throw new InvalidOperationException("Fullscreen could not be retried after the rejected request.");
            }
        }
        else if (scenario.StartsWith("plain-", StringComparison.Ordinal))
        {
            var plain = new ApplicationWindow { Title = "Fullscreen probe", Width = 932, Height = 545, ShowActivated = activate };
            plain.Show();
            try { await MeasureAsync(plain, scenario, activate, nativeToggle); }
            finally { plain.Close(); }
        }
        else if (scenario.StartsWith("forms-", StringComparison.Ordinal))
        {
            using var form = new Form { Text = "Fullscreen probe", Size = new System.Drawing.Size(932, 545) };
            form.Show();
            try { await MeasureAsync(form.ToAvaloniaWindow(), scenario, activate, nativeToggle); }
            finally { form.Close(); }
        }
        else if (scenario.StartsWith("character-", StringComparison.Ordinal))
        {
            var character = new Character();
            character.LoadSnapshot(fixture.CaptureSnapshot());
            using var editor = new frmCreate(character) { MdiParent = main, Loading = true };
            editor.Show();
            try { await MeasureAsync(workspace, scenario, activate, nativeToggle); }
            finally { editor.Close(); }
        }
        else
            await MeasureAsync(workspace, scenario, activate, nativeToggle);

        async Task MeasureAsync(ApplicationWindow window, string surface, bool activate, bool nativeToggle)
        {
            var handle = window.TryGetPlatformHandle()?.Handle ?? IntPtr.Zero;
            if (handle == IntPtr.Zero)
                throw new InvalidOperationException("Fullscreen probe window has no native handle.");

            bool AppKitFullscreen() => AppKitFullscreenNotifications.IsFullscreen(handle);
            if (activate)
                window.Activate();
            await Task.Delay(250);
            var expected = NativeFullscreenGeometry.ExpectedClientSize(window);
            var clock = Stopwatch.StartNew();
            void Log(string phase, string? via = null)
            {
                writer.WriteLine(JsonSerializer.Serialize(new
                {
                    surface, activate, nativeToggle, phase, ms = clock.ElapsedMilliseconds,
                    active = window.IsActive, appKitFullScreen = AppKitFullscreen(),
                    state = window.WindowState.ToString(), client = window.ClientSize.ToString(),
                    requested = $"{window.Width}x{window.Height}", frame = window.FrameSize.ToString(),
                    position = window.Position.ToString(), expected = expected.ToString(), via
                }));
            }
            void Resized(object? sender, WindowResizedEventArgs args) => Log("resized");
            void Changed(object? sender, AvaloniaPropertyChangedEventArgs args)
            {
                if (args.Property == ApplicationWindow.WindowStateProperty)
                    Log("state");
                if (args.Property == ApplicationWindow.WidthProperty || args.Property == ApplicationWindow.HeightProperty)
                    Log(args.Property.Name, string.Join(" > ", new StackTrace().GetFrames().Skip(1).Take(15)
                        .Select(frame => frame.GetMethod()).Where(method => method != null)
                        .Select(method => method!.DeclaringType?.FullName + "." + method.Name)));
            }
            var stopWatching = AppKitFullscreenNotifications.Watch(handle, phase => Log(phase), "WillEnter", "DidEnter", "WillExit", "DidExit", "DidFailToEnter", "DidFailToExit");
            window.Resized += Resized;
            window.PropertyChanged += Changed;
            try
            {
                Log("before");
                if (nativeToggle)
                    SendAction(handle, Selector("toggleFullScreen:"), IntPtr.Zero);
                else
                    window.WindowState = WindowState.FullScreen;
                Log("requested");
                bool previous = AppKitFullscreen();
                for (int i = 0; i < 32; i++)
                {
                    await Task.Delay(75);
                    bool current = AppKitFullscreen();
                    if (current != previous)
                        Log("AppKit style changed");
                    previous = current;
                }
                Log("settled");
            }
            finally
            {
                stopWatching();
                window.Resized -= Resized;
                window.PropertyChanged -= Changed;
            }
        }
    }
}
