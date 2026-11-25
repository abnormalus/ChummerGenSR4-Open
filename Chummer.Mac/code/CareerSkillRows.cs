#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCareer
{
    private void AttachCareerSkillRows(Forms.Panel source, string name)
    {
        var workspace = MdiParent ?? this;
        var rows = new StackPanel { Spacing = 3 };
        var scroll = new ScrollViewer { Content = rows, HorizontalScrollBarVisibility = Avalonia.Controls.Primitives.ScrollBarVisibility.Disabled };
        NativeControlPresentation.AddTheme(scroll.Styles);
        var presenter = new CreationRegionPresenter(this, source, scroll, name,
            () => new System.Drawing.Rectangle(System.Drawing.Point.Empty, InventoryTreeLayout.VisiblePageSize(workspace, source)),
            clipToSource: true);
        var mounted = new Dictionary<SkillControl, Control>();
        bool scheduled = false;
        void Schedule(object? sender, EventArgs args)
        {
            if (scheduled)
                return;
            scheduled = true;
            Dispatcher.UIThread.Post(() =>
            {
                scheduled = false;
                if (IsDisposed || !source.Visible)
                    return;

                int index = 0;
                foreach (var skill in source.Controls.OfType<SkillControl>())
                {
                    if (!mounted.TryGetValue(skill, out var row) && skill.Visible)
                    {
                        skill.CreateControl();
                        row = skill.CreateCareerSkillRow(this);
                        mounted.Add(skill, row);
                        rows.Children.Insert(index, row);
                    }
                    if (row != null)
                    {
                        row.IsVisible = skill.Visible;
                        index++;
                    }
                }
            }, DispatcherPriority.Loaded);
        }
        void Added(object? sender, Forms.ControlEventArgs args)
        {
            if (args.Control is SkillControl skill)
                skill.VisibleChanged += Schedule;
            Schedule(sender, args);
        }
        void Removed(object? sender, Forms.ControlEventArgs args)
        {
            if (args.Control is not SkillControl skill)
                return;
            skill.VisibleChanged -= Schedule;
            if (mounted.Remove(skill, out var row))
                rows.Children.Remove(row);
        }
        foreach (var skill in source.Controls.OfType<SkillControl>())
            skill.VisibleChanged += Schedule;
        source.AutoScroll = false;
        source.VisibleChanged += Schedule;
        source.ControlAdded += Added;
        source.ControlRemoved += Removed;
        Disposed += (_, _) =>
        {
            source.VisibleChanged -= Schedule;
            source.ControlAdded -= Added;
            source.ControlRemoved -= Removed;
            foreach (var skill in source.Controls.OfType<SkillControl>())
                skill.VisibleChanged -= Schedule;
            presenter.Dispose();
        };
        Schedule(null, EventArgs.Empty);
    }
}

public partial class SkillControl
{
    private StackPanel? careerNativeRow;

    internal Control CreateCareerSkillRow(frmCareer editor)
    {
        var details = new WrapPanel { ItemSpacing = 8, LineSpacing = 3 };
        var commands = new WrapPanel { ItemSpacing = 4, LineSpacing = 3 };
        careerNativeRow = new StackPanel { Spacing = 3, Margin = new Thickness(2), Tag = this };
        if (KnowledgeSkill)
        {
            careerNativeRow.Children.Add(Choice(cboKnowledgeSkillCategory, "Knowledge skill type"));
            careerNativeRow.Children.Add(Choice(cboSkillName, "Knowledge skill name", knowledgeSearch));
        }
        else
            careerNativeRow.Children.Add(Readout(lblSkillName));
        details.Children.Add(Readout(lblAttribute));
        details.Children.Add(Readout(lblSkillRating));
        details.Children.Add(Readout(lblModifiedRating));
        careerNativeRow.Children.Add(details);
        commands.Children.Add(Command(cmdImproveSkill, "+", "NativeCareerImproveSkill"));
        commands.Children.Add(Command(cmdRoll, LanguageManager.Instance.GetString("Tip_DiceRoller"), "NativeCareerRollSkill"));
        commands.Children.Add(Command(cmdBreakGroup, LanguageManager.Instance.GetString("String_Break"), "NativeCareerBreakGroup"));
        commands.Children.Add(Command(cmdDelete, LanguageManager.Instance.GetString("String_Delete"), "NativeCareerDeleteSkill"));
        commands.Children.Add(Command(KnowledgeSkill ? addKnowledgeSpecialty : cmdChangeSpec, "+", "NativeCareerSpecialization"));
        careerNativeRow.Children.Add(commands);
        NativeControlPresentation.ConstrainWrapItems(details);
        NativeControlPresentation.ConstrainWrapItems(commands);
        if (careerSpecializationExpanded)
            MountCareerSpecialization(editor);
        return careerNativeRow;

        ComboBox Choice(Forms.ComboBox source, string caption, SearchableChoices? search = null)
        {
            Controls.Remove(source);
            var combo = NativeListSelection.Attach(editor, source, caption, search: search, row: this, layoutManaged: true)!;
            Disposed += (_, _) => source.Dispose();
            return combo;
        }

        TextBlock Readout(Forms.Label source)
        {
            Controls.Remove(source);
            var text = new TextBlock { TextWrapping = TextWrapping.Wrap };
            void Refresh(object? sender, EventArgs args)
            {
                text.Text = source.Text;
                text.IsVisible = source.Visible;
                ToolTip.SetTip(text, tipTooltip.GetToolTip(source));
            }
            source.TextChanged += Refresh;
            source.VisibleChanged += Refresh;
            Refresh(null, EventArgs.Empty);
            Disposed += (_, _) =>
            {
                source.TextChanged -= Refresh;
                source.VisibleChanged -= Refresh;
                source.Dispose();
            };
            return text;
        }

        Button Command(Forms.Button source, string caption, string name)
        {
            Controls.Remove(source);
            var binding = new NativeInlineCommandBinding(source, tipTooltip, () => !IsDisposed && Enabled,
                () =>
                {
                    if (Visible)
                        source.PerformClick();
                }, () => caption);
            binding.Input.Name = name;
            binding.Input.ClearValue(Button.FontSizeProperty);
            binding.Input.ContentTemplate = new Avalonia.Controls.Templates.FuncDataTemplate<string>((text, _) =>
                new TextBlock { Text = text, TextWrapping = TextWrapping.Wrap });
            Avalonia.Automation.AutomationProperties.SetName(binding.Input, tipTooltip.GetToolTip(source));
            binding.Input.PointerEntered += (_, _) => binding.Refresh();
            Disposed += (_, _) =>
            {
                binding.Dispose();
                source.Dispose();
            };
            return binding.Input;
        }
    }
}
