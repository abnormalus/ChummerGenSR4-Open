#nullable enable
using System;
using System.Linq;
using System.Drawing;
using System.Collections.Generic;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class ContactControl
{
    private bool creationCompactContact;
    private Dictionary<Forms.Control, Rectangle>? creationContactOriginalBounds;
    internal void ArrangeForCreationColumn(int width)
    {
        creationCompactContact = true;
        creationContactOriginalBounds ??= Controls.Cast<Forms.Control>().ToDictionary(child => child, child => child.Bounds);
        Width = width;
        if (width >= 536)
        {
            int extra = width - 533;
            foreach (var pair in creationContactOriginalBounds)
            {
                var bounds = pair.Value;
                if (ReferenceEquals(pair.Key, txtContactName)) bounds.Width += extra;
                else bounds.X += extra;
                pair.Key.Bounds = bounds;
            }
            Height = 26;
            return;
        }
        txtContactName.Bounds = new Rectangle(2, 2, Math.Max(80, width - 50), 22);
        imgLink.Location = new Point(width - 44, 5);
        imgNotes.Location = new Point(width - 22, 5);
        lblConnection.Location = new Point(2, 30);
        nudConnection.Location = new Point(Math.Max(84, lblConnection.Right + 4), 26);
        lblLoyalty.Location = new Point(2, 54);
        nudLoyalty.Location = new Point(Math.Max(84, lblLoyalty.Right + 4), 50);
        lblGroupLabel.Location = new Point(2, 78);
        lblGroup.Location = new Point(lblGroupLabel.Right + 4, 78);
        cmdGroup.Location = new Point(lblGroup.Right + 4, 74);
        cmdDelete.Location = new Point(width - cmdDelete.Width - 2, 74);
        if (cmdGroup.Right + 4 > cmdDelete.Left) cmdDelete.Top += 26;
        Height = cmdDelete.Bottom + 3;
    }
}

public partial class frmCreate
{
    private void InitializeCreationContactLayout()
    {
        foreach (var panel in new[] { panContacts, panEnemies })
        {
            bool arranging = false;
            void Arrange()
            {
                if (arranging || IsDisposed) return;
                arranging = true;
                try
                {
                    int top = panel.AutoScrollPosition.Y;
                    foreach (var row in panel.Controls.OfType<ContactControl>())
                    {
                        row.CreateControl();
                        row.ArrangeForCreationColumn(Math.Max(180, panel.ClientSize.Width - 18));
                        row.Location = new Point(0, top);
                        top += row.Height + 4;
                    }
                }
                finally { arranging = false; }
            }
            EventHandler resized = (_, _) => Arrange();
            Forms.ControlEventHandler children = (_, _) => Arrange();
            panel.SizeChanged += resized; panel.ControlAdded += children; panel.ControlRemoved += children;
            Disposed += (_, _) => { panel.SizeChanged -= resized; panel.ControlAdded -= children; panel.ControlRemoved -= children; };
            Arrange();
        }
    }
}
