#nullable enable
using System;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

internal static class NativeRenderingChecks
{
    private static async Task AwaitFrameAsync(Func<bool> painted)
    {
        DateTime deadline = DateTime.UtcNow.AddSeconds(10);
        while (!painted())
        {
            if (DateTime.UtcNow >= deadline) throw new TimeoutException("Native repaint did not arrive.");
            await Task.Delay(25);
        }
    }

    internal static async Task RunAsync(Action<bool, string> check)
    {
        bool previousMode = RenderingDiagnostics.ForceFullRedraw;
        using var window = new Form { Text = "Rendering checks", Width = 400, Height = 180 };
        var details = new Panel { Dock = DockStyle.Fill };
        var value = new Label { Left = 10, Top = 10, Width = 180, Text = "Capacity: 1250" };
        details.Controls.Add(value); window.Controls.Add(details);
        using var fullRedrawCommand = AppRenderingDiagnostics.CreateMenuCommand();
        int childPaints = 0, windowPaints = 0;
        value.Paint += (_, _) => childPaints++;
        window.Paint += (_, _) => windowPaints++;
        try
        {
            AppRenderingDiagnostics.SetFullRedraw(false);
            window.Show();
            await AwaitFrameAsync(() => childPaints > 0);
            childPaints = 0;
            window.Invalidate(true);
            await AwaitFrameAsync(() => childPaints > 0);
            check(childPaints > 0, "Native full invalidation repaints nested detail controls");

            childPaints = 0; windowPaints = 0;
            window.Invalidate(false);
            await AwaitFrameAsync(() => windowPaints > 0);
            check(childPaints == 0, "Native normal invalidation preserves unchanged control caches");

            childPaints = 0;
            fullRedrawCommand.PerformClick();
            await AwaitFrameAsync(() => childPaints > 0);
            check(childPaints > 0 && fullRedrawCommand.Checked && RenderingDiagnostics.ForceFullRedraw,
                "Native full redraw menu command enables nested-control repainting");

            fullRedrawCommand.PerformClick();
            // The mode transition invalidates once; measure only subsequent normal frames.
            windowPaints = 0;
            await AwaitFrameAsync(() => windowPaints > 0);
            childPaints = 0; windowPaints = 0;
            window.Invalidate(false);
            await AwaitFrameAsync(() => windowPaints > 0);
            check(childPaints == 0 && !fullRedrawCommand.Checked && !RenderingDiagnostics.ForceFullRedraw,
                "Native cached rendering resumes after disabling the diagnostic menu command");

            value.Visible = false;
            AppRenderingDiagnostics.SetFullRedraw(true);
            childPaints = 0; windowPaints = 0;
            window.Invalidate(true);
            await AwaitFrameAsync(() => windowPaints > 0);
            check(childPaints == 0, "Native full redraw omits hidden detail controls");
        }
        finally
        {
            AppRenderingDiagnostics.SetFullRedraw(previousMode);
            window.Close();
        }
    }
}
