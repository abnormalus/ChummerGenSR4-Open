#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCharacterTreeChecks
{
    internal static async Task RunAsync(frmMain main, Character fixture, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character();
            character.LoadSnapshot(fixture.CaptureSnapshot());
            character.Created = career;
            var implant = new Cyberware(character);
            var grades = new GradeList();
            grades.LoadList(Improvement.ImprovementSource.Cyberware);
            implant.Create(XmlManager.Instance.Load("cyberware.xml")
                    .SelectSingleNode("/chummer/cyberwares/cyberware[name='Obvious Lower Arm']")!, character,
                grades.GetGrade("Standard"), Improvement.ImprovementSource.Cyberware, 0, new Forms.TreeNode(), [], [], false, false, "Left");
            character.Cyberware.Add(implant);
            var spell = new Spell(character);
            spell.Create(XmlManager.Instance.Load("spells.xml").SelectSingleNode("/chummer/spells/spell[name='Manabolt']")!,
                character, new Forms.TreeNode());
            character.Spells.Add(spell);
            using Forms.Form editor = career ? new frmCareer(character) : new frmCreate(character);
            editor.MdiParent = main;
            editor.Show();
            character.MAGEnabled = true;
            var metamagic = new Metamagic(character) { Name = "Synthetic metamagic", Source = "SR4", Page = "1" };
            character.Metamagics.Add(metamagic);
            byte[] before = character.CaptureSnapshot();
            T Field<T>(string name) => (T)editor.GetType().GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(editor)!;

            var outer = Field<Forms.TabControl>("tabCharacterTabs");
            var gear = Field<Forms.TabControl>("tabStreetGearTabs");
            foreach (var (name, tabName) in new[]
            {
                ("Cyberware", "tabCyberware"), ("Spells", "tabMagician"),
                ("MartialArts", "tabMartialArts"), ("Foci", "tabGear"),
                ("Metamagic", "tabInitiation"), ("CritterPowers", "tabCritter"),
                ("ComplexForms", "tabTechnomancer")
            })
            {
                var tab = Field<Forms.TabPage>(tabName);
                if (name == "Foci")
                {
                    outer.SelectedTab = Field<Forms.TabPage>("tabStreetGear");
                    gear.SelectedTab = tab;
                }
                else
                {
                    if (!outer.TabPages.Contains(tab))
                        outer.TabPages.Add(tab);
                    outer.SelectedTab = tab;
                }

                string label = (career ? "Career" : "Creation") + name;
                var host = main.Controls.OfType<Forms.NativeControlHost>().SingleOrDefault(control => control.Name == "Native" + (name == "Foci" ? (career ? "CareerGear" : "CreationGear") : label));
                check(host != null, label + " installs a native host");
                var source = Field<Forms.TreeView>("tre" + name);
                var native = (Control)host!.NativeControl!;
                var responsivePage = name is "Cyberware" or "CritterPowers" or "ComplexForms" or "Spells" or "MartialArts" or "Metamagic" ? (Grid)((Grid)native).Children[0] : null;
                var choices = responsivePage?.Children[0] ?? native;
                var tree = name == "Foci" ? native.GetVisualDescendants().OfType<TreeView>()
                    .Single(candidate => candidate.Name == "Native" + label)
                    : choices is Grid pane ? pane.Children.OfType<TreeView>().Single() : (TreeView)choices;
                if (name is "Cyberware" or "Spells" or "MartialArts" or "ComplexForms" or "CritterPowers" or "Metamagic")
                {
                    var commands = ((Grid)choices).Children.OfType<WrapPanel>().Single();
                    check(commands.Children.Count == (name is "Cyberware" or "MartialArts" ? 3 : 2) && commands.Bounds.Bottom <= tree.Bounds.Top,
                        $"{(career ? "Career" : "Creation")} {name} keeps the wrapping commands above its tree");
                }
                if (name == "Cyberware")
                {
                    var details = responsivePage!.Children.OfType<ScrollViewer>().Single();
                    var nameSource = Field<Forms.Label>("lblCyberwareName");
                    string previousName = nameSource.Text;
                    bool previousVisibility = nameSource.Visible;
                    var itemRows = ((StackPanel)details.Content!).Children.OfType<StackPanel>().Single();
                    var readout = itemRows.Children.OfType<WrapPanel>().SelectMany(row => row.Children)
                        .OfType<TextBlock>().Single(text => text.Name == "NativelblCyberwareName");
                    source.SelectedNode = null;
                    check(!readout.IsEffectivelyVisible, label + " hides item readouts without a selection");
                    source.SelectedNode = source.Nodes.Cast<Forms.TreeNode>().SelectMany(root => root.Nodes.Cast<Forms.TreeNode>())
                        .Single(node => Equals(node.Tag, implant.InternalId));
                    try
                    {
                        details.Width = 145;
                        double enlarged = readout.FontSize * 1.5;
                        Avalonia.Controls.Documents.TextElement.SetFontSize(details, enlarged);
                        nameSource.Visible = true;
                        nameSource.Text = "Translated cyberware name with a long implant description";
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        check(readout.IsEffectivelyVisible && readout.FontSize == enlarged &&
                            readout.Bounds.Right <= details.Viewport.Width + 1 && readout.Bounds.Height > enlarged * 2,
                            label + " wraps a long implant name within 145 logical pixels at 150% text");
                        if (!career)
                        {
                            var inputFields = itemRows.Children.OfType<WrapPanel>().SelectMany(row => row.Children)
                                .OfType<Grid>().ToArray();
                            check(inputFields.Length == 2 && inputFields.All(field =>
                            {
                                var heading = field.Children.OfType<TextBlock>().Single();
                                var input = field.Children.Single(child => child != heading);
                                return Grid.GetRow(input) == 1 && heading.Bounds.Bottom <= input.Bounds.Top &&
                                    input.Bounds.Right <= field.Bounds.Width + 1;
                            }), "Creation Cyberware keeps Grade and Rating below their own captions at 145 logical pixels");
                        }
                    }
                    finally
                    {
                        nameSource.Text = previousName;
                        nameSource.Visible = previousVisibility;
                        details.ClearValue(Control.WidthProperty);
                        details.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    }
                }
                if (name == "MartialArts")
                {
                    var details = responsivePage!.Children.OfType<ScrollViewer>().Single();
                    var rows = (StackPanel)details.Content!;
                    var rating = rows.Children.OfType<WrapPanel>().Single();
                    var sourceReference = rows.Children.OfType<Button>().Single();
                    var caption = Field<Forms.Label>("lblMartialArtsRatingLabel");
                    string originalCaption = caption.Text;
                    try
                    {
                        details.Width = 145;
                        double enlarged = sourceReference.FontSize * 1.5;
                        Avalonia.Controls.Documents.TextElement.SetFontSize(details, enlarged);
                        caption.Text = "Translated rating of the currently selected martial art";
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        var ratingCaption = rating.GetVisualDescendants().OfType<TextBlock>()
                            .Single(text => text.Text?.Contains(caption.Text) == true);
                        var ratingInput = rating.GetVisualDescendants().OfType<NumericUpDown>().SingleOrDefault();
                        check(ratingCaption.FontSize == enlarged && ratingCaption.Bounds.Height > enlarged * 2 &&
                            (career || ratingInput != null && Grid.GetRow(ratingInput) == 1 &&
                                ratingCaption.Bounds.Bottom <= ratingInput.Bounds.Top) &&
                            rating.Children.Where(item => item.IsVisible).All(item => item.Bounds.Right <= rating.Bounds.Width + 1) &&
                            rating.Bounds.Bottom <= sourceReference.Bounds.Top,
                            label + " wraps the rating caption above the source link at 150% text");
                    }
                    finally
                    {
                        caption.Text = originalCaption;
                        details.ClearValue(Control.WidthProperty);
                        details.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    }
                }
                if (name == "Spells")
                {
                    var spiritPanel = Field<Forms.Panel>("panSpirits");
                    var spiritHeader = ((Grid)native).Children.OfType<WrapPanel>().Single();
                    var spiritOrigin = main.PointToClient(spiritPanel.PointToScreen(System.Drawing.Point.Empty));
                    check(spiritHeader.Children.OfType<Button>().Any(button => button.Name == "NativecmdAddSpirit") &&
                        host.Bottom <= spiritOrigin.Y && spiritPanel.Bounds ==
                            MagicInventoryLayout.EntityPanelBounds(InventoryTreeLayout.VisiblePageSize(main, tab)),
                        label + " reserves the Spirit list and places its native command above it");
                    var spellDetails = responsivePage!.Children.OfType<ScrollViewer>().Single();
                    var spellRows = (StackPanel)spellDetails.Content!;
                    var spellRoot = source.Nodes[0];
                    var spellNode = spellRoot.Nodes.Cast<Forms.TreeNode>().Single(node => Equals(node.Tag, spell.InternalId));
                    spellRoot.Expand();
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var nativeRoot = tree.Items.OfType<TreeViewItem>().Single(row => ReferenceEquals(row.Tag, spellRoot));
                    tree.SelectedItem = nativeRoot.Items.OfType<TreeViewItem>().Single(row => ReferenceEquals(row.Tag, spellNode));
                    var citation = spellRows.Children.OfType<Button>().Single(button => button.Name == "NativeSpellSource");
                    check(citation.IsEnabled && citation.Content is TextBlock { Text: { } sourceCitation } &&
                        sourceCitation.EndsWith("SR4 204", StringComparison.Ordinal) &&
                        Field<Forms.Label>("lblSpellDescriptors").Text == spell.DisplayDescriptors,
                        label + " shows the selected installed spell's descriptors and source");
                    var sourceText = Field<Forms.Label>("lblSpellDescriptors");
                    string previous = sourceText.Text;
                    bool visible = sourceText.Visible;
                    sourceText.Visible = true;
                    sourceText.Text = "An especially long set of spell descriptors that must wrap";
                    spellDetails.Width = 145;
                    var descriptors = ((WrapPanel)spellRows.Children[0]).Children.OfType<TextBlock>().Single();
                    double enlarged = descriptors.FontSize * 1.5;
                    Avalonia.Controls.Documents.TextElement.SetFontSize(spellDetails, enlarged);
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(host.Visible && descriptors.FontSize == enlarged && descriptors.Bounds.Height >= 30 &&
                        descriptors.Bounds.Right <= spellDetails.Viewport.Width + 1 &&
                        descriptors.Text?.EndsWith(sourceText.Text, StringComparison.Ordinal) == true &&
                        spellRows.Children[0].Bounds.Bottom <= spellRows.Children[1].Bounds.Top,
                        label + " wraps long descriptors above the remaining spell details at 150% text");
                    spellDetails.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    spellDetails.ClearValue(Control.WidthProperty);
                    sourceText.Text = previous;
                    sourceText.Visible = visible;
                }
                if (name == "ComplexForms")
                {
                    var spriteHeader = ((Grid)native).Children.OfType<WrapPanel>().Single();
                    var addSprite = spriteHeader.Children.OfType<Button>().Single();
                    var addSpriteSource = Field<Forms.Button>("cmdAddSprite");
                    bool addSpriteVisible = addSpriteSource.Visible;
                    addSpriteSource.Visible = true;
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var spritePanel = Field<Forms.Panel>("panSprites");
                    var spriteOrigin = main.PointToClient(spritePanel.PointToScreen(System.Drawing.Point.Empty));
                    check(addSprite.IsEffectivelyVisible && addSprite.Bounds.Width > 0 &&
                        responsivePage!.Bounds.Bottom <= spriteHeader.Bounds.Top && host.Bottom <= spriteOrigin.Y,
                        label + " keeps the native Add Sprite command between the form layout and sprite rows");
                    var shortPanel = MagicInventoryLayout.EntityPanelBounds(new System.Drawing.Size(480, 330));
                    check(spritePanel.Bounds == MagicInventoryLayout.EntityPanelBounds(InventoryTreeLayout.VisiblePageSize(main, tab)) &&
                        shortPanel.Top < spritePanel.Top && shortPanel.Height >= 90 && shortPanel.Bottom < 330,
                        label + " reserves a sprite viewport when the Technomancer page gets shorter");
                    addSpriteSource.Visible = addSpriteVisible;
                    var summary = responsivePage!.Children.OfType<ScrollViewer>().Single();
                    var summaryRows = (StackPanel)summary.Content!;
                    var citation = summaryRows.Children.OfType<Button>().Single();
                    var sourceLabel = Field<Forms.Label>("lblComplexFormSource");
                    string originalSource = sourceLabel.Text;
                    bool sourceVisible = sourceLabel.Visible;
                    summary.Width = 145;
                    sourceLabel.Visible = true;
                    sourceLabel.Text = "A long and deliberately detailed complex form sourcebook citation, page 154";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(host.Visible && citation.IsEnabled && ((TextBlock)citation.Content!).Bounds.Height >= 30 &&
                        citation.Bounds.Top >= summaryRows.Children[1].Bounds.Bottom,
                        label + " wraps the source citation below the skill readout");
                    var stream = summaryRows.GetVisualDescendants().OfType<ComboBox>()
                        .Single(input => input.Name == "NativecboStream");
                    var streamCaption = ((Grid)stream.Parent!).Children.OfType<TextBlock>().Single();
                    var streamSource = Field<Forms.ComboBox>("cboStream");
                    check(stream.Items.Count == streamSource.Items.Count && stream.SelectedIndex == streamSource.SelectedIndex &&
                        stream.IsEnabled == streamSource.Enabled && stream.Bounds.Right <= summary.Viewport.Width + 1 &&
                        Grid.GetRow(stream) == 1 && streamCaption.Bounds.Bottom <= stream.Bounds.Top,
                        label + " keeps the Stream choices synchronized in the narrow summary");
                    summary.ClearValue(Control.WidthProperty);
                    sourceLabel.Text = originalSource;
                    sourceLabel.Visible = sourceVisible;

                    var fields = summaryRows.Children.OfType<StackPanel>()
                        .Single(panel => panel.Name == "NativeTechnomancerPersona");
                    var stats = (WrapPanel)fields.Children[1];
                    var descriptionSource = Field<Forms.Label>("lblParagonInformation");
                    var description = (TextBlock)fields.Children[3];
                    string previous = descriptionSource.Text;
                    bool wasVisible = descriptionSource.Visible;
                    summary.Width = 145;
                    descriptionSource.Visible = true;
                    descriptionSource.Text = string.Join(" ", Enumerable.Repeat("The paragon has a long description.", 20));
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(host.Visible && stats.Children.Count == 5 && description.Text == descriptionSource.Text &&
                        description.Bounds.Top >= stats.Bounds.Bottom && summary.Extent.Height > summary.Viewport.Height,
                        label + " wraps and scrolls Paragon text below the Living Persona statistics");
                    summary.ClearValue(Control.WidthProperty);
                    descriptionSource.Text = previous;
                    descriptionSource.Visible = wasVisible;
                }
                if (responsivePage != null)
                {
                    var details = responsivePage.Children.OfType<ScrollViewer>().Single();
                    responsivePage.Width = 480;
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(Grid.GetRow(details) == 1 && choices.Bounds.Bottom <= details.Bounds.Top &&
                        tree.Bounds.Height > 0 && details.Bounds.Height > 0,
                        label + " stacks the tree above the details in a narrow page");
                    if (name == "CritterPowers")
                    {
                        var fields = (StackPanel)details.Content!;
                        var nameLabel = Field<Forms.Label>("lblCritterPowerName");
                        var nameReadout = fields.Children.OfType<TextBlock>()
                            .Single(field => field.Name == "NativelblCritterPowerName");
                        string original = nameLabel.Text;
                        bool visible = nameLabel.Visible;
                        details.Width = 145;
                        nameLabel.Visible = true;
                        nameLabel.Text = "A deliberately long name for the selected critter power";
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        check(nameReadout.Bounds.Height >= 30 && nameReadout.Text?.EndsWith(nameLabel.Text, StringComparison.Ordinal) == true &&
                            nameReadout.Bounds.Bottom <= fields.Children[2].Bounds.Top,
                            label + " wraps the power name above its category");
                        details.ClearValue(Control.WidthProperty);
                        nameLabel.Text = original;
                        nameLabel.Visible = visible;
                    }
                    responsivePage.ClearValue(Control.WidthProperty);
                }
                if (name == "Foci")
                {
                    var region = (Grid)tree.Parent!;
                    var heading = region.Children.OfType<TextBlock>().Single();
                    var commands = region.Children.OfType<WrapPanel>().Single();
                    check(!string.IsNullOrWhiteSpace(heading.Text) && commands.Children.Count == 1 &&
                        heading.Bounds.Bottom <= commands.Bounds.Top && commands.Bounds.Bottom <= tree.Bounds.Top,
                        label + " measures the heading and stacked focus command above its tree");
                    var scroller = native.GetVisualDescendants().OfType<ScrollViewer>()
                        .Single(view => view.Name == (career ? "NativeCareerGearSummary" : "NativeCreationGearSummary"));
                    var pageLayout = (Grid)((Grid)native).Children[0];
                    var inventoryPane = (Grid)pageLayout.Children[0];
                    var detailPane = (Grid)pageLayout.Children[1];
                    pageLayout.Width = 480;
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(Grid.GetColumn(detailPane) == 0 && Grid.GetRow(detailPane) == 1 &&
                        inventoryPane.Bounds.Bottom <= detailPane.Bounds.Top && scroller.Bounds.Height > 0 &&
                        scroller.Bounds.Bottom <= region.Bounds.Top && tree.Bounds.Height > 0,
                        label + " stacks the inventory above the details and Bonded Foci at narrow widths");
                    pageLayout.ClearValue(Control.WidthProperty);
                    var rows = (StackPanel)scroller.Content!;
                    var matrix = (WrapPanel)rows.Children[5];
                    var response = Field<Forms.Label>("lblGearResponse");
                    var signal = Field<Forms.Label>("lblGearSignal");
                    string original = response.Text;
                    bool responseVisible = response.Visible;
                    bool signalVisible = signal.Visible;
                    response.Visible = true;
                    signal.Visible = true;
                    response.Text = "Synthetic response";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(host.Visible && scroller.Bounds.Width > 0 && rows.Children.Count == 9 &&
                        ((TextBlock)matrix.Children[0]).Text?.EndsWith(response.Text, StringComparison.Ordinal) == true,
                        label + " displays the Matrix and weapon readouts in a scrolling region");
                    double width = scroller.Width;
                    scroller.MaxHeight = 160;
                    scroller.Width = 145;
                    response.Text = "A deliberately long response value to exercise wrapping in narrow layouts while the other Gear details, including the selected item’s sourcebook and equipment options, remain in the same scroll area";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var responseText = (TextBlock)matrix.Children[0];
                    var signalText = (TextBlock)matrix.Children[1];
                    check(responseText.Bounds.Height >= 30 && responseText.Bounds.Right <= scroller.Viewport.Width + 1 &&
                        signalText.Bounds.Top >= responseText.Bounds.Bottom && scroller.Extent.Height > scroller.Viewport.Height,
                        label + $" wraps long Matrix readouts without overlapping at narrow widths [response={responseText.Bounds}, signal={signalText.Bounds}, viewport={scroller.Viewport}, extent={scroller.Extent}]");
                    var sourceLabel = Field<Forms.Label>("lblGearSource");
                    string originalCitation = sourceLabel.Text;
                    sourceLabel.Text = "A deliberately long sourcebook reference for the selected item, page 255";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var reference = rows.Children.OfType<Button>().Single();
                    var citation = (TextBlock)reference.Content!;
                    check(reference.IsEnabled && citation.Text?.EndsWith(sourceLabel.Text, StringComparison.Ordinal) == true &&
                        citation.Bounds.Height >= 30 && reference.Bounds.Right <= scroller.Viewport.Width + 1,
                        label + " keeps a long source citation in a wrapping button");
                    bool focusedCitation = reference.Focus(NavigationMethod.Tab);
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(focusedCitation && reference.IsFocused && scroller.Offset.Y > 0,
                        label + " scrolls the keyboard-focused source citation into view");
                    sourceLabel.Text = originalCitation;
                    scroller.ClearValue(Control.MaxHeightProperty);
                    scroller.Width = width;
                    response.Text = original;
                    response.Visible = responseVisible;
                    signal.Visible = signalVisible;
                    var nameLabel = Field<Forms.Label>("lblGearName");
                    string originalName = nameLabel.Text;
                    scroller.Width = 145;
                    nameLabel.Text = "A deliberately long name for the selected piece of gear";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var nameText = (TextBlock)rows.Children[0];
                    check(host.Visible && nameText.Text?.EndsWith(nameLabel.Text, StringComparison.Ordinal) == true &&
                        nameText.Bounds.Height >= 30 && rows.Children[1].Bounds.Top >= nameText.Bounds.Bottom,
                        label + " wraps a long gear name above the category");
                    scroller.ClearValue(Control.WidthProperty);
                    nameLabel.Text = originalName;
                    if (career)
                    {
                        var ratingRow = (WrapPanel)rows.Children[2];
                        var cost = (TextBlock)ratingRow.Children[2];
                        var costLabel = Field<Forms.Label>("lblGearCost");
                        string originalCost = costLabel.Text;
                        scroller.Width = 145;
                        costLabel.Text = "A deliberately long cost value for the selected piece of gear, including a detailed explanation of the additional fees, discounts, and adjustments that apply to the final purchase price";
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        check(host.Visible && cost.Text?.EndsWith(costLabel.Text, StringComparison.Ordinal) == true &&
                            cost.Bounds.Height >= 30 && ratingRow.Bounds.Bottom <= rows.Children[3].Bounds.Top &&
                            rows.Children[3].Bounds.Bottom <= rows.Children[4].Bounds.Top,
                            "Career Gear wraps long costs and keeps capacity and quantity reachable");
                        var quantityActions = ((WrapPanel)rows.Children[4]).Children.OfType<Button>().ToArray();
                        check(quantityActions.Length == 5 && quantityActions.All(button =>
                            button.Bounds.Width > 0 && button.Bounds.Right <= scroller.Viewport.Width + 1 &&
                            button.IsEnabled == Field<Forms.Button>(button.Name!["Native".Length..]).Enabled),
                            "Career Gear keeps all quantity and transfer commands within the narrow scrolling pane");
                        scroller.ClearValue(Control.WidthProperty);
                        costLabel.Text = originalCost;
                    }
                }
                if (name is "Foci" or "Metamagic" or "Spells" or "ComplexForms")
                {
                    var workspaceSize = main.ClientSize;
                    try
                    {
                        main.ClientSize = new System.Drawing.Size(workspaceSize.Width, 380);
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        check(host.Bottom <= main.ClientSize.Height && tree.Bounds.Height > 0,
                            label + " keeps its tree within the short workspace");
                        if (name is "Spells" or "ComplexForms")
                        {
                            var entities = Field<Forms.Panel>(name == "Spells" ? "panSpirits" : "panSprites");
                            var entityOrigin = main.PointToClient(entities.PointToScreen(System.Drawing.Point.Empty));
                            check(host.Bottom <= entityOrigin.Y && entities.Height > 0 &&
                                entityOrigin.Y + entities.Height <= main.ClientSize.Height,
                                label + " keeps the lower list inside the short workspace");
                        }
                    }
                    finally
                    {
                        main.ClientSize = workspaceSize;
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    }
                }
                var probe = new Forms.TreeNode { Text = "Synthetic tree root", Tag = name == "Metamagic" ? metamagic.InternalId : "" };
                source.Nodes.Add(probe);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                var row = tree.Items.OfType<TreeViewItem>().SingleOrDefault(item => ReferenceEquals(item.Tag, probe));
                check(row != null, label + " projects an inserted item");
                check(source.Parent == null && host.Visible && tree.IsEffectivelyVisible &&
                    TopLevel.GetTopLevel(tree) != null && tree.Bounds.Height > 0 &&
                    tree.Items.Count == source.Nodes.Count,
                    label + " renders its native rows on the owning tab");
                tree.SelectedItem = row;
                check(source.SelectedNode == probe, label + " routes selection to the editor");
                if (name == "Metamagic")
                {
                    var initiationDetails = responsivePage!.Children.OfType<ScrollViewer>().Single();
                    var initiationRows = (StackPanel)initiationDetails.Content!;
                    var gradeRows = (WrapPanel)initiationRows.Children[0];
                    var group = gradeRows.Children.OfType<CheckBox>().Single(box => box.Name == "NativechkInitiationGroup");
                    var groupSource = Field<Forms.CheckBox>("chkInitiationGroup");
                    var history = initiationRows.Children.OfType<ListBox>().Single();
                    check(host.Visible && gradeRows.Bounds.Height > 0 &&
                        initiationRows.Children.IndexOf(gradeRows) < initiationRows.Children.IndexOf(history) &&
                        gradeRows.Children.OfType<CheckBox>().Count() == 2 && group.IsChecked == groupSource.Checked &&
                        gradeRows.Children.OfType<Button>().Any(button => button.Name == "NativecmdImproveInitiation"),
                        label + " keeps initiation grade controls above the history list");
                    var groupFields = initiationRows.Children.OfType<StackPanel>().Single();
                    var notes = groupFields.Children.OfType<TextBox>().Single(input => input.AcceptsReturn);
                    string originalNotes = notes.Text ?? "";
                    check(host.Visible && notes.IsEnabled && notes.Bounds.Height >= notes.FontSize * 7,
                        label + " displays a multiline initiation group notes field");
                    double baseNotesHeight = notes.Bounds.Height;
                    double enlargedText = notes.FontSize * 1.5;
                    initiationDetails.Width = 165;
                    Avalonia.Controls.Documents.TextElement.SetFontSize(initiationDetails, enlargedText);
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(notes.FontSize == enlargedText && notes.Bounds.Height > baseNotesHeight &&
                        history.FontSize == enlargedText && history.MinHeight == enlargedText * 6 &&
                        gradeRows.Children.Where(child => child.IsVisible).All(child => child.Bounds.Right <= gradeRows.Bounds.Width + 1),
                        label + " keeps grade controls, history, and notes usable at 150% text");
                    initiationDetails.ClearValue(Control.WidthProperty);
                    initiationDetails.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    notes.Text = "First line\nSecond line";
                    check(character.GroupNotes == notes.Text,
                        label + " writes group notes through the original character controller");
                    notes.Text = originalNotes;
                    var reference = ((Grid)choices).Children.OfType<Button>().Single();
                    var citation = (TextBlock)reference.Content!;
                    var sourceLabel = Field<Forms.Label>("lblMetamagicSource");
                    string original = sourceLabel.Text;
                    reference.MaxWidth = 145;
                    sourceLabel.Text = "A deliberately long reference for the selected metamagic, page 132";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(reference.IsEnabled && citation.Bounds.Height >= 30 && reference.Bounds.Top >= tree.Bounds.Bottom,
                        label + " wraps its sourcebook link below the native tree");
                    sourceLabel.Text = original;
                    reference.ClearValue(Control.MaxWidthProperty);
                }
                check(source.CheckBoxes == (row!.Header is CheckBox), label + " preserves the row presentation kind");
                probe.Remove();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(!tree.Items.Contains(row) && !ReferenceEquals(tree.SelectedItem, row),
                    label + " clears stale selection after removal");
                outer.SelectedTab = Field<Forms.TabPage>("tabCommon");
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(!host.Visible, label + " hides when leaving the tab");
            }
            var adept = Field<Forms.TabPage>("tabAdept");
            if (!outer.TabPages.Contains(adept))
                outer.TabPages.Add(adept);
            outer.SelectedTab = adept;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var powerHeader = main.Controls.OfType<Forms.NativeControlHost>()
                .Single(control => control.Name == "NativeAdeptPowerHeader");
            var powerRows = (StackPanel)((Border)powerHeader.NativeControl!).Child!;
            var points = Field<Forms.Label>("lblPowerPoints");
            string existingPoints = points.Text;
            int normalHeight = powerHeader.Height;
            points.Text = string.Join(" ", Enumerable.Repeat("A deliberately long power point total.", 12));
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var powers = Field<Forms.Panel>("panPowers");
            var powerOrigin = main.PointToClient(powers.PointToScreen(System.Drawing.Point.Empty));
            check(powerHeader.Visible && powerHeader.Height > normalHeight &&
                powerRows.Children[0].Bounds.Bottom <= powerRows.Children[1].Bounds.Top && powerHeader.Bottom <= powerOrigin.Y,
                "Adept power totals wrap above Add Power and move the list down");
            points.Text = existingPoints;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var originalWorkspace = main.ClientSize;
            try
            {
                main.ClientSize = new System.Drawing.Size(originalWorkspace.Width, 380);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                powerOrigin = main.PointToClient(powers.PointToScreen(System.Drawing.Point.Empty));
                check(powerHeader.Bottom <= powerOrigin.Y && powers.Height > 0 &&
                    powerOrigin.Y + powers.Height <= main.ClientSize.Height,
                    "Adept Powers keeps its list within the short workspace");
            }
            finally
            {
                main.ClientSize = originalWorkspace;
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            }
            check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
                "Tree-only interaction leaves the editor character unchanged");
            var sprites = Field<Forms.Panel>("panSprites");
            var first = new Spirit(character) { EntityType = SpiritType.Sprite };
            var second = new Spirit(character) { EntityType = SpiritType.Sprite };
            var firstRow = new SpiritControl(career) { SpiritObject = first, Height = 38 };
            var secondRow = new SpiritControl(career) { SpiritObject = second, Height = 26 };
            character.Spirits.Add(first);
            character.Spirits.Add(second);
            sprites.Controls.Add(firstRow);
            sprites.Controls.Add(secondRow);
            outer.SelectedTab = Field<Forms.TabPage>("tabTechnomancer");
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var nativeActions = main.Controls.OfType<Forms.NativeControlHost>()
                .Where(control => control.Name == "NativeSpriteActions" && control.Visible).ToArray();
            check(nativeActions.Length >= 2 && nativeActions.All(host =>
            {
                var actions = (WrapPanel)((Border)host.NativeControl!).Child!;
                return actions.Children.OfType<CheckBox>().Single().IsEnabled == career &&
                    actions.Children.OfType<Button>().Any(button => button.Name == "NativecmdDelete");
            }), "Sprite rows expose native Bound and Delete controls");
            var secondOrigin = main.PointToClient(secondRow.PointToScreen(System.Drawing.Point.Empty));
            var secondSurface = (Border)nativeActions.Single(host => host.Top >= secondOrigin.Y &&
                host.Top < secondOrigin.Y + secondRow.Height).NativeControl!;
            var secondActions = (WrapPanel)secondSurface.Child!;
            var nameInput = secondActions.Children.OfType<TextBox>().Single();
            check(secondActions.Children.OfType<Button>().Any(button => button.Name == "NativeSpriteLink") &&
                secondActions.Children.OfType<Button>().Any(button => button.Name == "NativeSpriteNotes"),
                "Sprite link and notes share the native wrapping row");
            secondRow.ForceChanged += _ => { };
            nameInput.Text = "A sprite with a custom name";
            check(second.CritterName == "A sprite with a custom name" && first.CritterName != second.CritterName,
                "Editing the native sprite name updates only the associated sprite");
            var services = secondActions.Children.OfType<StackPanel>().SelectMany(group => group.Children)
                .OfType<NumericUpDown>().Single(input => input.Name == "nativenudServices");
            secondRow.ServicesOwedChanged += _ => { };
            services.Value = 3;
            check(second.ServicesOwed == 3 && first.ServicesOwed != 3,
                "Editing native sprite services updates only the associated sprite");
            var type = secondActions.Children.OfType<ComboBox>().Single();
            var typeSource = (Forms.ComboBox)typeof(SpiritControl)
                .GetField("cboSpiritName", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(secondRow)!;
            typeSource.DisplayMember = "Name";
            typeSource.ValueMember = "Value";
            typeSource.DataSource = new[] { new ListItem { Name = "Machine Sprite", Value = "Machine Sprite" },
                new ListItem { Name = "Data Sprite", Value = "Data Sprite" } };
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            type.SelectedIndex = 1;
            check(type.Items.Count == 2 && second.Name == "Data Sprite" && first.Name != second.Name,
                "Selecting a native sprite type updates only the associated sprite");
            int originalWidth = sprites.Width;
            int originalHeight = secondRow.Height;
            var originalMaximum = sprites.MaximumSize;
            sprites.MaximumSize = new System.Drawing.Size(265, originalMaximum.Height > 0 ? originalMaximum.Height : int.MaxValue);
            sprites.Width = 265;
            for (int attempt = 0; attempt < 30; attempt++)
            {
                sprites.ScrollControlIntoView(secondRow);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                if (secondActions.IsEffectivelyVisible && secondActions.Bounds.Width is > 0 and <= 265)
                    break;
                await Task.Delay(20);
            }
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var spriteName = secondActions.Children.OfType<TextBox>().Single();
            check(secondRow.Width <= sprites.ClientSize.Width && secondRow.Height >= originalHeight &&
                Math.Abs(type.Bounds.Width - spriteName.Bounds.Width) <= 1 && type.Bounds.Bottom <= spriteName.Bounds.Top &&
                spriteName.Bounds.Width <= secondActions.Bounds.Width + 1 && secondRow.Top == firstRow.Bottom,
                $"Narrow sprite rows stack Type and Name without overlapping the next row [height={originalHeight}/{secondRow.Height}, type={type.Bounds}, name={spriteName.Bounds}, row={secondActions.Bounds}, pane={sprites.Bounds}, scroll={sprites.AutoScrollPosition}, visible={secondActions.IsEffectivelyVisible}]");
            sprites.AutoScrollPosition = System.Drawing.Point.Empty;
            sprites.MaximumSize = originalMaximum;
            sprites.Width = originalWidth;
            check(secondRow.Top == firstRow.Bottom,
                "Sprite rows follow the actual height of the preceding row");
            bool confirm = character.Options.ConfirmDelete;
            try
            {
                character.Options.ConfirmDelete = false;
                editor.GetType().GetMethod("objSprite_DeleteSpirit", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(editor, [firstRow]);
                check(!character.Spirits.Contains(first) && character.Spirits.Contains(second) &&
                    secondRow.Parent == sprites && secondRow.Top == firstRow.Top,
                    "Deleting a sprite reflows the list and preserves the following sprite");
            }
            finally
            {
                character.Options.ConfirmDelete = confirm;
                editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .SetValue(editor, false);
            }
            editor.Close();
        }
    }
}
