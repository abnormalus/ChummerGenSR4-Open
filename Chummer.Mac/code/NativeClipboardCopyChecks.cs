#nullable enable
using System;
using System.Threading.Tasks;
using Forms = Majorsilence.Forms;

namespace Chummer;

// Native-only theme/dispatcher coverage; shared commands also run headlessly.
internal static class NativeClipboardCopyChecks
{
    internal static async Task RunAsync(Forms.Form main, Character original, Action<bool, string> check)
    {
        var app = Avalonia.Application.Current!; var oldTheme = app.RequestedThemeVariant;
        try
        {
            foreach (var theme in new[] { Avalonia.Styling.ThemeVariant.Light, Avalonia.Styling.ThemeVariant.Dark })
            {
                app.RequestedThemeVariant = theme;
                await ClipboardTransferChecks.RunAsync(main, original, check, () => Task.Delay(100), "Native " + theme.Key);
            }
        }
        finally { app.RequestedThemeVariant = oldTheme; }
    }
}
