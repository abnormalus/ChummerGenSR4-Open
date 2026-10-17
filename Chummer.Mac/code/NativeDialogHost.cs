#nullable enable
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Avalonia.Controls;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Registers direct Avalonia dialogs with the application's explicit Quit workflow.
// The native owner handles modal input; this registry handles cancellation and logging
// without inventing a hidden compatibility form or leaving an untracked window alive.
internal static class NativeDialogHost
{
    private static readonly List<Window> windows = new();
    internal static Window? ActiveWindow => windows.LastOrDefault();
    internal static string? ActiveDialogType => ActiveWindow?.GetType().Name;
    internal static int Count => windows.Count;
    internal static async Task<T?> ShowAsync<T>(Window window, Forms.Form owner) where T : class
    {
        var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner.MdiParent ?? owner);
        bool wasEnabled = nativeOwner.IsEnabled;
        var originalDialogTheme = window.RequestedThemeVariant;
        // Owned windows are separate theme roots: native modality does not imply
        // inheritance of an explicit owner override. Follow the actual owner live.
        void FollowOwnerTheme(object? sender, System.EventArgs args) => window.RequestedThemeVariant = nativeOwner.ActualThemeVariant;
        nativeOwner.ActualThemeVariantChanged += FollowOwnerTheme;
        windows.Add(window);
        AppDiagnostics.CaptureUiState();
        AppDiagnostics.Record("native-dialog-opening");
        try
        {
            FollowOwnerTheme(null, System.EventArgs.Empty);
            var pending = window.ShowDialog<T?>(nativeOwner);
            // Native modality blocks pointer input, but desktop menu commands use
            // IsEnabled as their shared availability guard. Keep that guard in sync.
            nativeOwner.IsEnabled = false;
            AppDiagnostics.CaptureUiState();
            AppDiagnostics.Record("native-dialog-opened");
            return await pending;
        }
        catch (System.Exception error)
        {
            AppDiagnostics.Record("native-dialog-failed", error);
            throw;
        }
        finally
        {
            nativeOwner.ActualThemeVariantChanged -= FollowOwnerTheme;
            window.RequestedThemeVariant = originalDialogTheme;
            windows.Remove(window);
            nativeOwner.IsEnabled = wasEnabled;
            AppDiagnostics.CaptureUiState();
            AppDiagnostics.Record("native-dialog-cleaned-up");
        }
    }
    internal static void CancelAll()
    {
        foreach (var window in windows.ToArray().Reverse()) window.Close();
    }
}
