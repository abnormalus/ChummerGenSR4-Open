#nullable enable
using System;
using System.Drawing;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private const int CommonSidebarMinimumWidth = 145;

    private int creationMoneyHeight = 40;

    private void UpdateCreationMoneyHeight(int height)
    {
        creationMoneyHeight = height;
        lblRatingLabel.Top = lblNuyen.Top + creationMoneyHeight + 7;
    }

    private Rectangle CreationMagicBounds()
    {
        int top = lblRatingLabel.Visible
            ? CommonAiAttributes.Bounds(this, tabCommon, lblRatingLabel).Bottom + 5
            : lblNuyen.Top + creationMoneyHeight + 4;
        return CommonMagicAllocation.Bounds(this, tabCommon, lblRatingLabel.Left, top);
    }

    private void InitializeCreationCommonLayout()
    {
        var workspace = MdiParent ?? this;
        bool arranging = false;
        void Arrange(object? sender, EventArgs args)
        {
            if (arranging || IsDisposed || (creationContactSection != null && !tabCommon.Visible)) return;
            arranging = true;
            try
            {
                int width = tabCommon.ClientSize.Width, height = tabCommon.ClientSize.Height;
                const int qualityGap = 14;
                const int attributeMinimumWidth = 324;
                const int sidebarGap = 12;
                int qualityWidth = Math.Clamp(width / 4, 180, 340);
                int visibleWidth = InventoryTreeLayout.VisiblePageSize(workspace, tabCommon).Width;
                bool compact = creationContactSection != null && visibleWidth < 700;
                if (compact)
                    qualityWidth = Math.Clamp(visibleWidth - qualityGap - attributeMinimumWidth - sidebarGap -
                        CommonSidebarMinimumWidth - 6, 145, qualityWidth);
                int attributeLeft = qualityWidth + qualityGap;
                int attributeWidth = compact ? attributeMinimumWidth : Math.Clamp((width - attributeLeft) / 2, attributeMinimumWidth, 440);
                int right = attributeLeft + attributeWidth + sidebarGap;
                treQualities.Width = qualityWidth;
                treQualities.Height = Math.Max(80, height - treQualities.Top - 48);
                lblQualityBPLabel.Top = lblQualityBP.Top = height - 40;
                lblQualitySourceLabel.Left = 8;
                lblQualitySource.Left = lblQualitySourceLabel.Right + 6;
                lblQualitySourceLabel.Top = lblQualitySource.Top = height - 20;
                panAttributes.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
                panAttributes.Bounds = new Rectangle(attributeLeft, 48, attributeWidth,
                    creationContactSection == null ? Math.Max(340, height - 56) : panAttributes.Height);
                panAttributes.AutoScroll = false;
                lblAttributes.Left = attributeLeft;
                lblAlias.Left = attributeLeft;
                txtAlias.Left = attributeLeft + 42;
                lblMetatypeLabel.Left = right;
                lblMetatype.Left = right + lblMetatypeLabel.Width + 6;
                lblMetatypeSourceLabel.Left = right;
                lblMetatypeSource.Left = right + lblMetatypeSourceLabel.Width + 6;
                lblNuyen.Left = right;
                lblNuyen.Top = CommonMetatypeSummary.Bounds(this, tabCommon, lblMetatypeLabel).Bottom;
                lblRatingLabel.Top = lblNuyen.Top + creationMoneyHeight + 7;
                foreach (var label in new Forms.Control[] { lblRatingLabel, lblSystemLabel, lblFirewallLabel, lblResponseLabel, lblSignalLabel })
                    label.Left = right;
                foreach (var value in new Forms.Control[] { lblRating, lblSystem, lblFirewall, nudResponse, nudSignal })
                    value.Left = right + 90;
                if (creationContactSection == null)
                {
                    splitContacts.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
                    int contactTop = CreationMagicBounds().Top;
                    if (nudMysticAdeptMAGMagician.Visible && Avalonia.Application.Current != null)
                        contactTop += CreationMagicBounds().Height + 6;
                    splitContacts.Bounds = new Rectangle(right, contactTop, Math.Max(180, width - right - 6), Math.Max(120, height - contactTop - 8));
                    splitContacts.SplitterDistance = splitContacts.Height / 2;
                }
            }
            finally { arranging = false; }
        }
        workspace.SizeChanged += Arrange;
        tabCommon.VisibleChanged += Arrange;
        tabCommon.SizeChanged += Arrange;
        nudMysticAdeptMAGMagician.VisibleChanged += Arrange;
        Disposed += (_, _) =>
        {
            workspace.SizeChanged -= Arrange;
            tabCommon.VisibleChanged -= Arrange;
            tabCommon.SizeChanged -= Arrange;
            nudMysticAdeptMAGMagician.VisibleChanged -= Arrange;
        };
        Arrange(this, EventArgs.Empty);
        InitializeCreationContactLayout();
    }
}
