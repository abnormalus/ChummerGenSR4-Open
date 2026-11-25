using System;
using System.Linq;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class EditorRowStack
{
    internal static void Attach<TRow>(Forms.Form owner, Forms.Panel panel) where TRow : Forms.Control
    {
        panel.Anchor |= Forms.AnchorStyles.Right;
        foreach (var row in panel.Controls.OfType<TRow>())
            row.SizeChanged += RowResized;

        panel.ControlAdded += Added;
        panel.ControlRemoved += Removed;
        owner.Disposed += (_, _) =>
        {
            panel.ControlAdded -= Added;
            panel.ControlRemoved -= Removed;
            foreach (var row in panel.Controls.OfType<TRow>())
                row.SizeChanged -= RowResized;
        };
        Arrange();

        void Added(object sender, Forms.ControlEventArgs args)
        {
            if (args.Control is TRow row)
                row.SizeChanged += RowResized;
            Arrange();
        }

        void Removed(object sender, Forms.ControlEventArgs args)
        {
            if (args.Control is TRow row)
                row.SizeChanged -= RowResized;
            Arrange();
        }

        void RowResized(object sender, EventArgs args) => Arrange();

        void Arrange()
        {
            int top = 0;
            foreach (var row in panel.Controls.OfType<TRow>())
            {
                row.Top = top;
                top += row.Height;
            }
        }
    }
}
