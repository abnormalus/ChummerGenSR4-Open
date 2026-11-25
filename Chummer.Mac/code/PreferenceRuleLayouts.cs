#nullable enable
using System;
using System.Linq;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmOptions
{
    private void InitializeRuleLayouts()
    {
        if (Avalonia.Application.Current == null)
            return;

        AttachRuleLayout(tabHouseRules, "NativeHouseRules",
            (chkFreeKarmaContacts, nudFreeKarmaContactsMultiplier, null),
            (chkFreeContactsFlat, nudFreeContactsFlatNumber, null),
            (chkMultiplyRestrictedCost, nudRestrictedCostMultiplier, null),
            (chkMultiplyForbiddenCost, nudForbiddenCostMultiplier, null));
        AttachRuleLayout(tabOptionalRules, "NativeOptionalRules",
            (chkMetatypeCostsKarma, nudMetatypeCostsKarmaMultiplier, lblMetatypeCostsKarma));
        AttachRuleLayout(tabPage1, "NativeMiscellaneous");
        AttachRuleLayout(tabPage2, "NativeCharacterAndPrinting",
            (cboBuildMethod, nudBP, null),
            (lblMaxAvail, nudMaxAvail, null),
            (lblNuyenPerBP, nudNuyenPerBP, null),
            (lblLimbCount, cboLimbCount, null),
            (lblEssenceDecimals, cboEssenceDecimals, null));
        AttachRuleLayout(tabGlobal, "NativeGlobalPreferences",
            (lblLanguage, cboLanguage, null),
            (lblXSLT, cboXSLT, null),
            (lblPDFArgumentStyle, cboPDFArgumentStyle, null),
            (lblPDFAppPath, txtPDFAppPath, null),
            (lblCharacterAutosaveMinutes, nudCharacterAutosaveMinutes, null));
        AttachRuleLayout(tabBPCosts, "NativeBPCosts",
            (lblBPAttribute, nudBPAttribute, null),
            (lblBPAttributeMax, nudBPAttributeMax, null),
            (lblBPContact, nudBPContact, null),
            (lblBPMartialArt, nudBPMartialArt, null),
            (lblBPMartialArtManeuver, nudBPMartialArtManeuver, null),
            (lblBPSkillGroup, nudBPSkillGroup, null),
            (lblBPActiveSkill, nudBPActiveSkill, null),
            (lblBPSkillSpecialization, nudBPSkillSpecialization, null),
            (lblBPKnowledgeSkill, nudBPKnowledgeSkill, null),
            (lblBPSpell, nudBPSpell, null),
            (lblBPFocus, nudBPFocus, null),
            (lblBPSpirit, nudBPSpirit, null),
            (lblBPComplexForm, nudBPComplexForm, null),
            (lblBPComplexFormOption, nudBPComplexFormOption, null));
        AttachKarmaCostsLayout(
            [
                (lblKarmaSpecialization, nudKarmaSpecialization, null),
                (lblKarmaNewKnowledgeSkill, nudKarmaNewKnowledgeSkill, null),
                (lblKarmaNewActiveSkill, nudKarmaNewActiveSkill, null),
                (lblKarmaNewSkillGroup, nudKarmaNewSkillGroup, null),
                (lblKarmaImproveKnowledgeSkill, nudKarmaImproveKnowledgeSkill, lblKarmaImproveKnowledgeSkillExtra),
                (lblKarmaImproveActiveSkill, nudKarmaImproveActiveSkill, lblKarmaImproveActiveSkillExtra),
                (lblKarmaImproveSkillGroup, nudKarmaImproveSkillGroup, lblKarmaImproveSkillGroupExtra),
                (lblKarmaAttribute, nudKarmaAttribute, lblKarmaAttributeExtra),
                (lblKarmaQuality, nudKarmaQuality, lblKarmaQualityExtra),
                (lblKarmaSpell, nudKarmaSpell, null),
                (lblKarmaNewComplexForm, nudKarmaNewComplexForm, null),
                (lblKarmaImproveComplexForm, nudKarmaImproveComplexForm, lblKarmaImproveComplexFormExtra),
                (lblKarmaComplexFormOption, nudKarmaComplexFormOption, lblKarmaComplexFormOptionExtra),
                (lblKarmaComplexFormSkillsoft, nudKarmaComplexFormSkillsoft, lblKarmaComplexFormSkillsoftExtra),
                (lblKarmaSpirit, nudKarmaSpirit, lblKarmaSpiritExtra),
                (lblKarmaManeuver, nudKarmaManeuver, null),
                (lblKarmaNuyenPer, nudKarmaNuyenPer, lblKarmaNuyenPerExtra),
                (lblKarmaContact, nudKarmaContact, lblKarmaContactExtra),
                (lblKarmaCarryover, nudKarmaCarryover, lblKarmaCarryoverExtra),
                (lblKarmaInitiation, nudKarmaInitiation, lblKarmaInitiationExtra)
            ],
            [
                (lblKarmaMetamagic, nudKarmaMetamagic, null),
                (lblKarmaJoinGroup, nudKarmaJoinGroup, null),
                (lblKarmaLeaveGroup, nudKarmaLeaveGroup, null),
                (lblKarmaAnchoringFocus, nudKarmaAnchoringFocus, lblKarmaAnchoringFocusExtra),
                (lblKarmaBanishingFocus, nudKarmaBanishingFocus, lblKarmaBanishingFocusExtra),
                (lblKarmaBindingFocus, nudKarmaBindingFocus, lblKarmaBindingFocusExtra),
                (lblKarmaCenteringFocus, nudKarmaCenteringFocus, lblKarmaCenteringFocusExtra),
                (lblKarmaCounterspellingFocus, nudKarmaCounterspellingFocus, lblKarmaCounterspellingFocusExtra),
                (lblKarmaDiviningFocus, nudKarmaDiviningFocus, lblKarmaDiviningFocusExtra),
                (lblKarmaDowsingFocus, nudKarmaDowsingFocus, lblKarmaDowsingFocusExtra),
                (lblKarmaInfusionFocus, nudKarmaInfusionFocus, lblKarmaInfusionFocusExtra),
                (lblKarmaMaskingFocus, nudKarmaMaskingFocus, lblKarmaMaskingFocusExtra),
                (lblKarmaPowerFocus, nudKarmaPowerFocus, lblKarmaPowerFocusExtra),
                (lblKarmaShieldingFocus, nudKarmaShieldingFocus, lblKarmaShieldingFocusExtra),
                (lblKarmaSpellcastingFocus, nudKarmaSpellcastingFocus, lblKarmaSpellcastingFocusExtra),
                (lblKarmaSummoningFocus, nudKarmaSummoningFocus, lblKarmaSummoningFocusExtra),
                (lblKarmaSustainingFocus, nudKarmaSustainingFocus, lblKarmaSustainingFocusExtra),
                (lblKarmaSymbolicLinkFocus, nudKarmaSymbolicLinkFocus, lblKarmaSymbolicLinkFocusExtra),
                (lblKarmaWeaponFocus, nudKarmaWeaponFocus, lblKarmaWeaponFocusExtra)
            ]);
    }

    private void AttachRuleLayout(Forms.TabPage page, string name,
        params (Forms.Control Caption, Forms.Control Value, Forms.Label? Suffix)[] pairs) =>
        AttachPreferenceRows(page, name, pairs, []);

    private void AttachKarmaCostsLayout(
        (Forms.Control Caption, Forms.Control Value, Forms.Label? Suffix)[] first,
        (Forms.Control Caption, Forms.Control Value, Forms.Label? Suffix)[] second) =>
        AttachPreferenceRows(tabKarmaCosts, "NativeKarmaCosts", first.Concat(second).ToArray(),
            second.Select(pair => pair.Caption).ToArray());

    private void AttachPreferenceRows(Forms.TabPage page, string name,
        (Forms.Control Caption, Forms.Control Value, Forms.Label? Suffix)[] pairs,
        Forms.Control[] secondColumn, Forms.Control[]? ordered = null, Forms.Button[][]? commandGroups = null)
    {
        var sources = ordered ?? page.Controls.Cast<Forms.Control>().OrderBy(control => control.Top).ToArray();
        var rows = new StackPanel { Spacing = 4 };
        var content = new StackPanel { Spacing = 12, Margin = new Thickness(12) };
        var secondaryRows = new StackPanel { Spacing = 4 };
        var commands = new StackPanel { Spacing = 4 };
        if (secondColumn.Length == 0)
            content.Children.Add(rows);
        else
        {
            var columns = new Grid { RowDefinitions = new RowDefinitions("Auto,Auto"), RowSpacing = 8 };
            Grid.SetRow(secondaryRows, 1);
            columns.Children.Add(rows);
            columns.Children.Add(secondaryRows);
            columns.SizeChanged += (_, _) =>
            {
                bool wide = columns.Bounds.Width >= 700;
                if (wide == (Grid.GetColumn(secondaryRows) == 1))
                    return;

                columns.ColumnDefinitions = new ColumnDefinitions(wide ? "*,*" : "*");
                columns.ColumnSpacing = wide ? 20 : 0;
                Grid.SetColumn(secondaryRows, wide ? 1 : 0);
                Grid.SetRow(secondaryRows, wide ? 0 : 1);
            };
            content.Children.Add(columns);
            content.Children.Add(commands);
        }
        var view = new ScrollViewer
        {
            Content = content,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            AllowAutoHide = false
        };
        NativeControlPresentation.AddTheme(view.Styles);
        page.AutoScroll = false;
        page.AutoScrollMinSize = System.Drawing.Size.Empty;
        foreach (var source in sources)
            page.Controls.Remove(source);

        foreach (var source in sources)
        {
            var pair = pairs.FirstOrDefault(pair => pair.Caption == source);
            if (source is Forms.CheckBox checkbox)
            {
                var caption = new TextBlock { TextWrapping = TextWrapping.Wrap };
                var toggle = new Avalonia.Controls.CheckBox
                {
                    Name = "Native" + checkbox.Name,
                    Content = caption,
                    HorizontalAlignment = HorizontalAlignment.Stretch
                };
                void Refresh()
                {
                    caption.Text = pair.Suffix is { Text.Length: > 0 } suffix
                        ? checkbox.Text + " " + suffix.Text
                        : checkbox.Text;
                    AutomationProperties.SetName(toggle, checkbox.Text);
                    toggle.IsChecked = checkbox.Checked;
                    toggle.IsEnabled = checkbox.Enabled;
                    toggle.IsVisible = checkbox.Visible;
                }
                var observer = new NativeControlObservation(checkbox, Refresh);
                void SourceChanged(object? sender, EventArgs args) => Refresh();
                checkbox.CheckedChanged += SourceChanged;
                checkbox.TextChanged += SourceChanged;
                if (pair.Suffix != null)
                    pair.Suffix.TextChanged += SourceChanged;
                toggle.IsCheckedChanged += (_, _) => checkbox.Checked = toggle.IsChecked == true;
                Refresh();
                Disposed += (_, _) =>
                {
                    observer.Dispose();
                    checkbox.CheckedChanged -= SourceChanged;
                    checkbox.TextChanged -= SourceChanged;
                    if (pair.Suffix != null)
                        pair.Suffix.TextChanged -= SourceChanged;
                };
                AddRow(rows, toggle, checkbox.Text, pair.Value, null);
            }
            else if (source == cboBuildMethod)
            {
                var choice = NativeListSelection.Attach(this, cboBuildMethod, "Character build method", layoutManaged: true)!;
                AddRow(rows, choice, label36.Text, pair.Value, null);
            }
            else if (source is Forms.Label label && (pair.Value != null || label == label36 ||
                label.Name is "nativePdfInformation" or "lblPdfCollectionHelp"))
            {
                var caption = new TextBlock { TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
                void Refresh()
                {
                    caption.Text = label == lblKarmaInitiation ? label.Text + " " + lblKarmaInitiationBracket.Text : label.Text;
                    caption.IsVisible = label.Visible;
                }
                void SourceChanged(object? sender, EventArgs args) => Refresh();
                label.TextChanged += SourceChanged;
                label.VisibleChanged += SourceChanged;
                Refresh();
                AddRow(secondColumn.Contains(label) ? secondaryRows : rows, caption, label.Text, pair.Value, pair.Suffix);
                Disposed += (_, _) =>
                {
                    label.TextChanged -= SourceChanged;
                    label.VisibleChanged -= SourceChanged;
                };
            }
            else if (source is Forms.TextBox { Multiline: true, ReadOnly: true } report)
            {
                rows.Children.Add(CreateTextInput(report, report.AccessibleName ?? report.Name));
            }
            else if (source is Forms.Button command && command != cmdPDFLocation && command != cmdPDFAppPath)
            {
                var group = commandGroups?.FirstOrDefault(group => group.Contains(command));
                if (group == null)
                    (secondColumn.Length == 0 ? rows : commands).Children.Add(CreateCommand(command));
                else if (group[0] == command)
                {
                    var actions = new WrapPanel { ItemSpacing = 6, LineSpacing = 4 };
                    foreach (var member in group)
                        actions.Children.Add(CreateCommand(member));
                    NativeControlPresentation.ConstrainWrapItems(actions);
                    rows.Children.Add(actions);
                }
            }
        }

        var host = new Forms.NativeControlHost
        {
            Name = name,
            Dock = Forms.DockStyle.Fill,
            NativeControl = view
        };
        page.Controls.Add(host);
        Disposed += (_, _) =>
        {
            host.NativeControl = null;
            foreach (var source in sources)
                source.Dispose();
        };

        Button CreateCommand(Forms.Button command)
        {
            var caption = new TextBlock { TextWrapping = TextWrapping.Wrap };
            var button = new Avalonia.Controls.Button
            {
                Name = "Native" + command.Name,
                Content = caption,
                HorizontalAlignment = HorizontalAlignment.Left
            };
            void Refresh()
            {
                caption.Text = command.Text;
                AutomationProperties.SetName(button, command.Text);
                button.IsEnabled = command.Enabled;
                button.IsVisible = command.Visible;
            }
            var observer = new NativeControlObservation(command, Refresh);
            command.TextChanged += CommandTextChanged;
            void CommandTextChanged(object? sender, EventArgs args) => Refresh();
            button.Click += (_, _) => command.PerformClick();
            Refresh();
            Disposed += (_, _) =>
            {
                observer.Dispose();
                command.TextChanged -= CommandTextChanged;
            };
            return button;
        }

        void AddRow(StackPanel target, Avalonia.Controls.Control caption, string label,
            Forms.Control? source, Forms.Label? suffix)
        {
            if (source == null)
            {
                target.Children.Add(caption);
                return;
            }

            Avalonia.Controls.Control input;
            if (source is Forms.NumericUpDown numeric)
            {
                var binding = new NativeInlineNumericBinding(numeric, null, wholeNumbers: numeric.DecimalPlaces == 0);
                input = binding.Input;
                input.ClearValue(NumericUpDown.WidthProperty);
                input.ClearValue(NumericUpDown.MinHeightProperty);
                input.ClearValue(NumericUpDown.FontSizeProperty);
                input.ClearValue(NumericUpDown.PaddingProperty);
                AutomationProperties.SetName(input, label);
                Disposed += (_, _) => binding.Dispose();
            }
            else if (source is Forms.TextBox field)
            {
                var fields = new StackPanel
                {
                    Spacing = 4,
                    Children = { caption, CreateTextInput(field, label) }
                };
                AutomationProperties.SetLabeledBy(fields.Children[1], caption);
                var browse = field == txtPDFLocation ? cmdPDFLocation : field == txtPDFAppPath ? cmdPDFAppPath : null;
                if (browse != null)
                    fields.Children.Add(CreateCommand(browse));
                target.Children.Add(fields);
                return;
            }
            else
            {
                input = NativeListSelection.Attach(this, (Forms.ComboBox)source, label, layoutManaged: true)!;
            }
            if (caption is TextBlock)
                AutomationProperties.SetLabeledBy(input, caption);
            string columns = suffix != null ? "2*,Auto,*" : input is ComboBox { IsEditable: true } ? "*,2*" : "*,Auto";
            var row = new Grid { ColumnSpacing = 8, RowSpacing = 4, Children = { caption, input } };
            TextBlock? suffixText = null;
            if (suffix != null)
            {
                var text = new TextBlock { Text = suffix.Text, TextWrapping = TextWrapping.Wrap, VerticalAlignment = VerticalAlignment.Center };
                suffixText = text;
                row.Children.Add(text);
                void SuffixChanged(object? sender, EventArgs args) => text.Text = suffix.Text;
                suffix.TextChanged += SuffixChanged;
                Disposed += (_, _) => suffix.TextChanged -= SuffixChanged;
            }

            bool? stacked = null;
            void Reflow()
            {
                double fontSize = Avalonia.Controls.Documents.TextElement.GetFontSize(row);
                bool narrow = row.Bounds.Width < Math.Max(240, fontSize * 18);
                if (stacked == narrow)
                    return;
                stacked = narrow;
                row.ColumnDefinitions = new ColumnDefinitions(narrow ? "*" : columns);
                row.RowDefinitions = new RowDefinitions(narrow ? (suffixText == null ? "Auto,Auto" : "Auto,Auto,Auto") : "Auto");
                Grid.SetColumn(input, narrow ? 0 : 1);
                Grid.SetRow(input, narrow ? 1 : 0);
                if (suffixText != null)
                {
                    Grid.SetColumn(suffixText, narrow ? 0 : 2);
                    Grid.SetRow(suffixText, narrow ? 2 : 0);
                }
            }
            row.SizeChanged += (_, args) =>
            {
                if (args.NewSize.Width != args.PreviousSize.Width)
                    Reflow();
            };
            row.PropertyChanged += (_, args) =>
            {
                if (args.Property == Avalonia.Controls.Documents.TextElement.FontSizeProperty)
                    Reflow();
            };
            Reflow();
            target.Children.Add(row);
        }

        TextBox CreateTextInput(Forms.TextBox source, string label)
        {
            var binding = new NativeInlineTextBinding(source, () => true);
            var input = binding.Input;
            input.Name = "Native" + source.Name;
            input.ClearValue(TextBox.MinHeightProperty);
            input.ClearValue(TextBox.FontSizeProperty);
            input.IsReadOnly = source.ReadOnly;
            input.AcceptsReturn = source.Multiline;
            if (source.Multiline)
                input.TextWrapping = TextWrapping.Wrap;
            AutomationProperties.SetName(input, label);
            Disposed += (_, _) => binding.Dispose();
            return input;
        }
    }
}
