namespace Majorsilence.Forms;

/// <summary>Opt-in rendering diagnostics for applications using cached compatibility controls.</summary>
public static class RenderingDiagnostics
{
    /// <summary>
    /// Repaints every visible control on each requested frame instead of reusing control pixels.
    /// Set on the UI thread, then invalidate open windows. This does not schedule idle frames.
    /// The application should leave it off normally and avoid persisting it as a user preference.
    /// </summary>
    public static bool ForceFullRedraw { get; set; }
}
