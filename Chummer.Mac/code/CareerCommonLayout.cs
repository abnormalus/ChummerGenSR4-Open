#nullable enable
using System;
using System.Drawing;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private Rectangle CareerMagicBounds()
    {
        int top = lblRatingLabel.Visible
            ? CommonAiAttributes.Bounds(this, tabCommon, lblRatingLabel).Bottom
            : CommonMetatypeSummary.Bounds(this, tabCommon, lblMetatypeLabel).Bottom;
        int left = lblMysticAdeptAssignment.Left;
        return CommonMagicAllocation.Bounds(this, tabCommon, left, top + 6);
    }

    private void InitializeCareerCommonLayout()
    {
        var workspace = MdiParent ?? this;
        var attributesHeight = panAttributes.Height;
        var matrixTop = lblRatingLabel.Top;
        int preferredQualityWidth = treQualities.Width;
        int preferredAttributeWidth = panAttributes.Width;
        int preferredAttributeLeft = panAttributes.Left;
        int preferredAliasLeft = lblAlias.Left;
        int preferredSidebarLeft = lblMetatypeLabel.Left;
        int matrixLeft = lblRatingLabel.Left;
        int magicLeft = lblMysticAdeptAssignment.Left;
        const int gap = 6;
        const int compactSplitHeight = 150;
        const int minimumAttributesHeight = 80;
        splitContacts.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
        InitializeCareerAttributesLayout();
        bool queued = false;
        void Schedule(object? sender, EventArgs args)
        {
            if (queued)
                return;
            queued = true;
            Dispatcher.UIThread.Post(() =>
            {
                queued = false;
                if (IsDisposed || !tabCommon.Visible)
                    return;

                var available = InventoryTreeLayout.VisiblePageSize(workspace, tabCommon);
                const int minimumSidebarWidth = 145;
                const int minimumQualityWidth = 145;
                const int minimumAttributeWidth = 245;
                int missingSidebarWidth = Math.Max(0, minimumSidebarWidth + preferredSidebarLeft + gap - available.Width);
                int qualityReduction = Math.Min(missingSidebarWidth, Math.Max(0, preferredQualityWidth - minimumQualityWidth));
                int attributeReduction = Math.Min(missingSidebarWidth - qualityReduction,
                    Math.Max(0, preferredAttributeWidth - minimumAttributeWidth));
                int sidebarShift = qualityReduction + attributeReduction;

                treQualities.Width = preferredQualityWidth - qualityReduction;
                panAttributes.Left = preferredAttributeLeft - qualityReduction;
                panAttributes.Width = preferredAttributeWidth - attributeReduction;
                lblAlias.Left = preferredAliasLeft - qualityReduction;
                lblMetatypeLabel.Left = preferredSidebarLeft - sidebarShift;
                lblRatingLabel.Left = matrixLeft - sidebarShift;
                lblMysticAdeptAssignment.Left = magicLeft - sidebarShift;

                int detailsBottom = CommonMetatypeSummary.Bounds(this, tabCommon, lblMetatypeLabel).Bottom;
                if (lblRatingLabel.Visible)
                {
                    lblRatingLabel.Top = Math.Max(matrixTop, detailsBottom + gap);
                    detailsBottom = CommonAiAttributes.Bounds(this, tabCommon, lblRatingLabel).Bottom;
                }
                if (nudMysticAdeptMAGMagician.Visible)
                    detailsBottom = Math.Max(detailsBottom, CareerMagicBounds().Bottom);
                int contentBottom = Math.Max(detailsBottom, panAttributes.Top + minimumAttributesHeight);
                int preferredTop = panAttributes.Top + attributesHeight + gap;
                int splitTop = Math.Max(contentBottom + gap,
                    Math.Min(preferredTop, available.Height - compactSplitHeight - gap));
                panAttributes.Height = Math.Min(attributesHeight, splitTop - gap - panAttributes.Top);
                splitContacts.Bounds = new Rectangle(panAttributes.Left, splitTop,
                    Math.Max(0, available.Width - panAttributes.Left - gap),
                    Math.Max(0, available.Height - splitTop - gap));
            }, DispatcherPriority.Loaded);
        }

        tabCommon.SizeChanged += Schedule;
        tabCommon.VisibleChanged += Schedule;
        workspace.SizeChanged += Schedule;
        lblRatingLabel.VisibleChanged += Schedule;
        nudMysticAdeptMAGMagician.VisibleChanged += Schedule;
        Disposed += (_, _) =>
        {
            tabCommon.SizeChanged -= Schedule;
            tabCommon.VisibleChanged -= Schedule;
            workspace.SizeChanged -= Schedule;
            lblRatingLabel.VisibleChanged -= Schedule;
            nudMysticAdeptMAGMagician.VisibleChanged -= Schedule;
        };
        Schedule(null, EventArgs.Empty);
    }
}
