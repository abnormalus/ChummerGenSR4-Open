#nullable enable
using System;
using System.Drawing;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private CreationContactSection? creationContactSection, creationEnemySection;
    internal CreationContactSection? ContactPresentation(bool enemy) => enemy ? creationEnemySection : creationContactSection;
    internal void InitializeCreationContactPresentation(bool attachPresenter = true)
    {
        if ((attachPresenter && Avalonia.Application.Current == null) || creationContactSection != null || IsDisposed) return;
        creationContactSection = new CreationContactSection(this, panContacts, creationContactControllers, "nativeCreationContacts", attachPresenter);
        creationEnemySection = new CreationContactSection(this, panEnemies, creationEnemyControllers, "nativeCreationEnemies", attachPresenter);
        if (attachPresenter)
            AttachCreationContactLayout();
    }
    private void AttachCreationContactLayout()
    {
        var workspace = MdiParent ?? this;
        int preferredAttributesHeight = panAttributes.Height;
        panAttributes.Anchor &= ~Forms.AnchorStyles.Bottom;
        splitContacts.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
        splitContacts.Panel1.Resize -= splitContacts_Panel1_Resize;
        splitContacts.Panel2.Resize -= splitContacts_Panel2_Resize;
        AdeptPowerHeader.AttachMeasured(this, splitContacts.Panel1, panContacts, "NativeCreationContactsHeader",
            TechnomancerPersonaLayout.BindLabel(this, splitContacts.Panel1, lblContacts), cmdAddContact, true, lblContacts);
        AdeptPowerHeader.AttachMeasured(this, splitContacts.Panel2, panEnemies, "NativeCreationEnemiesHeader",
            TechnomancerPersonaLayout.BindLabel(this, splitContacts.Panel2, lblEnemies), cmdAddEnemy, true, lblEnemies);

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
                const int gap = 6;
                const int contactHeight = 150;
                const int minimumAttributesHeight = 80;
                int left = panAttributes.Left;
                int top = panAttributes.Top + preferredAttributesHeight + gap;
                int sidebarLeft = Math.Max(panAttributes.Right + gap, lblMetatypeLabel.Left);
                int sidebarBottom = CreationMagicBounds().Top;
                if (nudMysticAdeptMAGMagician.Visible)
                    sidebarBottom = CreationMagicBounds().Bottom + gap;
                if (available.Width - sidebarLeft - gap >= CommonSidebarMinimumWidth)
                {
                    left = sidebarLeft;
                    top = sidebarBottom;
                }
                else
                {
                    top = Math.Max(Math.Max(sidebarBottom, panAttributes.Top + minimumAttributesHeight + gap),
                        Math.Min(top, available.Height - contactHeight - gap));
                }

                int attributesHeight = left == sidebarLeft ? preferredAttributesHeight : top - gap - panAttributes.Top;
                panAttributes.Height = Math.Clamp(attributesHeight, 0,
                    Math.Max(0, Math.Min(preferredAttributesHeight, available.Height - panAttributes.Top - gap)));

                splitContacts.Bounds = new Rectangle(left, top,
                    Math.Max(0, available.Width - left - gap), Math.Max(0, available.Height - top - gap));
            }, DispatcherPriority.Loaded);
        }

        tabCommon.SizeChanged += Schedule;
        tabCommon.VisibleChanged += Schedule;
        workspace.SizeChanged += Schedule;
        panAttributes.LocationChanged += Schedule;
        lblRatingLabel.VisibleChanged += Schedule;
        lblRatingLabel.LocationChanged += Schedule;
        nudMysticAdeptMAGMagician.VisibleChanged += Schedule;
        Disposed += (_, _) =>
        {
            tabCommon.SizeChanged -= Schedule;
            tabCommon.VisibleChanged -= Schedule;
            workspace.SizeChanged -= Schedule;
            panAttributes.LocationChanged -= Schedule;
            lblRatingLabel.VisibleChanged -= Schedule;
            lblRatingLabel.LocationChanged -= Schedule;
            nudMysticAdeptMAGMagician.VisibleChanged -= Schedule;
        };
        Schedule(null, EventArgs.Empty);
    }
    private void DisposeCreationContactPresentation()
    {
        creationContactSection?.Dispose(); creationEnemySection?.Dispose();
    }
}
