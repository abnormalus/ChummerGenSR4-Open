#nullable enable
using System;
using System.Drawing;
using Avalonia.Controls;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private void InitializeCareerSkillHeader()
    {
        if (Avalonia.Application.Current == null)
            return;

        var knowledgePane = splitSkills.Panel2;
        knowledgePane.Resize -= splitSkills_Panel2_Resize;
        AdeptPowerHeader.AttachMeasured(this, knowledgePane, panKnowledgeSkills, "NativeCareerKnowledgeHeader",
            TechnomancerPersonaLayout.BindLabel(this, knowledgePane, lblKnowledgeSkills), cmdAddKnowledgeSkill,
            inline: false, listReserve: 36, lblKnowledgeSkills);

        AttachCareerSkillRows(panActiveSkills, "NativeCareerActiveSkillRows");
        AttachCareerSkillRows(panKnowledgeSkills, "NativeCareerKnowledgeSkillRows");
        var pane = splitSkills.Panel1;
        var workspace = MdiParent ?? this;
        var heading = TechnomancerPersonaLayout.BindLabel(this, pane, lblActiveSkills);
        var add = ArmorEquipmentLayout.BindCommand(this, pane, cmdAddExoticSkill);
        pane.Controls.Remove(cboSkillFilter);
        var filter = NativeListSelection.Attach(this, cboSkillFilter, "Skill filter", layoutManaged: true)!;
        var row = new WrapPanel { ClipToBounds = true, ItemSpacing = 6, LineSpacing = 4, Children = { heading, filter, add } };
        NativeControlPresentation.AddTheme(row.Styles);
        int headerHeight = 32;
        int HeaderWidth() => Math.Max(0, InventoryTreeLayout.VisiblePageSize(workspace, pane).Width - panActiveSkills.Left - 6);
        var presenter = new CreationRegionPresenter(this, pane, row, "NativeCareerSkillHeader",
            () => new Rectangle(panActiveSkills.Left, 2, HeaderWidth(), headerHeight), clipToSource: true);
        panActiveSkills.Anchor = Forms.AnchorStyles.Top | Forms.AnchorStyles.Left;
        bool queued = false;
        void Schedule(object? sender, EventArgs args)
        {
            if (queued)
                return;
            queued = true;
            Dispatcher.UIThread.Post(() =>
            {
                queued = false;
                if (IsDisposed || !pane.Visible)
                    return;

                var available = InventoryTreeLayout.VisiblePageSize(workspace, pane);
                int width = HeaderWidth();
                filter.MaxWidth = Math.Min(width, 250);
                row.ClearValue(Avalonia.Layout.Layoutable.WidthProperty);
                row.ClearValue(Avalonia.Layout.Layoutable.HeightProperty);
                row.InvalidateMeasure();
                row.Measure(new Avalonia.Size(width, double.PositiveInfinity));
                headerHeight = Math.Clamp((int)Math.Ceiling(row.DesiredSize.Height), 0, Math.Max(0, available.Height - 46));
                int top = headerHeight + 6;
                panActiveSkills.Bounds = new Rectangle(panActiveSkills.Left, top, width, Math.Max(0, available.Height - top));
                presenter.Refresh();
            }, DispatcherPriority.Loaded);
        }

        void TypographyChanged(object? sender, Avalonia.AvaloniaPropertyChangedEventArgs args)
        {
            if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty ||
                args.Property == Avalonia.Controls.Documents.TextElement.FontFamilyProperty)
                Schedule(sender, EventArgs.Empty);
        }

        row.PropertyChanged += TypographyChanged;
        pane.SizeChanged += Schedule;
        pane.VisibleChanged += Schedule;
        workspace.SizeChanged += Schedule;
        lblActiveSkills.TextChanged += Schedule;
        cmdAddExoticSkill.TextChanged += Schedule;
        Disposed += (_, _) =>
        {
            row.PropertyChanged -= TypographyChanged;
            pane.SizeChanged -= Schedule;
            pane.VisibleChanged -= Schedule;
            workspace.SizeChanged -= Schedule;
            lblActiveSkills.TextChanged -= Schedule;
            cmdAddExoticSkill.TextChanged -= Schedule;
            presenter.Dispose();
            cboSkillFilter.Dispose();
        };
        Schedule(null, EventArgs.Empty);
    }
}
