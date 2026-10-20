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
                treQualities.Height = Math.Max(80, height - treQualities.Top - 48);
                lblQualityBPLabel.Top = lblQualityBP.Top = height - 40;
                lblQualitySourceLabel.Left = 8;
                lblQualitySource.Left = lblQualitySourceLabel.Right + 6;
                lblQualitySourceLabel.Top = lblQualitySource.Top = height - 20;
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
                foreach (var label in new Forms.Control[] { lblRatingLabel, lblSystemLabel, lblFirewallLabel, lblResponseLabel, lblSignalLabel })
                    label.Left = right;
                foreach (var value in new Forms.Control[] { lblRating, lblSystem, lblFirewall, nudResponse, nudSignal })
                    value.Left = right + 90;
                int contactTop = lblRatingLabel.Visible ? 238 : 104;
                splitContacts.Bounds = new Rectangle(right, contactTop, Math.Max(180, width - right - 6), Math.Max(120, height - contactTop - 8));
            }
            finally { arranging = false; }
        }
        tabCommon.SizeChanged += Arrange;
        Disposed += (_, _) => tabCommon.SizeChanged -= Arrange;
        Arrange(this, EventArgs.Empty);
    }
}
