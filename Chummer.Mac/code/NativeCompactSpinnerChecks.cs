#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Presenters;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCompactSpinnerChecks
{
    internal static async Task RunAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;

        using (var options = new frmOptions())
        {
            options.Show();
            await Idle();
            var sources = typeof(frmOptions).GetFields(fields).Where(field => field.FieldType == typeof(Forms.NumericUpDown))
                .Select(field => (Forms.NumericUpDown)field.GetValue(options)!).ToArray();
            var hosts = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Where(host => host.NativeControl is Border { Child: NumericUpDown }).ToDictionary(host => host.Name);
            var houseHost = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Single(host => host.Name == "NativeHouseRules");
            var houseView = (ScrollViewer)houseHost.NativeControl!;
            Reveal(houseHost.Parent);
            await Idle();
            var houseInputs = houseView.GetVisualDescendants().OfType<NumericUpDown>().ToDictionary(input => input.Name!);
            var optionalHost = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Single(host => host.Name == "NativeOptionalRules");
            Reveal(optionalHost.Parent);
            await Idle();
            var optionalView = (ScrollViewer)optionalHost.NativeControl!;
            var optionalInput = optionalView.GetVisualDescendants().OfType<NumericUpDown>().Single();
            var bpHost = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Single(host => host.Name == "NativeBPCosts");
            Reveal(bpHost.Parent);
            await Idle();
            var bpView = (ScrollViewer)bpHost.NativeControl!;
            var bpInputs = bpView.GetVisualDescendants().OfType<NumericUpDown>().ToArray();
            var karmaHost = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Single(host => host.Name == "NativeKarmaCosts");
            Reveal(karmaHost.Parent);
            await Idle();
            var karmaView = (ScrollViewer)karmaHost.NativeControl!;
            var karmaInputs = karmaView.GetVisualDescendants().OfType<NumericUpDown>().ToArray();
            var characterHost = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Single(host => host.Name == "NativeCharacterAndPrinting");
            Reveal(characterHost.Parent);
            await Idle();
            var characterView = (ScrollViewer)characterHost.NativeControl!;
            var characterInputs = characterView.GetVisualDescendants().OfType<NumericUpDown>().ToArray();
            check(characterInputs.Length == 3 && characterInputs.All(input => input.Parent is Grid grid &&
                grid.Children[0].Bounds.Right <= input.Bounds.Left),
                "Character and Printing keeps build choices and labels separate from their inputs");
            var miscHost = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Single(host => host.Name == "NativeMiscellaneous");
            Reveal(miscHost.Parent);
            await Idle();
            var miscView = (ScrollViewer)miscHost.NativeControl!;
            var miscToggles = miscView.GetVisualDescendants().OfType<CheckBox>().ToArray();
            check(miscToggles.Length == 11 && miscToggles.All(toggle => toggle.IsEffectivelyVisible),
                "Miscellaneous presents all eleven native settings checkboxes");
            var globalHost = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Single(host => host.Name == "NativeGlobalPreferences");
            Reveal(globalHost.Parent);
            await Idle();
            var globalView = (ScrollViewer)globalHost.NativeControl!;
            var autosaveInput = globalView.GetVisualDescendants().OfType<NumericUpDown>().Single();
            var sourcesHost = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Single(host => host.Name == "NativeSources");
            Reveal(sourcesHost.Parent);
            await Idle();
            var sourcesView = (ScrollViewer)sourcesHost.NativeControl!;
            var pdfPath = sourcesView.GetVisualDescendants().OfType<TextBox>().Single(input => input.Name == "NativetxtPDFLocation");
            var browsePdf = sourcesView.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "NativecmdPDFLocation");
            check(pdfPath.Parent == browsePdf.Parent && pdfPath.Parent is StackPanel,
                "Preferences keeps the source PDF path and Browse command in the same responsive group");
            var sourceOffset = sourcesView.GetVisualDescendants().OfType<NumericUpDown>().Single();
            var ruleInputs = houseInputs.Values.Append(optionalInput).Concat(bpInputs).Concat(karmaInputs).Concat(characterInputs)
                .Append(autosaveInput).Append(sourceOffset)
                .ToDictionary(input => input.Name!);
            check(sources.Length == 63 && hosts.Count + ruleInputs.Count == sources.Length,
                "All 63 Preferences numeric fields have native inputs");
            foreach (var source in sources)
            {
                if (ruleInputs.TryGetValue("native" + source.Name, out var ruleInput))
                {
                    check(source.Parent == null && ruleInput.Minimum == source.Minimum && ruleInput.Maximum == source.Maximum &&
                        ruleInput.Increment == source.Increment && ruleInput.Value == source.Value,
                        source.Name + " keeps its controller values in the native Preferences row");
                    continue;
                }
                var host = hosts["native" + source.Name];
                var input = Input(host);
                Reveal(host.Parent);
                await Idle();
                check(source.Parent == null && input.Minimum == source.Minimum && input.Maximum == source.Maximum &&
                    input.Increment == source.Increment && input.Value == source.Value && input.IsEnabled == source.Enabled,
                    source.Name + " preserves Preferences range, increment, value and enabled state");
                if (host.Parent is Forms.ScrollableControl { AutoScroll: true } scroll && host.Bottom > scroll.ClientSize.Height)
                {
                    scroll.AutoScrollPosition = new System.Drawing.Point(0, -scroll.AutoScrollPosition.Y + host.Bottom - scroll.ClientSize.Height + 12);
                    await Idle();
                }
                await Idle();
                CheckLayout(input, source.Name + $" (tab={host.Parent?.Name}, host={host.Visible}/{host.Bounds}, valueVisible={input.IsVisible})");
            }

            Reveal(karmaHost.Parent);
            await Idle();
            var columns = (Grid)((StackPanel)karmaView.Content!).Children[0];
            check(karmaInputs.Length == 39 && Grid.GetColumn(columns.Children[1]) == 1 &&
                karmaInputs.All(input => input.Parent is Grid grid && grid.Children[0].Bounds.Right <= input.Bounds.Left &&
                    (grid.Children.Count == 2 || input.Bounds.Right <= grid.Children[2].Bounds.Left)),
                "Karma Costs presents all 39 inputs in two columns with separate labels and formula text");
            var initiation = karmaInputs.Single(input => input.Name == "nativenudKarmaInitiation");
            var formula = (Grid)initiation.Parent!;
            check(((TextBlock)formula.Children[0]).Text!.EndsWith("(") &&
                ((TextBlock)formula.Children[2]).Text!.Contains(") + 10") &&
                karmaView.GetVisualDescendants().OfType<Button>().Any(button => button.Name == "NativecmdRestoreDefaultsKarma"),
                "Karma Costs keeps the initiation formula and Restore Defaults command");
            karmaView.MaxWidth = 420;
            await Idle();
            check(Grid.GetColumn(columns.Children[1]) == 0 && Grid.GetRow(columns.Children[1]) == 1 &&
                columns.Children[0].Bounds.Bottom <= columns.Children[1].Bounds.Top &&
                karmaView.Extent.Width <= karmaView.Viewport.Width,
                "Narrow Karma Costs stacks the existing groups without horizontal scrolling");
            karmaView.ClearValue(ScrollViewer.MaxWidthProperty);

            Reveal(bpHost.Parent);
            await Idle();
            check(bpInputs.Length == 14 && bpInputs.All(input => input.Parent is Grid grid &&
                grid.Children[0] is TextBlock { Text.Length: > 0 } label &&
                label.Bounds.Right <= input.Bounds.Left) &&
                bpView.GetVisualDescendants().OfType<Button>().Any(button => button.Name == "NativecmdRestoreDefaultsBP"),
                "BP Costs renders all fourteen labeled cost rows and the Restore Defaults command");

            Reveal(optionalHost.Parent);
            await Idle();
            var optionalToggles = optionalView.GetVisualDescendants().OfType<CheckBox>().ToArray();
            var metatypeToggle = optionalToggles.Single(toggle => toggle.Name == "NativechkMetatypeCostsKarma");
            var metatypeSource = (Forms.CheckBox)typeof(frmOptions).GetField("chkMetatypeCostsKarma", fields)!.GetValue(options)!;
            var multiplierSuffix = (Forms.Label)typeof(frmOptions).GetField("lblMetatypeCostsKarma", fields)!.GetValue(options)!;
            check(optionalToggles.Length == 15 && optionalToggles.All(toggle => toggle.IsEffectivelyVisible) &&
                ReferenceEquals(metatypeToggle.Parent, optionalInput.Parent) &&
                metatypeToggle.Bounds.Right <= optionalInput.Bounds.Left &&
                ((TextBlock)metatypeToggle.Content!).Text == metatypeSource.Text + " " + multiplierSuffix.Text,
                "Optional Rules renders all native checkboxes and gives the metatype multiplier its own space");
            bool originalMetatype = metatypeSource.Checked;
            metatypeToggle.IsChecked = !originalMetatype;
            check(metatypeSource.Checked == !originalMetatype, "Optional Rules updates the existing settings controller");
            metatypeSource.Checked = originalMetatype;
            check(metatypeToggle.IsChecked == originalMetatype, "Optional Rules reflects values loaded by the controller");

            Reveal(houseHost.Parent);
            await Idle();
            var houseRows = (StackPanel)((StackPanel)houseView.Content!).Children[0];
            check(houseView.Extent.Height > houseView.Viewport.Height && houseView.Extent.Width <= houseView.Viewport.Width,
                "House Rules scrolls vertically and fits its content to the viewport width");
            var sample = houseInputs["nativenudFreeKarmaContactsMultiplier"];
            var row = (Grid)sample.Parent!;
            var toggle = row.Children.OfType<CheckBox>().Single();
            var caption = (TextBlock)toggle.Content!;
            double originalHeight = row.Bounds.Height;
            var originalCaption = caption.Text;
            houseView.MaxWidth = 320;
            caption.Text = string.Join(" ", Enumerable.Repeat(originalCaption, 4));
            await Idle();
            check(houseView.Bounds.Width <= 320 && row.Bounds.Height > originalHeight &&
                toggle.Bounds.Right <= sample.Bounds.Left &&
                houseRows.Children[1].Bounds.Top >= row.Bounds.Bottom,
                "At a narrow viewport, long House Rules captions wrap and move following rows without covering their inputs");
            Avalonia.Controls.Documents.TextElement.SetFontSize(houseView, caption.FontSize * 2);
            await Idle();
            check(Grid.GetRow(sample) == 1 && toggle.Bounds.Bottom <= sample.Bounds.Top &&
                sample.Bounds.Right <= row.Bounds.Width + 1 && houseRows.Children[1].Bounds.Top >= row.Bounds.Bottom,
                "At 200% text, House Rules stacks the multiplier below its caption without covering the next row");
            houseView.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
            houseView.ClearValue(ScrollViewer.MaxWidthProperty);
            caption.Text = originalCaption;
            var sampleSource = sources.Single(source => source.Name == "nudFreeKarmaContactsMultiplier");
            sample.Value = sampleSource.Value == sampleSource.Minimum ? sampleSource.Maximum : sampleSource.Minimum;
            check(sampleSource.Value == sample.Value, "House Rules native input updates the settings controller");
            var restriction = houseView.GetVisualDescendants().OfType<CheckBox>()
                .Single(control => control.Name == "NativechkRestrictStickNShock");
            var categories = houseView.GetVisualDescendants().OfType<Button>()
                .Single(control => control.Name == "NativecmdSelectStickNShockWeaponCategories");
            var restrictionSource = (Forms.CheckBox)typeof(frmOptions).GetField("chkRestrictStickNShock", fields)!.GetValue(options)!;
            bool previousRestriction = restrictionSource.Checked;
            restriction.IsChecked = true;
            check(restrictionSource.Checked && categories.IsEnabled, "House Rules toggle activates the existing category command");
            restrictionSource.Checked = false;
            check(restriction.IsChecked == false && !categories.IsEnabled,
                "House Rules controller changes update the native checkbox and category command");
            restrictionSource.Checked = previousRestriction;

            var autosave = sources.Single(source => source.Name == "nudCharacterAutosaveMinutes");
            Reveal(globalHost.Parent);
            var enable = (Forms.CheckBox)typeof(frmOptions).GetField("chkCharacterAutosave", fields)!.GetValue(options)!;
            enable.Checked = true;
            autosave.Value = autosave.Minimum;
            await Idle();
            autosaveInput.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_IncreaseButton")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(autosave.Value == autosave.Minimum + autosave.Increment,
                "Preferences native autosave spinner button updates the value used by Apply");
            enable.Checked = false;
            check(!autosaveInput.IsEnabled, "Disabling Preferences autosave disables its native interval");
            options.Close();
        }

        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = false;
        using var creation = new frmCreate(character) { MdiParent = owner, WindowState = Forms.FormWindowState.Maximized };
        creation.Show();
        var tabs = (Forms.TabControl)typeof(frmCreate).GetField("tabCharacterTabs", fields)!.GetValue(creation)!;
        var grades = new GradeList();
        grades.LoadList(Improvement.ImprovementSource.Cyberware);
        var ware = new Cyberware(character);
        var wareNode = new Forms.TreeNode();
        ware.Create(XmlManager.Instance.Load("cyberware.xml")
                .SelectSingleNode("/chummer/cyberwares/cyberware[name='Olfactory Booster']")!,
            character, grades.GetGrade("Standard"), Improvement.ImprovementSource.Cyberware, 1, wareNode,
            new List<Weapon>(), new List<Forms.TreeNode>(), false, false);
        character.Cyberware.Add(ware);
        var cyberwareTree = Field<Forms.TreeView>("treCyberware");
        cyberwareTree.Nodes[0].Nodes.Add(wareNode);
        foreach (var (name, tabName) in new[]
        {
            ("nudCyberwareRating", "tabCyberware"), ("nudArmorRating", "tabArmor"),
            ("nudMartialArtsRating", "tabMartialArts"), ("nudComplexFormRating", "tabTechnomancer"),
            ("nudMysticAdeptMAGMagician", "tabCommon"), ("nudSignal", "tabCommon"), ("nudResponse", "tabCommon")
        })
        {
            var source = (Forms.NumericUpDown)typeof(frmCreate).GetField(name, fields)!.GetValue(creation)!;
            var page = (Forms.TabPage)typeof(frmCreate).GetField(tabName, fields)!.GetValue(creation)!;
            if (tabName == "tabTechnomancer" && !tabs.TabPages.Contains(page))
                tabs.TabPages.Add(page);
            Reveal(page);
            if (name == "nudCyberwareRating")
                cyberwareTree.SelectedNode = wareNode;
            await Idle();
            var input = EditorInput(name);
            check(source.Parent == null && input.Minimum == source.Minimum && input.Maximum == source.Maximum &&
                input.Value == source.Value && input.IsEnabled == source.Enabled && input.IsVisible == source.Visible,
                name + " preserves the Creation controller range, value, and availability");
            bool visible = source.Visible;
            source.Visible = true;
            await Idle();
            CheckLayout(input, name);
            await CheckNarrowCommonInput(input, name, "Creation");
            if (name == "nudMysticAdeptMAGMagician")
            {
                var magic = owner.Controls.OfType<Forms.NativeControlHost>()
                    .Single(host => host.Name == "NativeCommonMagicAllocation");
                var contacts = Field<Forms.SplitContainer>("splitContacts");
                var contactOrigin = owner.PointToClient(contacts.PointToScreen(System.Drawing.Point.Empty));
                check(magic.Bottom <= contactOrigin.Y,
                    "Creation reserves space for Mystic Adept allocation above Contacts");
            }
            source.Visible = visible;
        }
        Reveal(Field<Forms.TabPage>("tabCyberware"));
        cyberwareTree.SelectedNode = wareNode;
        await Idle();
        var wareInput = EditorInput("nudCyberwareRating");
        await Idle();
        wareInput.Value = 2;
        check(ware.Rating == 2 && wareNode.Text == ware.DisplayName &&
            Field<Forms.Label>("lblCyberwareEssence").Text == ware.CalculatedESS.ToString(),
            "Editing native installed Cyberware rating updates the model, row, and essence");
        var gradeInput = CyberwareFields(owner.Controls.OfType<Forms.NativeControlHost>()
            .Single(host => host.Name == "NativeCreationCyberware")).OfType<ComboBox>().Single();
        var gradeSource = Field<Forms.ComboBox>("cboCyberwareGrade");
        int standard = gradeSource.SelectedIndex;
        int alternate = Enumerable.Range(0, gradeSource.Items.Count).First(index => index != standard);
        check(gradeInput.IsEnabled && gradeInput.SelectedIndex == standard && gradeInput.Items.Count == gradeSource.Items.Count,
            "Native Cyberware Grade presents the controller choices and selected grade");
        gradeInput.SelectedIndex = alternate;
        check(gradeSource.SelectedIndex == alternate && ware.Grade.Name == gradeSource.SelectedValue?.ToString(),
            "Changing native Cyberware Grade updates the installed implant");
        gradeInput.SelectedIndex = standard;

        var armorCatalog = XmlManager.Instance.Load("armor.xml");
        var armor = new Armor(character);
        var armorNode = new Forms.TreeNode();
        armor.Create(armorCatalog.SelectSingleNode("/chummer/armors/armor[name='Armor Vest']")!, armorNode, null, false, false);
        var modification = new ArmorMod(character);
        var modificationNode = new Forms.TreeNode();
        modification.Create(armorCatalog.SelectSingleNode("/chummer/mods/mod[name='Fire Resistance']")!,
            modificationNode, 1, new List<Weapon>(), new List<Forms.TreeNode>());
        modification.Parent = armor;
        armor.ArmorMods.Add(modification);
        armorNode.Nodes.Add(modificationNode);
        character.Armor.Add(armor);
        var armorTree = Field<Forms.TreeView>("treArmor");
        armorTree.Nodes[0].Nodes.Add(armorNode);
        Reveal(Field<Forms.TabPage>("tabArmor"));
        armorTree.SelectedNode = modificationNode;
        var armorInput = EditorInput("nudArmorRating");
        await Idle();
        armorInput.Value = 3;
        check(modification.Rating == 3 && modificationNode.Text == modification.DisplayName &&
            Field<Forms.Label>("lblArmorCost").Text.Contains("300", StringComparison.Ordinal),
            "Editing native installed Armor rating updates the model, row, and cost");
        var reopened = new Character();
        reopened.LoadSnapshot(character.CaptureSnapshot());
        check(reopened.Cyberware.Single(item => item.InternalId == ware.InternalId).Rating == 2 &&
            reopened.Armor.Single(item => item.InternalId == armor.InternalId).ArmorMods.Single().Rating == 3,
            "Edited Cyberware and Armor ratings survive the existing character format");
        typeof(frmCreate).GetField("_blnIsDirty", fields)!.SetValue(creation, false);
        creation.Close();

        var careerCharacter = new Character();
        careerCharacter.LoadSnapshot(fixture.CaptureSnapshot());
        careerCharacter.Created = true;
        careerCharacter.MAGEnabled = true;
        careerCharacter.AdeptEnabled = true;
        careerCharacter.MagicianEnabled = true;
        using var career = new frmCareer(careerCharacter) { MdiParent = owner, WindowState = Forms.FormWindowState.Maximized };
        career.Show();
        try
        {
            var characterTabs = CareerField<Forms.TabControl>("tabCharacterTabs");
            check(typeof(frmCareer).GetFields(fields).Count(field => field.FieldType == typeof(Forms.NumericUpDown)) == 7,
                "The Career test accounts for all seven numeric fields");
            foreach (var (name, tabName) in new[]
            {
                ("nudStreetCred", "tabCharacterInfo"), ("nudNotoriety", "tabCharacterInfo"),
                ("nudPublicAware", "tabCharacterInfo"), ("nudComplexFormRating", "tabTechnomancer"),
                ("nudMysticAdeptMAGMagician", "tabCommon"), ("nudSignal", "tabCommon"), ("nudResponse", "tabCommon")
            })
            {
                var source = CareerField<Forms.NumericUpDown>(name);
                var page = CareerField<Forms.TabPage>(tabName);
                if (tabName == "tabTechnomancer" && !characterTabs.TabPages.Contains(page))
                    characterTabs.TabPages.Add(page);
                Reveal(page);
                var input = CareerInput(name);
                await Idle();
                check(source.Parent == null && input.Minimum == source.Minimum && input.Maximum == source.Maximum &&
                    input.Value == source.Value && input.IsEnabled == source.Enabled && input.IsVisible == source.Visible,
                    name + " preserves the Career controller range, value, and availability");
                bool visible = source.Visible;
                source.Visible = true;
                await Idle();
                CheckLayout(input, name);
                await CheckNarrowCommonInput(input, name, "Career");
                source.Visible = visible;
            }

            Reveal(CareerField<Forms.TabPage>("tabCharacterInfo"));
            await Idle();
            var info = (ScrollViewer)owner.Controls.OfType<Forms.NativeControlHost>()
                .Single(host => host.Name == "NativeCharacterNarrative").NativeControl!;
            var sidebar = ((Grid)info.Content!).Children.OfType<StackPanel>().Single(panel => panel.Name == "NativeCharacterInfoSidebar");
            var reputation = sidebar.Children.OfType<StackPanel>().Single(panel => panel.Name == "NativeCareerReputation");
            check(reputation.IsEffectivelyVisible && reputation.Bounds.Bottom <= sidebar.Children[1].Bounds.Top,
                "Career reputation remains above the portrait in the shared scroll viewport");
            var streetCred = CareerInput("nudStreetCred");
            var streetField = (Grid)streetCred.Parent!;
            var streetCaption = (StackPanel)streetField.Children[0];
            reputation.MaxWidth = 150;
            Avalonia.Controls.Documents.TextElement.SetFontSize(reputation, streetCred.FontSize * 1.5);
            await Idle();
            check(Grid.GetRow(streetCred) == 1 && streetCaption.Bounds.Bottom <= streetCred.Bounds.Top &&
                streetCred.Bounds.Right <= streetField.Bounds.Width + 1,
                "Career reputation keeps its label and total above the editable input at 150% text and narrow width");
            reputation.ClearValue(Control.MaxWidthProperty);
            reputation.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
            await Idle();
            streetCred.Value = 4;
            CareerInput("nudNotoriety").Value = 2;
            CareerInput("nudPublicAware").Value = 1;
            check(careerCharacter.StreetCred == 4 && careerCharacter.Notoriety == 2 && careerCharacter.PublicAwareness == 1 &&
                CareerField<bool>("_blnIsDirty"),
                "Native Career reputation edits update all three character fields and mark the editor dirty");
            var restoredCareer = new Character();
            restoredCareer.LoadSnapshot(careerCharacter.CaptureSnapshot());
            check(restoredCareer.StreetCred == 4 && restoredCareer.Notoriety == 2 && restoredCareer.PublicAwareness == 1,
                "Native Career reputation edits survive the existing character format");
        }
        finally
        {
            typeof(frmCareer).GetField("_blnIsDirty", fields)!.SetValue(career, false);
            career.Close();
        }

        T CareerField<T>(string name) => (T)typeof(frmCareer).GetField(name, fields)!.GetValue(career)!;
        NumericUpDown CareerInput(string name) => EditorInput(name);

        NumericUpDown EditorInput(string name)
        {
            string? section = name switch
            {
                "nudCyberwareRating" => "NativeCreationCyberware",
                "nudComplexFormRating" => owner.ActiveMdiChild is frmCareer
                    ? "NativeCareerComplexForms" : "NativeCreationComplexForms",
                "nudMartialArtsRating" => "NativeCreationMartialArts",
                "nudArmorRating" => "NativeCreationArmor",
                "nudSignal" or "nudResponse" => "NativeCommonAiAttributes",
                "nudStreetCred" or "nudNotoriety" or "nudPublicAware" => "NativeCharacterNarrative",
                "nudMysticAdeptMAGMagician" => "NativeCommonMagicAllocation",
                _ => null
            };
            var host = owner.Controls.OfType<Forms.NativeControlHost>()
                .Single(item => item.Name == (section ?? "native" + name));
            if (section is "NativeCreationComplexForms" or "NativeCareerComplexForms" or "NativeCreationMartialArts")
            {
                var layout = (Grid)((Grid)host.NativeControl!).Children[0];
                var details = layout.Children.OfType<ScrollViewer>().Single();
                return ((StackPanel)details.Content!).Children.OfType<WrapPanel>().SelectMany(row => row.Children)
                    .OfType<Grid>().SelectMany(field => field.Children).OfType<NumericUpDown>()
                    .Single(input => input.Name == "native" + name);
            }
            if (section == "NativeCreationCyberware")
                return CyberwareFields(host).OfType<NumericUpDown>().Single();
            if (section == "NativeCreationArmor")
                return ((StackPanel)((Control)host.NativeControl!).GetVisualDescendants().OfType<ScrollViewer>()
                    .Single(view => view.Name == "NativeArmorEquipment").Content!).Children
                    .OfType<WrapPanel>().SelectMany(row => row.Children).OfType<Grid>()
                    .SelectMany(field => field.Children).OfType<NumericUpDown>().Single();
            if (section == "NativeCharacterNarrative")
            {
                var sidebar = ((Grid)((ScrollViewer)host.NativeControl!).Content!).Children.OfType<StackPanel>()
                    .Single(panel => panel.Name == "NativeCharacterInfoSidebar");
                var reputation = (StackPanel)sidebar.Children[0];
                return reputation.Children.OfType<Grid>().SelectMany(row => row.Children).OfType<NumericUpDown>()
                    .Single(input => input.Name == "native" + name);
            }
            if (section == "NativeCommonMagicAllocation")
            {
                var content = (StackPanel)((ScrollViewer)((Border)host.NativeControl!).Child!).Content!;
                return ((StackPanel)((Grid)content.Children[1]).Children[0]).Children.OfType<NumericUpDown>().Single();
            }
            return section != null
                ? ((StackPanel)((ScrollViewer)((Border)host.NativeControl!).Child!).Content!).Children
                    .OfType<Grid>().SelectMany(row => row.Children).OfType<NumericUpDown>().Single(input => input.Name == "native" + name)
                : Input(host);
        }

        static IEnumerable<Control> CyberwareFields(Forms.NativeControlHost host)
        {
            var layout = (Grid)((Grid)host.NativeControl!).Children[0];
            var details = layout.Children.OfType<ScrollViewer>().Single();
            return details.GetVisualDescendants().OfType<Control>();
        }

        T Field<T>(string name) => (T)typeof(frmCreate).GetField(name, fields)!.GetValue(creation)!;

        async Task CheckNarrowCommonInput(NumericUpDown input, string name, string editor)
        {
            if (name is not ("nudMysticAdeptMAGMagician" or "nudSignal" or "nudResponse"))
                return;

            var scroll = input.GetVisualAncestors().OfType<ScrollViewer>().First();
            string region = name == "nudMysticAdeptMAGMagician" ? "NativeCommonMagicAllocation" : "NativeCommonAiAttributes";
            var host = owner.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == region);
            var original = owner.ClientSize;
            int targetWidth = host.Left + 145 + 6;
            try
            {
                if (name == "nudResponse" || editor == "Career")
                {
                    // Career's summary can change the main sheet width on either side of its breakpoint.
                    // Constrain the field viewport directly for the narrow typography check.
                    scroll.MaxWidth = 145;
                }
                else
                {
                    owner.ClientSize = new System.Drawing.Size(targetWidth, original.Height);
                    for (int attempt = 0; attempt < 4; attempt++)
                    {
                        // The native window and hosted viewport settle on different dispatcher turns.
                        await Task.Delay(60);
                        await Idle();
                        int correction = 145 - (int)Math.Round(scroll.Bounds.Width);
                        if (correction == 0)
                            break;
                        owner.ClientSize = new System.Drawing.Size(owner.ClientSize.Width + correction, original.Height);
                    }
                }
                await Idle();
                input.BringIntoView();
                await Idle();
                int minimumWidth = editor == "Career" ? 120 : 145;
                check(scroll.Bounds.Width >= minimumWidth && scroll.Bounds.Width <= 145 && input.Bounds.Width >= 100,
                    $"{editor} {name} uses the full narrow column [viewport={scroll.Viewport}, input={input.Bounds}]");
                CheckLayout(input, editor + " narrow " + name);
                double baseFont = input.FontSize;
                Avalonia.Controls.Documents.TextElement.SetFontSize(scroll, baseFont * 1.5);
                await Idle();
                input.BringIntoView();
                await Idle();
                var nativeEditor = input.GetVisualDescendants().OfType<TextBox>().Single();
                check(input.FontSize == baseFont * 1.5 && nativeEditor.FontSize == input.FontSize,
                    $"{editor} {name} inherits enlarged text in its numeric editor [input={input.FontSize}, editor={nativeEditor.FontSize}]");
                CheckLayout(input, editor + " 150% " + name);
                if (name is "nudSignal" or "nudMysticAdeptMAGMagician")
                {
                    int previousHeight = host.Height;
                    owner.ClientSize = new System.Drawing.Size(owner.ClientSize.Width, owner.ClientSize.Height - 80);
                    for (int attempt = 0; attempt < 20 && host.Height >= previousHeight; attempt++)
                    {
                        await Task.Delay(20);
                        await Idle();
                    }
                    check(host.Height > 0 && host.Height < previousHeight && scroll.Extent.Height >= scroll.Viewport.Height,
                        $"{editor} {name} viewport shrinks with workspace height at 150% text [host={previousHeight}/{host.Height}, viewport={scroll.Viewport}, extent={scroll.Extent}]");
                }
            }
            finally
            {
                scroll.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                scroll.ClearValue(ScrollViewer.MaxWidthProperty);
                owner.ClientSize = original;
                await Idle();
            }
        }

        void CheckLayout(NumericUpDown input, string name)
        {
            var textBox = input.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
            var buttons = input.GetVisualDescendants().OfType<Button>().Where(button =>
                button.Name is "PART_IncreaseButton" or "PART_DecreaseButton").ToArray();
            check(TopLevel.GetTopLevel(input) != null && input.IsEffectivelyVisible && textBox is { Bounds.Width: >= 10, Bounds.Height: >= 16 } &&
                buttons.Count(button => button.IsEffectivelyVisible && button.Bounds.Width >= 9) == 2,
                name + $" lays out a readable input and both spinner buttons (visible={input.IsEffectivelyVisible}, input={input.Bounds.Size}, text={textBox?.Bounds.Size}, buttons={string.Join("/", buttons.Select(button => $"{button.GetType().Name}.{button.TemplatedParent?.GetType().Name}:{button.Bounds.Size}/{button.Width}/{button.MinWidth}/{button.MaxWidth}"))})");

            var text = textBox?.GetVisualDescendants().OfType<TextPresenter>().FirstOrDefault();
            var textOrigin = text?.TranslatePoint(default, input);
            var editorOrigin = textBox?.TranslatePoint(default, input);
            var buttonBounds = buttons.Select(button => new Rect(
                button.TranslatePoint(default, input)!.Value, button.Bounds.Size)).ToArray();
            var visibleText = textOrigin.HasValue && editorOrigin.HasValue && textBox!.ClipToBounds
                ? new Rect(textOrigin.Value, text!.Bounds.Size).Intersect(new Rect(editorOrigin.Value, textBox.Bounds.Size))
                : default;
            check(visibleText.Width > 0 && visibleText.Height > 0 && buttonBounds.Length == 2 &&
                !Overlaps(buttonBounds[0], buttonBounds[1]) &&
                buttonBounds.All(button => !Overlaps(button, visibleText)),
                name + $" keeps the value text and spinner buttons from overlapping [text={visibleText}, buttons={string.Join(" / ", buttonBounds)}]");

            static bool Overlaps(Rect first, Rect second) =>
                first.Left < second.Right && second.Left < first.Right &&
                first.Top < second.Bottom && second.Top < first.Bottom;
        }

        static NumericUpDown Input(Forms.NativeControlHost host) => (NumericUpDown)((Border)host.NativeControl!).Child!;
        static async Task Idle() => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        static void Reveal(Forms.Control? control)
        {
            if (control == null) return;
            Reveal(control.Parent);
            if (control is Forms.TabPage page && page.Parent is Forms.TabControl tabs)
                tabs.SelectedTab = page;
        }
        static IEnumerable<Forms.Control> Descendants(IEnumerable<Forms.Control> roots) =>
            roots.SelectMany(control => Descendants(control.Controls.Cast<Forms.Control>()).Prepend(control));
    }
}
