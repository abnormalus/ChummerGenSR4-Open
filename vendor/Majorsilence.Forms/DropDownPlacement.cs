using System.Drawing;

namespace Majorsilence.Forms;

/// <summary>Working-area placement for legacy dropdown windows awaiting native control migration.</summary>
public static class DropDownPlacement
{
    /// <summary>Positions a dropdown below its anchor, or above when more space is available.</summary>
    /// <remarks>All arguments and the result use device pixels. Oversized lists shrink and scroll;
    /// callers convert the resulting size back to their backend's logical units.</remarks>
    public static Rectangle Calculate(Rectangle anchor, Size requested, Rectangle workingArea)
    {
        int width = Math.Min(Math.Max(1, requested.Width), Math.Max(1, workingArea.Width));
        int below = Math.Clamp(workingArea.Bottom - anchor.Bottom, 0, Math.Max(0, workingArea.Height));
        int above = Math.Clamp(anchor.Top - workingArea.Top, 0, Math.Max(0, workingArea.Height));
        bool opensAbove = requested.Height > below && above > below;
        int available = opensAbove ? above : below;
        if (available == 0) available = Math.Max(1, workingArea.Height);
        int height = Math.Min(Math.Max(1, requested.Height), available);
        int x = Math.Clamp(anchor.Left, workingArea.Left, Math.Max(workingArea.Left, workingArea.Right - width));
        int y = opensAbove ? anchor.Top - height : anchor.Bottom;
        y = Math.Clamp(y, workingArea.Top, Math.Max(workingArea.Top, workingArea.Bottom - height));
        return new Rectangle(x, y, width, height);
    }
}
