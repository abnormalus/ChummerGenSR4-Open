#nullable enable
using System.Linq;
using Majorsilence.Forms;

namespace Chummer;

// Keep diagnosis separate from user preferences and character serialization. Turning
// this on trades performance for evidence; it must not become an invisible workaround.
internal static class AppRenderingDiagnostics
{
    internal static void SetFullRedraw(bool enabled)
    {
        if (RenderingDiagnostics.ForceFullRedraw == enabled) return;
        RenderingDiagnostics.ForceFullRedraw = enabled;
        AppDiagnostics.Record(enabled ? "rendering.full-redraw-on" : "rendering.full-redraw-off");
        foreach (Form window in Application.OpenForms.Cast<Form>().ToArray())
            window.Invalidate(true);
    }

    internal static ToolStripMenuItem CreateMenuCommand()
    {
        var command = new ToolStripMenuItem(LanguageManager.Instance.GetString("Menu_FullRedrawDiagnostic"))
        {
            Name = "fullRedrawDiagnostic", Tag = "Menu_FullRedrawDiagnostic",
            Checked = RenderingDiagnostics.ForceFullRedraw
        };
        command.Click += (_, _) =>
        {
            SetFullRedraw(!RenderingDiagnostics.ForceFullRedraw);
            command.Checked = RenderingDiagnostics.ForceFullRedraw;
        };
        return command;
    }
}
