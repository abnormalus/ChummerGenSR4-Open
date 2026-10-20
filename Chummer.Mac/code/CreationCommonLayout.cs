#nullable enable
using System;
using System.Drawing;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private void InitializeCreationCommonLayout()
    {
        bool arranging = false;
        void Arrange(object? sender, EventArgs args)
        {
            if (arranging || IsDisposed) return;
            arranging = true;
            try
            {
                int width = tabCommon.ClientSize.Width, height = tabCommon.ClientSize.Height;
                int qualityWidth = Math.Clamp(width / 4, 180, 340);
                int attributeLeft = qualityWidth + 14;
                int attributeWidth = Math.Clamp((width - attributeLeft) / 2, 324, 440);
                int right = attributeLeft + attributeWidth + 12;
                treQualities.Width = qualityWidth;
                panAttributes.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
                panAttributes.Bounds = new Rectangle(attributeLeft, 48, attributeWidth, Math.Max(340, height - 56));
                panAttributes.AutoScroll = false;
                lblAttributes.Left = attributeLeft;
                lblAlias.Left = attributeLeft;
                txtAlias.Left = attributeLeft + 42;
                lblMetatypeLabel.Left = right;
                lblMetatype.Left = right + lblMetatypeLabel.Width + 6;
                lblMetatypeSourceLabel.Left = right;
                lblMetatypeSource.Left = right + lblMetatypeSourceLabel.Width + 6;
                lblNuyen.Left = right; lblNuyen.Top = 60;
                splitContacts.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
                splitContacts.Bounds = new Rectangle(right, 104, Math.Max(180, width - right - 6), Math.Max(120, height - 112));
            }
            finally { arranging = false; }
        }
        tabCommon.SizeChanged += Arrange;
        Disposed += (_, _) => tabCommon.SizeChanged -= Arrange;
        Arrange(this, EventArgs.Empty);
    }
}
