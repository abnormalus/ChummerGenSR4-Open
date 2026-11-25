#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Input;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeEditorLayoutChecks
{
    internal static async Task RunAsync(frmMain owner, Character fixture, Action<bool, string> check, string? captures = null)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character();
            character.LoadSnapshot(fixture.CaptureSnapshot());
            character.Created = career;
            if (career)
            {
                var testSkill = character.Skills.First(skill => !skill.KnowledgeSkill && !skill.IsMeta && !skill.ExoticSkill);
                testSkill.Rating = 2;
                testSkill.SkillGroup = "";
                testSkill.IsGrouped = false;
            }
            var quality = new Quality(character)
            {
                Name = "Synthetic responsive quality", Source = "SR4", BP = 3,
                Page = string.Join(" ", Enumerable.Repeat("Long translated quality citation", 20))
            };
            character.Qualities.Add(quality);
            var firstPet = new Contact(character) { Name = "First pet", EntityType = ContactType.Pet };
            var secondPet = new Contact(character) { Name = "Second pet", EntityType = ContactType.Pet };
            character.Contacts.Add(firstPet);
            character.Contacts.Add(secondPet);
            using Forms.Form editor = career ? new frmCareer(character) : new frmCreate(character);
            editor.MdiParent = owner;
            editor.WindowState = Forms.FormWindowState.Maximized;
            editor.Show();
            try
            {
                await Idle();
                var sections = Field<Forms.TabControl>(editor, "tabCharacterTabs");
                var nativeSections = Tabs(sections);
                AssertTabs(sections, nativeSections, "editor sections");
                var originalSizeForSummary = owner.ClientSize;
                var mainSplit = Field<Forms.SplitContainer>(editor, "splitMain");
                var summaryHeader = (Grid)Host("Native" + sections.Name).NativeControl!;
                var toggle = summaryHeader.Children.OfType<Button>().Single();
                owner.ClientSize = new System.Drawing.Size(700, 500);
                for (int attempt = 0; attempt < 20 && !mainSplit.Panel2Collapsed; attempt++)
                    await Task.Delay(20);
                await Idle();
                check(toggle.IsEffectivelyVisible && mainSplit.Panel2Collapsed &&
                    mainSplit.Panel1.Width >= mainSplit.ClientSize.Width - 1 && TabViewport(sections).Bounds.Right <= toggle.Bounds.Left,
                    Label($"narrow windows hide the summary, give its width back to the sheet, and keep the toggle clear of tabs [owner={owner.ClientSize}, split={mainSplit.ClientSize}, pane={mainSplit.Panel1.Width}, collapsed={mainSplit.Panel2Collapsed}, toggle={toggle.IsEffectivelyVisible}/{toggle.Bounds}, viewport={TabViewport(sections).Bounds}]"));
                toggle.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                await Idle();
                check(!mainSplit.Panel2Collapsed && mainSplit.Panel2.Visible && mainSplit.Panel2.Width >= 150,
                    Label("the summary can be reopened in a narrow window"));
                owner.ClientSize = originalSizeForSummary;
                await Idle();
                var original = sections.SelectedTab!;
                using var conditional = new Forms.TabPage("Synthetic conditional section " + new string('W', 160));
                sections.TabPages.Add(conditional);
                await Idle();
                var header = nativeSections.Items.OfType<TabItem>().Single(item => ReferenceEquals(item.Tag, conditional));
                var scroll = TabViewport(sections);
                var bar = scroll.GetVisualDescendants().OfType<ScrollBar>()
                    .Single(control => control.Orientation == Avalonia.Layout.Orientation.Horizontal);
                int standardHeight = Host("Native" + sections.Name).Height;
                double standardFont = header.FontSize;
                Avalonia.Controls.Documents.TextElement.SetFontSize(scroll, standardFont * 2.25);
                await Idle();
                check(header.FontSize == standardFont * 2.25 && Host("Native" + sections.Name).Height > standardHeight &&
                    original.Top >= Host("Native" + sections.Name).Height,
                    Label($"larger tab text grows the strip and moves the page down [font={standardFont}/{header.FontSize}, scrollFont={Avalonia.Controls.Documents.TextElement.GetFontSize(scroll)}, strip={standardHeight}/{Host("Native" + sections.Name).Height}, page={original.Top}]"));
                var tabTop = header.TranslatePoint(default, scroll);
                var barTop = bar.TranslatePoint(default, scroll);
                check(bar.IsEffectivelyVisible && bar.Bounds.Height > 0 && tabTop.HasValue && barTop.HasValue &&
                    tabTop.Value.Y + header.Bounds.Height <= barTop.Value.Y,
                    Label($"overflow scrollbar sits below the clickable tabs [visible={bar.IsEffectivelyVisible}, bar={barTop}/{bar.Bounds}, tab={tabTop}/{header.Bounds}, extent={scroll.Extent}, viewport={scroll.Viewport}]"));
                scroll.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                await Idle();
                conditional.Text = "Renamed conditional section";
                nativeSections.SelectedItem = header;
                await Idle();
                check(sections.SelectedTab == conditional && conditional.Visible && (string?)header.Header == conditional.Text,
                    Label("conditional tabs appear, rename and select through the native header"));
                sections.SelectedTab = original;
                sections.TabPages.Remove(conditional);
                await Idle();
                check(!nativeSections.Items.Contains(header) && ((TabItem?)nativeSections.SelectedItem)?.Tag == original,
                    Label("programmatic selection and conditional tab removal update the native strip"));

                sections.SelectedTab = Field<Forms.TabPage>(editor, "tabCommon");
                await Idle();
                var aliasHost = Host("NativeCommonAlias");
                var aliasLayout = (Grid)aliasHost.NativeControl!;
                var alias = aliasLayout.Children.OfType<TextBox>().Single();
                var randomize = aliasLayout.Children.OfType<Avalonia.Controls.SplitButton>().Single();
                string priorAlias = character.Alias;
                check(alias.IsEffectivelyVisible && alias.Bounds.Width > 40 &&
                    alias.Bounds.Right <= randomize.Bounds.Left && randomize.Bounds.Right <= aliasLayout.Bounds.Width &&
                    randomize.Flyout is MenuFlyout options && options.Items.Count == 3,
                    Label("Common alias input and split-button menu fit the measured row"));
                alias.Text = "Synthetic alias edit";
                check(character.Alias == "Synthetic alias edit", Label("Native alias input updates the character"));
                alias.Text = priorAlias;
                randomize.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Avalonia.Controls.SplitButton.ClickEvent));
                check(!string.IsNullOrWhiteSpace(alias.Text) && alias.Text != priorAlias && character.Alias == alias.Text,
                    Label("Native randomization updates the alias input and character model"));
                var undoAlias = ((MenuFlyout)randomize.Flyout!).Items.OfType<MenuItem>().Last();
                check(undoAlias.IsEnabled, Label("Randomizing an alias enables the native Undo command"));
                undoAlias.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
                check(alias.Text == priorAlias && character.Alias == priorAlias && !undoAlias.IsEnabled,
                    Label("The native Undo command restores the previous alias"));

                var aliasCaption = Field<Forms.Label>(editor, "lblAlias");
                string originalCaption = aliasCaption.Text;
                try
                {
                    aliasLayout.MaxWidth = 220;
                    aliasCaption.Text = "A long translated alias caption";
                    await Idle();
                    var aliasLabel = aliasLayout.Children.OfType<TextBlock>().Single();
                    check(alias.IsEffectivelyVisible && alias.Bounds.Width >= 90 &&
                        aliasLabel.Bounds.Right <= alias.Bounds.Left && alias.Bounds.Right <= randomize.Bounds.Left &&
                        randomize.Bounds.Right <= aliasLayout.Bounds.Width &&
                        Equals(ToolTip.GetTip(aliasLabel), aliasCaption.Text),
                        Label($"long Alias captions preserve the input and menu at 220 logical pixels [caption={aliasLabel.Bounds}, input={alias.Bounds}, menu={randomize.Bounds}]"));
                }
                finally
                {
                    aliasLayout.MaxWidth = double.PositiveInfinity;
                    aliasCaption.Text = originalCaption;
                    await Idle();
                }

                var metatypeHost = Host("NativeCommonMetatype");
                var metatypeScroll = (ScrollViewer)metatypeHost.NativeControl!;
                var metatypeLayout = (Grid)metatypeScroll.Content!;
                var identity = (TextBlock)metatypeLayout.Children[0];
                var citation = (Button)metatypeLayout.Children[1];
                var controller = Field<Forms.Label>(editor, "lblMetatype");
                var sourceController = Field<Forms.Label>(editor, "lblMetatypeSource");
                var typeCaption = Field<Forms.Label>(editor, "lblMetatypeLabel");
                var citationCaption = Field<Forms.Label>(editor, "lblMetatypeSourceLabel");
                string originalTypeCaption = typeCaption.Text;
                string originalCitationCaption = citationCaption.Text;
                string originalMetatype = controller.Text;
                string originalSource = sourceController.Text;
                var originalWorkspace = owner.ClientSize;
                try
                {
                    metatypeScroll.MaxWidth = 145;
                    controller.Text = string.Join(" ", Enumerable.Repeat("Long translated metatype name", 12));
                    sourceController.Text = "A long translated source citation that also needs to wrap";
                    await Idle();
                    check(identity.IsEffectivelyVisible && identity.Bounds.Bottom <= citation.Bounds.Top &&
                        citation.Bounds.Bottom <= metatypeLayout.Bounds.Height && citation.IsEnabled &&
                        identity.Text!.Contains(controller.Text, StringComparison.Ordinal) &&
                        ((TextBlock)citation.Content!).Text!.Contains(sourceController.Text, StringComparison.Ordinal) &&
                        metatypeScroll.Bounds.Width is > 0 and <= 145 && metatypeScroll.Extent.Height > metatypeScroll.Viewport.Height &&
                        aliasHost.Right <= metatypeHost.Left,
                        Label($"Common metatype and citation wrap in a narrow pane beside Alias [workspace={owner.ClientSize}, pane={metatypeScroll.Bounds}]"));
                    citation.Focus();
                    await Idle();
                    var citationTop = citation.TranslatePoint(default, metatypeScroll);
                    check(metatypeScroll.Offset.Y > 0 && citationTop.HasValue &&
                        citationTop.Value.Y < metatypeScroll.Viewport.Height &&
                        citationTop.Value.Y + citation.Bounds.Height > 0,
                        Label("Keyboard focus scrolls the Common source button into view"));
                    int initialMetatypeHeight = metatypeHost.Height;
                    typeCaption.Text = "Translated metatype heading";
                    citationCaption.Text = "Translated source heading";
                    owner.ClientSize = new System.Drawing.Size(owner.ClientSize.Width, owner.ClientSize.Height - 80);
                    await Task.Delay(100);
                    await Idle();
                    check(metatypeHost.Height > 0 && metatypeHost.Height < initialMetatypeHeight &&
                        identity.Text!.StartsWith(typeCaption.Text, StringComparison.Ordinal) &&
                        ((TextBlock)citation.Content!).Text!.StartsWith(citationCaption.Text, StringComparison.Ordinal) &&
                        (career || metatypeHost.Bottom <= Host("nativeCreationMoney").Top),
                        Label("Short Common metatype viewport follows live captions and leaves space below"));
                }
                finally
                {
                    metatypeScroll.ClearValue(Control.MaxWidthProperty);
                    typeCaption.Text = originalTypeCaption;
                    citationCaption.Text = originalCitationCaption;
                    controller.Text = originalMetatype;
                    sourceController.Text = originalSource;
                    owner.ClientSize = originalWorkspace;
                    await Idle();
                }

                var qualities = Host(career ? "NativeCareerQualities" : "NativeCreationQualities");
                var qualityLayout = (DockPanel)qualities.NativeControl!;
                var qualityTree = qualityLayout.Children.OfType<TreeView>().Single();
                var qualityFooter = qualityLayout.Children.OfType<ScrollViewer>().Single();
                var qualitySource = Field<Forms.Label>(editor, "lblQualitySource");
                var qualityController = Field<Forms.TreeView>(editor, "treQualities");
                var addQuality = Field<Forms.Button>(editor, "cmdAddQuality");
                string originalAddCaption = addQuality.Text;
                var qualityActions = qualityLayout.Children.OfType<WrapPanel>().Single();
                var addQualityView = qualityActions.Children.OfType<Button>().Single(button => button.Name == "NativecmdAddQuality");
                try
                {
                    owner.ClientSize = new System.Drawing.Size(660, 500);
                    await Idle();
                    qualityController.SelectedNode = qualityController.Nodes.Cast<Forms.TreeNode>()
                        .SelectMany(root => root.Nodes.Cast<Forms.TreeNode>())
                        .Single(node => Equals(node.Tag, quality.InternalId));
                    await Idle();
                    var page = Field<Forms.TabPage>(editor, "tabCommon");
                    var viewport = InventoryTreeLayout.VisiblePageSize(owner, page);
                    var qualityButton = ((WrapPanel)qualityFooter.Content!).Children.OfType<Button>().Single();
                    check(qualities.Height <= viewport.Height && qualityFooter.IsEffectivelyVisible && qualityTree.Bounds.Height > 80 &&
                        qualityTree.Bounds.Bottom <= qualityFooter.Bounds.Top &&
                        qualityFooter.Extent.Height > qualityFooter.Viewport.Height &&
                        qualityButton.Bounds.Right <= qualityFooter.Viewport.Width + 1,
                        Label($"short Common keeps the quality tree and wrapped source reachable [tree={qualityTree.Bounds} root={qualityLayout.Bounds}, footer={qualityFooter.Bounds} visible={qualityFooter.IsVisible}/{qualityFooter.IsEffectivelyVisible} source={qualitySource.Text.Length}, citation={qualityButton.Bounds}, viewport={qualityFooter.Viewport}, extent={qualityFooter.Extent}]"));
                    var app = Application.Current!;
                    var savedTheme = app.RequestedThemeVariant;
                    try
                    {
                        app.RequestedThemeVariant = Avalonia.Styling.ThemeVariant.Dark;
                        await Task.Delay(150);
                        await Idle();
                        if (career)
                        {
                            var attributes = Field<Forms.Panel>(editor, "panAttributes");
                            check(metatypeHost.Width >= 145 && aliasHost.Right <= metatypeHost.Left &&
                                qualities.Right <= owner.PointToClient(attributes.PointToScreen(System.Drawing.Point.Empty)).X,
                                Label("Narrow Career Common reserves readable width for Metatype without covering Attributes"));
                            var contactDivider = Host("NativesplitContacts");
                            var enemyHeading = Host("NativeCareerEnemiesHeader");
                            var contactHeading = Host("NativeCareerContactsHeader");
                            var thumb = (Thumb)contactDivider.NativeControl!;
                            var nativeWindow = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
                            var enemyView = (Control)enemyHeading.NativeControl!;
                            var contactView = (Control)contactHeading.NativeControl!;
                            var baseline = owner.ClientSize;
                            int frameCount = 0;
                            var dividerPositions = new System.Collections.Generic.HashSet<double>();
                            try
                            {
                                foreach (int height in new[] { 560, 500, 540, 500 })
                                {
                                    owner.ClientSize = new System.Drawing.Size(660, height);
                                    bool observedHeight = false;
                                    for (int frame = 0; frame < 20; frame++)
                                    {
                                        var rendered = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                                        nativeWindow.RequestAnimationFrame(_ => rendered.TrySetResult());
                                        await rendered.Task.WaitAsync(TimeSpan.FromSeconds(2));
                                        var dividerPosition = thumb.TranslatePoint(default, nativeWindow);
                                        var contactsPosition = contactView.TranslatePoint(default, nativeWindow);
                                        var enemiesPosition = enemyView.TranslatePoint(default, nativeWindow);
                                        if (thumb.IsEffectivelyVisible)
                                        {
                                            if (!dividerPosition.HasValue || !contactsPosition.HasValue || !enemiesPosition.HasValue ||
                                                dividerPosition.Value.Y + thumb.Bounds.Height > enemiesPosition.Value.Y + 1 ||
                                                dividerPosition.Value.Y + 1 < contactsPosition.Value.Y + contactView.Bounds.Height)
                                                check(false, Label($"Resize frame crosses a heading [height={height}, frame={frame}, divider={dividerPosition}/{thumb.Bounds.Height}, contacts={contactsPosition}/{contactView.Bounds.Height}, enemies={enemiesPosition}]"));
                                            if (dividerPosition.HasValue) dividerPositions.Add(dividerPosition.Value.Y);
                                            frameCount++;
                                        }
                                        observedHeight |= Math.Abs(nativeWindow.ClientSize.Height - height) <= 1;
                                        if (frame >= 9 && observedHeight) break;
                                        await Task.Delay(10);
                                    }
                                    check(observedHeight, Label($"Resize sequence reaches native client height {height}"));
                                }
                                check(frameCount >= 16 && dividerPositions.Count > 1,
                                    Label($"Resize frames keep the moving divider clear of both headings [frames={frameCount}, positions={string.Join(", ", dividerPositions)}]"));
                            }
                            finally
                            {
                                owner.ClientSize = baseline;
                                await Idle();
                            }
                        }
                        CaptureCorner("normal");
                        double enlarged = addQualityView.FontSize * 1.5;
                        Avalonia.Controls.Documents.TextElement.SetFontSize(qualityLayout, enlarged);
                        addQuality.Text = string.Join(" ", Enumerable.Repeat("Translated command to add a quality", 3));
                        await Idle();
                        var text = (TextBlock)addQualityView.Content!;
                        check(text.FontSize == enlarged && text.Text == addQuality.Text && text.Bounds.Height > enlarged * 2 &&
                            text.TextLayout.TextLines.Count <= 3 && Equals(ToolTip.GetTip(addQualityView), text.Text) &&
                            addQualityView.Bounds.Right <= qualityActions.Bounds.Width + 1 &&
                            qualityActions.Bounds.Bottom <= qualityTree.Bounds.Top && qualityTree.Bounds.Height > 40,
                            Label($"Top-left Common quality toolbar wraps without covering the tree [text={text.Bounds}, button={addQualityView.Bounds}, toolbar={qualityActions.Bounds}, tree={qualityTree.Bounds}]"));
                        var selectedItem = (TreeViewItem)qualityTree.SelectedItem!;
                        var selectedCaption = selectedItem.GetVisualDescendants().OfType<TextBlock>()
                            .Single(caption => Equals(caption.Text, selectedItem.Header));
                        selectedItem.BringIntoView();
                        await Idle();
                        var captionPosition = selectedCaption.TranslatePoint(default, qualityTree);
                        check(selectedCaption.FontSize == enlarged && captionPosition.HasValue &&
                            captionPosition.Value.X + selectedCaption.Bounds.Width <= qualityTree.Bounds.Width + 1 &&
                            captionPosition.Value.Y >= -1 && captionPosition.Value.Y + selectedCaption.Bounds.Height <= qualityTree.Bounds.Height + 1 &&
                            selectedCaption.Bounds.Height >= enlarged * 2,
                            Label($"Selected quality wraps inside the Common tree [caption={selectedCaption.Bounds}, position={captionPosition}, tree={qualityTree.Bounds}]"));
                        await Task.Delay(80);
                        await Idle();
                        CaptureCorner("enlarged");
                    }
                    finally
                    {
                        app.RequestedThemeVariant = savedTheme;
                        addQuality.Text = originalAddCaption;
                        qualityLayout.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    }

                    void CaptureCorner(string scenario)
                    {
                        if (captures == null) return;
                        var window = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner);
                        window.UpdateLayout();
                        using var bitmap = new Avalonia.Media.Imaging.RenderTargetBitmap(
                            new PixelSize((int)Math.Ceiling(window.ClientSize.Width * 2), (int)Math.Ceiling(window.ClientSize.Height * 2)),
                            new Vector(192, 192));
                        bitmap.Render(window);
                        bitmap.Save(System.IO.Path.Combine(captures, $"common-top-left-{(career ? "career" : "creation")}-{scenario}.png"),
                            Avalonia.Media.Imaging.PngBitmapEncoderOptions.Default);
                    }
                }
                finally
                {
                    qualityController.SelectedNode = null;
                    owner.ClientSize = originalWorkspace;
                    await Idle();
                }

                sections.SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
                await Idle();
                var gear = Field<Forms.TabControl>(editor, "tabStreetGearTabs");
                var nativeGear = Tabs(gear);
                AssertTabs(gear, nativeGear, "Street Gear sections");
                var gearHeader = nativeGear.Items.OfType<TabItem>().First(item => item.Tag is Forms.TabPage page && page != gear.SelectedTab);
                nativeGear.SelectedItem = gearHeader;
                await Idle();
                check(ReferenceEquals(gear.SelectedTab, gearHeader.Tag), Label("native Street Gear tabs switch the visible page"));

                var information = Field<Forms.TabControl>(editor, "tabInfo");
                var nativeInformation = Tabs(information);
                AssertTabs(information, nativeInformation, "character information sections");
                var details = nativeInformation.Items.OfType<TabItem>().First(item => item.Tag != information.SelectedTab);
                nativeInformation.SelectedItem = details;
                await Idle();
                check(ReferenceEquals(details.Tag, information.SelectedTab), Label("native information tabs switch the visible page"));

                if (!career)
                {
                    information.SelectedTab = Field<Forms.TabPage>(editor, "tabBPSummary");
                    await Idle();
                    var points = (ScrollViewer)Host("NativeBuildPointSummary").NativeControl!;
                    var rows = ((StackPanel)points.Content!).Children.OfType<Grid>().ToArray();
                    check(points.IsEffectivelyVisible && rows.Length == 18 &&
                        rows[0].Children[1].Name == "NativelblMetatypeBP" &&
                        rows[^1].Children[1].Name == "NativelblManeuversBP",
                        "Creation shows all build-point summary rows in their original order");
                    var initiation = rows.Single(row => row.Children[1].Name == "NativelblInitiationBP");
                    var caption = (TextBlock)initiation.Children[0];
                    var amount = (TextBlock)initiation.Children[1];
                    var captionSource = Field<Forms.Label>(editor, "lblBuildInitiation");
                    var valueSource = Field<Forms.Label>(editor, "lblInitiationBP");
                    string originalInitiationCaption = captionSource.Text, originalValue = valueSource.Text;
                    try
                    {
                        points.MaxWidth = 145;
                        double enlarged = caption.FontSize * 1.5;
                        Avalonia.Controls.Documents.TextElement.SetFontSize(points, enlarged);
                        captionSource.Text = "A translated caption for initiation and submersion";
                        valueSource.Text = "125 Karma";
                        await Idle();
                        amount.BringIntoView();
                        await Idle();
                        check(caption.Text == captionSource.Text && amount.Text == valueSource.Text &&
                            caption.FontSize == enlarged && caption.Bounds.Bottom <= amount.Bounds.Top &&
                            amount.Bounds.Width <= initiation.Bounds.Width && points.Offset.Y > 0,
                            "Creation build-point summary wraps translated captions and scrolls to live totals at 150% text");
                    }
                    finally
                    {
                        captionSource.Text = originalInitiationCaption;
                        valueSource.Text = originalValue;
                        points.ClearValue(Control.MaxWidthProperty);
                        points.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    }
                }

                information.SelectedTab = Field<Forms.TabPage>(editor, "tabOtherInfo");
                await Idle();
                var summary = (ScrollViewer)Host("NativeCharacterSummary").NativeControl!;
                var summaryRows = (StackPanel)summary.Content!;
                var movementLabel = Field<Forms.Label>(editor, "lblMovementLabel");
                string originalMovementLabel = movementLabel.Text;
                try
                {
                    summary.MaxWidth = 175;
                    summary.MaxHeight = 120;
                    movementLabel.Text = string.Join(" ", Enumerable.Repeat("Translated movement label", 3));
                    await Idle();
                    var movement = summaryRows.Children.OfType<Grid>().Single(row =>
                        row.Children.OfType<TextBlock>().Any(text => text.Name == "NativelblMovement"));
                    var caption = (TextBlock)movement.Children[0];
                    var value = (TextBlock)movement.Children[1];
                    value.BringIntoView();
                    await Idle();
                    check(movement.IsEffectivelyVisible && caption.Text == movementLabel.Text &&
                        caption.Bounds.Bottom <= value.Bounds.Top && value.Bounds.Width <= movement.Bounds.Width &&
                        summary.Offset.Y > 0 && value.TranslatePoint(default, summary) is { } position &&
                        position.Y >= -1 && position.Y + value.Bounds.Height <= summary.Viewport.Height + 1,
                        Label($"Narrow Other Info stacks long text and scrolls to Movement [caption={caption.Bounds}, value={value.Bounds}, offset={summary.Offset}]"));
                }
                finally
                {
                    summary.ClearValue(Control.MaxWidthProperty);
                    summary.ClearValue(Control.MaxHeightProperty);
                    movementLabel.Text = originalMovementLabel;
                    await Idle();
                }

                if (career)
                {
                    information.SelectedTab = Field<Forms.TabPage>(editor, "tabConditionMonitor");
                    await Idle();
                    var monitor = (ScrollViewer)Host("NativeCareerConditionMonitor").NativeControl!;
                    var physical = monitor.GetVisualDescendants().OfType<WrapPanel>().Single(track => track.Name == "NativepanPhysicalCM");
                    var boxes = physical.Children.OfType<ToggleButton>().Where(box => box.IsVisible).ToArray();
                    check(boxes.Length >= 3 && boxes.All(box => box.Bounds.Right <= physical.Bounds.Width + 1) &&
                        monitor.IsEffectivelyVisible, Label("Condition Monitor wraps native damage boxes within the sidebar"));
                    int previousDamage = character.PhysicalCMFilled;
                    boxes[2].IsChecked = true;
                    check(character.PhysicalCMFilled >= 3 && boxes.Take(3).All(box => box.IsChecked == true),
                        Label("Toggling a native physical damage box fills the preceding boxes"));
                    boxes[0].IsChecked = false;
                    check(character.PhysicalCMFilled == 0 && boxes.All(box => box.IsChecked == false),
                        Label("Clearing the first native physical damage box clears the entire track"));
                    int overflowIndex = character.PhysicalCM;
                    if (overflowIndex < boxes.Length)
                        check(boxes[overflowIndex].Opacity < boxes[0].Opacity &&
                            Avalonia.Automation.AutomationProperties.GetName(boxes[overflowIndex])!
                                .Contains(LanguageManager.Instance.GetString("String_SpellOverflowDamage"), StringComparison.Ordinal),
                            Label("Physical overflow remains visually distinct and has an accessible label"));

                    var stun = monitor.GetVisualDescendants().OfType<WrapPanel>().Single(track => track.Name == "NativepanStunCM");
                    var stunBoxes = stun.Children.OfType<ToggleButton>().Where(box => box.IsVisible).ToArray();
                    int previousStun = character.StunCMFilled;
                    stunBoxes[0].IsChecked = false;
                    stunBoxes[2].IsChecked = true;
                    check(character.StunCMFilled == 3 && stunBoxes.Take(3).All(box => box.IsChecked == true),
                        Label("Native Stun damage fills the correct track"));
                    stunBoxes[0].IsChecked = false;
                    if (previousStun > 0 && previousStun <= stunBoxes.Length)
                        stunBoxes[previousStun - 1].IsChecked = true;
                    if (previousDamage > 0 && previousDamage <= boxes.Length)
                        boxes[previousDamage - 1].IsChecked = true;
                }

                var splitters = new[] { "splitMain", "splitContacts", "splitSkills" };
                if (career)
                    splitters = splitters.Append("splitKarmaNuyen").ToArray();
                foreach (string name in splitters)
                {
                    var source = Field<Forms.SplitContainer>(editor, name);
                    if (source.Parent is Forms.TabPage tab)
                        sections.SelectedTab = tab;
                    await Idle();
                    var host = Host("Native" + name);
                    var thumb = (Thumb)host.NativeControl!;
                    check(thumb.IsEffectivelyVisible && host.Width == source.SplitterRectangle.Width &&
                        host.Height == source.SplitterRectangle.Height,
                        Label(name + " displays the native handle in the splitter gutter"));
                    int before = source.SplitterDistance;
                    bool vertical = source.Orientation == Forms.Orientation.Vertical;
                    Press(vertical ? Key.Right : Key.Down);
                    if (before == source.SplitterDistance)
                        Press(vertical ? Key.Left : Key.Up);
                    await Idle();
                    check(before != source.SplitterDistance &&
                        (vertical ? source.Panel1.Width : source.Panel1.Height) == source.SplitterDistance,
                        Label(name + " keyboard input resizes the existing pane"));
                    source.SplitterDistance = before;
                    await Idle();
                    if (name == "splitContacts")
                    {
                        var contactsHeader = Host(career ? "NativeCareerContactsHeader" : "NativeCreationContactsHeader");
                        var enemiesHeader = Host(career ? "NativeCareerEnemiesHeader" : "NativeCreationEnemiesHeader");
                        var contacts = Field<Forms.Panel>(editor, "panContacts");
                        var enemies = Field<Forms.Panel>(editor, "panEnemies");
                        var caption = Field<Forms.Label>(editor, "lblContacts");
                        string text = caption.Text;
                        caption.Text = string.Join(" ", Enumerable.Repeat("A long contacts heading", 5));
                        await Idle();
                        var contactsOrigin = owner.PointToClient(contacts.PointToScreen(System.Drawing.Point.Empty));
                        var enemiesOrigin = owner.PointToClient(enemies.PointToScreen(System.Drawing.Point.Empty));
                        check(contactsHeader.Visible && enemiesHeader.Visible && contactsHeader.Height > 0 &&
                            contactsHeader.Bottom <= contactsOrigin.Y && enemiesHeader.Bottom <= enemiesOrigin.Y &&
                            contacts.Height > 0 && enemies.Height > 0,
                            Label($"Contacts and Enemies headers reserve room for their lists [split={source.Bounds}, headers={contactsHeader.Bounds}/{enemiesHeader.Bounds}, lists={contactsOrigin}/{enemiesOrigin} {contacts.Height}/{enemies.Height}]"));
                        caption.Text = text;
                        await Idle();
                        var contactPage = Field<Forms.TabPage>(editor, "tabCommon");
                        var contactViewport = InventoryTreeLayout.VisiblePageSize(owner, contactPage);
                        check(source.Right <= contactViewport.Width && source.Bottom <= contactViewport.Height,
                            Label($"Contacts splitter fits the visible Common page [split={source.Bounds}, viewport={contactViewport}]"));
                        if (!career)
                        {
                            var initialSize = owner.ClientSize;
                            try
                            {
                                owner.ClientSize = new System.Drawing.Size(660, 500);
                                await Idle();
                                var moneyHost = Host("nativeCreationMoney");
                                var moneyInput = ((Grid)moneyHost.NativeControl!).Children.OfType<NumericUpDown>().Single();
                                check(moneyInput.Bounds.Width >= 90 && metatypeHost.Width >= 145 && moneyHost.Width == metatypeHost.Width &&
                                    source.Left > Field<Forms.Panel>(editor, "panAttributes").Right,
                                    Label($"compact Common keeps metatype, starting money and contacts beside Attributes [metatype={metatypeHost.Bounds}, money={moneyHost.Bounds}, contacts={source.Bounds}]"));
                                var moneyCaption = Field<Forms.Label>(editor, "lblNuyen");
                                string originalMoneyCaption = moneyCaption.Text;
                                try
                                {
                                    var moneyGrid = (Grid)moneyHost.NativeControl!;
                                    var label = moneyGrid.Children.OfType<StackPanel>().Single().Children.OfType<TextBlock>().First();
                                    int initialMoneyHeight = moneyHost.Height;
                                    moneyCaption.Text = "Translated starting money allocation for this character";
                                    Avalonia.Controls.Documents.TextElement.SetFontSize(moneyGrid, label.FontSize * 1.5);
                                    await Idle();
                                    int contactsTop = owner.PointToClient(source.PointToScreen(System.Drawing.Point.Empty)).Y;
                                    check(label.Text == moneyCaption.Text && Equals(ToolTip.GetTip(label), moneyCaption.Text) &&
                                        moneyHost.Height > initialMoneyHeight && moneyHost.Bottom < contactsTop &&
                                        moneyGrid.Children.All(child => child.Bounds.Bottom <= moneyGrid.Bounds.Height + 1),
                                        Label("Starting Money wraps a translated caption and clears Contacts at 150% text"));
                                    moneyGrid.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                                }
                                finally
                                {
                                    moneyCaption.Text = originalMoneyCaption;
                                    await Idle();
                                }
                                owner.ClientSize = new System.Drawing.Size(600, 500);
                                await Idle();
                                var viewport = InventoryTreeLayout.VisiblePageSize(owner, contactPage);
                                var attributes = Field<Forms.Panel>(editor, "panAttributes");
                                var attributeScroll = (ScrollViewer)Host("nativeCreationAttributes").NativeControl!;
                                check(source.Left == attributes.Left && source.Top > attributes.Bottom &&
                                    source.Right <= viewport.Width && source.Bottom <= viewport.Height &&
                                    contactsHeader.Height >= 32 && enemiesHeader.Height >= 32 &&
                                    contacts.Height >= 12 && enemies.Height >= 12 &&
                                    attributeScroll.Extent.Height > attributeScroll.Viewport.Height,
                                    Label($"short and narrow Common retains Contacts, Enemies and scrollable Attributes [split={source.Bounds}, viewport={viewport}, attributes={attributes.Bounds}/{attributeScroll.Bounds}/{attributeScroll.Viewport}/{attributeScroll.Extent}, lists={contacts.Height}/{enemies.Height}, headers={contactsHeader.Height}/{enemiesHeader.Height}]"));
                                var attributeGrid = (Grid)attributeScroll.Content!;
                                var input = attributeGrid.Children.OfType<NumericUpDown>().First();
                                var augmented = attributeGrid.Children.OfType<TextBlock>().First(text => Grid.GetColumn(text) == 2);
                                var heading = Field<Forms.Label>(editor, "lblAttributesAug");
                                string headingText = heading.Text;
                                try
                                {
                                    heading.Text = "Translated augmented rating";
                                    double enlargedFont = input.FontSize * 1.5;
                                    Avalonia.Controls.Documents.TextElement.SetFontSize(attributeScroll, enlargedFont);
                                    await Idle();
                                    check(augmented.Text == heading.Text && augmented.FontSize == enlargedFont &&
                                        input.Bounds.Right <= augmented.Bounds.Left && input.Bounds.Width >= 104 &&
                                        attributeGrid.Children.All(cell => cell.Bounds.Right <= attributeGrid.Bounds.Width + 1),
                                        Label("Creation attribute columns wrap at 150% text and keep ratings inside the grid"));
                                }
                                finally
                                {
                                    heading.Text = headingText;
                                    attributeScroll.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                                    await Idle();
                                }
                            }
                            finally
                            {
                                owner.ClientSize = initialSize;
                                await Idle();
                            }
                            continue;
                        }

                        var workspaceSize = owner.ClientSize;
                        try
                        {
                            owner.ClientSize = new System.Drawing.Size(workspaceSize.Width, 500);
                            await Idle();
                            var page = Field<Forms.TabPage>(editor, "tabCommon");
                            var attributes = Field<Forms.Panel>(editor, "panAttributes");
                            var visible = InventoryTreeLayout.VisiblePageSize(owner, page);
                            check(source.Top > attributes.Bottom && source.Bottom <= visible.Height &&
                                contactsHeader.Height >= 32 && enemiesHeader.Height >= 32 &&
                                contacts.Height >= 12 && enemies.Height >= 12,
                                Label($"short Common keeps both contact headings and lists visible [split={source.Bounds}, viewport={visible}, headers={contactsHeader.Height}/{enemiesHeader.Height}, lists={contacts.Height}/{enemies.Height}]"));
                            var attributesViewport = (ScrollViewer)Host("NativeCareerAttributes").NativeControl!;
                            var attributesGrid = (Grid)attributesViewport.Content!;
                            var body = attributesGrid.Children.OfType<TextBlock>().Single(text => text.Name == "NativelblBOD");
                            var bodyController = Field<Forms.Label>(editor, "lblBOD");
                            var bodyCommand = attributesGrid.GetVisualDescendants().OfType<Button>()
                                .Single(button => button.Name == "NativecmdImproveBOD");
                            double defaultAttributeFont = body.FontSize;
                            try
                            {
                                Avalonia.Controls.Documents.TextElement.SetFontSize(attributesViewport, defaultAttributeFont * 1.5);
                                await Idle();
                                check(body.FontSize == defaultAttributeFont * 1.5 && body.Text == bodyController.Text &&
                                    body.Bounds.Right <= attributesGrid.Children.OfType<TextBlock>()
                                        .Single(text => text.Name == "NativelblBODAug").Bounds.Left &&
                                    attributesViewport.Extent.Height > attributesViewport.Viewport.Height &&
                                    bodyCommand.IsEffectivelyVisible,
                                    Label("Short Career Attributes preserves live values and scrolls at 150% inherited text"));
                            }
                            finally
                            {
                                attributesViewport.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                                await Idle();
                            }
                            int previousBody = character.BOD.Value;
                            int previousExpenseCount = character.ExpenseEntries.Count;
                            int previousKarma = character.Karma;
                            bool confirmAttribute = character.Options.ConfirmKarmaExpense;
                            try
                            {
                                character.Karma = 100;
                                character.Options.ConfirmKarmaExpense = false;
                                check(bodyCommand.IsEnabled, Label("The synthetic character can improve Body"));
                                bodyCommand.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                                check(character.BOD.Value == previousBody + 1 && character.Karma < 100 &&
                                    character.ExpenseEntries.Count == previousExpenseCount + 1 && body.Text == bodyController.Text,
                                    Label("Native Career Body improvement updates the live rating and records one karma expense"));
                            }
                            finally
                            {
                                character.Options.ConfirmKarmaExpense = confirmAttribute;
                                character.Karma = previousKarma;
                            }
                            await CheckBurnEdgeAsync(editor, character, attributesGrid, check);

                            var allocation = Field<Forms.NumericUpDown>(editor, "nudMysticAdeptMAGMagician");
                            bool allocationVisible = allocation.Visible;
                            try
                            {
                                allocation.Visible = true;
                                await Idle();
                                var allocationHost = Host("NativeCommonMagicAllocation");
                                var splitOrigin = owner.PointToClient(source.PointToScreen(System.Drawing.Point.Empty));
                                check(allocationHost.Visible && allocationHost.Bottom < splitOrigin.Y &&
                                    contactsHeader.Height >= 32 && enemiesHeader.Height >= 32,
                                    Label($"short Common keeps the Mystic Adept editor above Contacts [allocation={allocationHost.Bounds}, split={splitOrigin}]"));
                            }
                            finally
                            {
                                allocation.Visible = allocationVisible;
                                await Idle();
                            }
                        }
                        finally
                        {
                            owner.ClientSize = workspaceSize;
                            await Idle();
                        }
                    }
                    if (career && name == "splitSkills")
                    {
                        var originalSize = owner.ClientSize;
                        var addExotic = Field<Forms.Button>(editor, "cmdAddExoticSkill");
                        string originalText = addExotic.Text;
                        var knowledgeCaption = Field<Forms.Label>(editor, "lblKnowledgeSkills");
                        string priorKnowledgeCaption = knowledgeCaption.Text;
                        try
                        {
                            owner.ClientSize = new System.Drawing.Size(550, 500);
                            addExotic.Text = "Add an exotic skill using a long translated caption";
                            knowledgeCaption.Text = string.Join(" ", Enumerable.Repeat("Translated Knowledge Skills", 5));
                            source.SplitterDistance = source.Height / 2;
                            await Idle();
                            var headerHost = Host("NativeCareerSkillHeader");
                            var skillHeader = (WrapPanel)headerHost.NativeControl!;
                            var panel = Field<Forms.Panel>(editor, "panActiveSkills");
                            var panelTop = owner.PointToClient(panel.PointToScreen(System.Drawing.Point.Empty)).Y;
                            check(skillHeader.IsEffectivelyVisible && skillHeader.Children.Select(child => child.Bounds.Top).Distinct().Count() > 1 &&
                                skillHeader.Children.All(child => child.Bounds.Right <= skillHeader.Bounds.Width + 1 &&
                                    child.Bounds.Bottom <= skillHeader.Bounds.Height + 1) &&
                                headerHost.Bottom < panelTop && panel.Height > 40,
                                Label($"Career Active Skills wraps its filter and Add command above the rows [header={headerHost.Bounds}, rows={panel.Bounds}]"));
                            int defaultActiveHeaderHeight = headerHost.Height;
                            var activeHeading = skillHeader.Children.OfType<TextBlock>().Single();
                            try
                            {
                                Avalonia.Controls.Documents.TextElement.SetFontSize(skillHeader, activeHeading.FontSize * 1.5);
                                await Idle();
                                panelTop = owner.PointToClient(panel.PointToScreen(System.Drawing.Point.Empty)).Y;
                                check(headerHost.Height > defaultActiveHeaderHeight && headerHost.Bottom < panelTop &&
                                    skillHeader.Children.All(child => child.Bounds.Bottom <= skillHeader.Bounds.Height + 1) &&
                                    panel.Height >= 40,
                                    Label($"Active Skills remeasures the command row at 150% text [header={defaultActiveHeaderHeight}/{headerHost.Height}, rows={panel.Height}]"));
                            }
                            finally
                            {
                                skillHeader.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                                await Idle();
                            }
                            var activeRows = (StackPanel)((ScrollViewer)Host("NativeCareerActiveSkillRows").NativeControl!).Content!;
                            var activeRow = activeRows.Children.OfType<StackPanel>().FirstOrDefault(row =>
                                row.IsVisible && row.Children.OfType<WrapPanel>().SelectMany(wrap => wrap.Children).OfType<Button>()
                                    .Any(button => button.Name == "NativeCareerSpecialization" && button.IsVisible && button.IsEnabled));
                            check(activeRow != null, Label($"Active Skills mounts an eligible specialization row [native={activeRows.Children.Count}, legacy={panel.Controls.Count}, visible={panel.Visible}, eligible={panel.Controls.OfType<SkillControl>().Count(skill => Field<Forms.Button>(skill, "cmdChangeSpec").Visible && Field<Forms.Button>(skill, "cmdChangeSpec").Enabled)}]"));
                            var skill = (SkillControl)activeRow!.Tag!;
                            var skillName = skill.CreationNameLabel;
                            string previousName = skillName.Text;
                            try
                            {
                                skillName.Text = string.Join(" ", Enumerable.Repeat("Long translated active skill name", 5));
                                ((Control)activeRow).BringIntoView();
                                await Idle();
                                var rowTitle = (TextBlock)activeRow.Children[0];
                                var stats = activeRow.Children[1];
                                var commands = (WrapPanel)activeRow.Children[2];
                                check(rowTitle.Text == skillName.Text && rowTitle.Bounds.Height > 20 &&
                                    rowTitle.Bounds.Bottom <= stats.Bounds.Top && stats.Bounds.Bottom <= commands.Bounds.Top &&
                                    commands.Children.Where(child => child.IsVisible).All(child => child.Bounds.Right <= commands.Bounds.Width + 1),
                                    Label("Native Active Skill rows wrap names above ratings and commands"));
                                byte[] beforeSpecialization = character.CaptureSnapshot();
                                commands.Children.OfType<Button>().Single(button => button.Name == "NativeCareerSpecialization")
                                    .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                                await Idle();
                                check(activeRow.Children.OfType<ComboBox>().Single().IsEffectivelyVisible &&
                                    beforeSpecialization.AsSpan().SequenceEqual(character.CaptureSnapshot()),
                                    Label("The native skill specialization button reveals its editor without changing the character"));
                                var filter = skillHeader.Children.OfType<ComboBox>().Single();
                                int priorFilter = filter.SelectedIndex;
                                try
                                {
                                    filter.SelectedIndex = 1;
                                    await Idle();
                                    check(activeRows.Children.OfType<StackPanel>().All(row => row.IsVisible == ((SkillControl)row.Tag!).Visible),
                                        Label("Native Active Skills rows follow the existing filter state"));
                                }
                                finally
                                {
                                    filter.SelectedIndex = priorFilter;
                                    await Idle();
                                }
                            }
                            finally { skillName.Text = previousName; }
                            var groupHost = Host("NativeCareerSkillGroups");
                            var groupScroll = ((DockPanel)groupHost.NativeControl!).Children.OfType<ScrollViewer>().Single();
                            var groupRows = (StackPanel)groupScroll.Content!;
                            var groupController = Field<Forms.Panel>(editor, "panSkillGroups").Controls.OfType<SkillGroupControl>().First();
                            var groupRow = (Grid)groupRows.Children[0];
                            var groupName = (TextBlock)groupRow.Children[0];
                            var groupRating = (TextBlock)groupRow.Children[1];
                            var improve = (Button)groupRow.Children[2];
                            var controllerName = groupController.CreationNameInput;
                            string priorGroupName = controllerName.Text;
                            bool groupEnabled = groupController.IsEnabled;
                            try
                            {
                                controllerName.Text = string.Join(" ", Enumerable.Repeat("Translated skill group name", 4));
                                groupController.IsEnabled = false;
                                await Idle();
                                check(groupRow.IsEffectivelyVisible && groupName.Bounds.Height > groupRating.Bounds.Height &&
                                    groupName.Bounds.Right <= groupRating.Bounds.Left && groupRating.Bounds.Right <= improve.Bounds.Left &&
                                    !improve.IsEnabled && groupHost.Right <= headerHost.Left &&
                                    groupRating.Text == groupController.GroupRating.ToString(),
                                    Label($"Career Skill Groups wraps names and preserves disabled improvement state [name={groupName.Bounds}, rating={groupRating.Bounds}, improve={improve.Bounds}]"));
                            }
                            finally
                            {
                                controllerName.Text = priorGroupName;
                                groupController.IsEnabled = groupEnabled;
                            }
                            int previousRating = groupController.GroupRating;
                            int karmaCost = previousRating == 0 ? character.Options.KarmaNewSkillGroup :
                                (previousRating + 1) * character.Options.KarmaImproveSkillGroup;
                            character.Karma = 100;
                            int previousExpenses = character.ExpenseEntries.Count;
                            bool confirmKarma = character.Options.ConfirmKarmaExpense;
                            try
                            {
                                character.Options.ConfirmKarmaExpense = true;
                                check(improve.IsEnabled, Label("The test group is eligible to improve"));
                                var decline = NativeContactRemovalChecks.ReplyToConfirmation(Forms.DialogResult.No);
                                improve.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                                await decline;
                                check(groupController.GroupRating == previousRating && character.Karma == 100 &&
                                    character.ExpenseEntries.Count == previousExpenses,
                                    Label("Declining Skill Group improvement leaves rating, karma and expenses unchanged"));
                                var confirm = NativeContactRemovalChecks.ReplyToConfirmation(Forms.DialogResult.Yes);
                                improve.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                                await confirm;
                                check(groupController.SkillGroupObject.Rating == previousRating + 1 &&
                                    groupRating.Text == (previousRating + 1).ToString() &&
                                    character.Karma == 100 - karmaCost &&
                                    character.ExpenseEntries.Count == previousExpenses + 1,
                                    Label("Confirming native Skill Group improvement updates the rating and records one karma expense"));
                            }
                            finally
                            {
                                character.Options.ConfirmKarmaExpense = confirmKarma;
                            }
                            var knowledgeHost = Host("NativeCareerKnowledgeHeader");
                            var knowledgeRows = (StackPanel)((Border)knowledgeHost.NativeControl!).Child!;
                            var knowledgePanel = Field<Forms.Panel>(editor, "panKnowledgeSkills");
                            int knowledgeTop = owner.PointToClient(knowledgePanel.PointToScreen(System.Drawing.Point.Empty)).Y;
                            check(knowledgeRows.IsEffectivelyVisible && knowledgeRows.Children[0].Bounds.Bottom <= knowledgeRows.Children[1].Bounds.Top &&
                                knowledgeRows.Children[1].Bounds.Bottom <= knowledgeRows.Bounds.Height &&
                                knowledgeHost.Bottom < knowledgeTop && knowledgePanel.Height >= 36,
                                Label($"Knowledge Skills keeps its Add button and rows below the wrapped heading [header={knowledgeHost.Bounds}, rows={knowledgePanel.Bounds}]"));
                            var knowledgeSurface = (Border)knowledgeHost.NativeControl!;
                            int normalHeaderHeight = knowledgeHost.Height;
                            double normalFont = ((TextBlock)knowledgeRows.Children[0]).FontSize;
                            try
                            {
                                Avalonia.Controls.Documents.TextElement.SetFontSize(knowledgeSurface, normalFont * 1.5);
                                await Idle();
                                knowledgeTop = owner.PointToClient(knowledgePanel.PointToScreen(System.Drawing.Point.Empty)).Y;
                                check(knowledgeHost.Height > normalHeaderHeight && knowledgeHost.Bottom < knowledgeTop &&
                                    knowledgeRows.Children[1].Bounds.Bottom <= knowledgeSurface.Bounds.Height &&
                                    knowledgePanel.Height >= 36,
                                    Label($"Shared list headings remeasure at 150% text and reserve list space [header={normalHeaderHeight}/{knowledgeHost.Height}, list={knowledgePanel.Height}]"));
                            }
                            finally
                            {
                                knowledgeSurface.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                                await Idle();
                            }
                        }
                        finally
                        {
                            addExotic.Text = originalText;
                            knowledgeCaption.Text = priorKnowledgeCaption;
                            source.SplitterDistance = before;
                            owner.ClientSize = originalSize;
                            await Idle();
                        }
                    }
                    if (name == "splitMain")
                    {
                        var root = TopLevel.GetTopLevel(thumb)!;
                        var pointer = new Avalonia.Input.Pointer(84, PointerType.Mouse, true);
                        var start = thumb.TranslatePoint(new Point(thumb.Bounds.Width / 2, thumb.Bounds.Height / 2), root)!.Value;
                        var finish = start + (vertical ? new Vector(20, 0) : new Vector(0, 20));
                        thumb.RaiseEvent(new PointerPressedEventArgs(thumb, pointer, root, start, 1,
                            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.LeftButtonPressed),
                            KeyModifiers.None, 1));
                        thumb.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, thumb, pointer, root, finish, 2,
                            new PointerPointProperties(RawInputModifiers.LeftMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
                        thumb.RaiseEvent(new PointerReleasedEventArgs(thumb, pointer, root, finish, 3,
                            new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.LeftButtonReleased),
                            KeyModifiers.None, MouseButton.Left));
                        await Idle();
                        check(source.SplitterDistance > before && pointer.Captured == null,
                            Label("main split drags through the native handle and releases pointer capture"));
                        source.SplitterDistance = before;
                    }

                    void Press(Key key) => thumb.RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = key });
                }
                sections.SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
                gear.SelectedTab = Field<Forms.TabPage>(editor, "tabPets");
                var petWorkspace = owner.ClientSize;
                bool confirmPetDelete = character.Options.ConfirmDelete;
                try
                {
                    owner.ClientSize = new System.Drawing.Size(480, 500);
                    await Idle();
                    var pets = (DockPanel)((Border)Host("NativePets").NativeControl!).Child!;
                    var petScroll = pets.Children.OfType<ScrollViewer>().Single();
                    var petRows = (StackPanel)petScroll.Content!;
                    var addPet = pets.Children.OfType<Button>().Single();
                    check(petRows.Children.Count == 2 && petScroll.IsEffectivelyVisible &&
                        addPet.Bounds.Bottom <= petScroll.Bounds.Top && petScroll.Bounds.Bottom <= pets.Bounds.Height,
                        Label("Narrow Pets and Cohorts keeps Add Pet above both native rows"));
                    var firstRow = (StackPanel)((Border)petRows.Children[0]).Child!;
                    var dirtyField = editor.GetType().GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!;
                    dirtyField.SetValue(editor, false);
                    firstRow.Children.OfType<TextBox>().Single().Text = "Edited pet";
                    check(firstPet.Name == "Edited pet" && secondPet.Name == "Second pet" && Field<bool>(editor, "_blnIsDirty"),
                        Label("Native pet name updates only its matching model and marks the editor dirty"));
                    var petActions = firstRow.Children.OfType<WrapPanel>().Single().Children.OfType<Button>().ToArray();
                    var link = petActions.Single(button => button.Name == "NativePetLink");
                    var linkCaption = (TextBlock)link.Content!;
                    var petInput = firstRow.Children.OfType<TextBox>().Single();
                    string originalLinkCaption = linkCaption.Text!;
                    try
                    {
                        firstRow.MaxWidth = 175;
                        double enlarged = petInput.FontSize * 1.5;
                        Avalonia.Controls.Documents.TextElement.SetFontSize(firstRow, enlarged);
                        linkCaption.Text = "Translated command to link an existing character to this pet";
                        await Idle();
                        check(petInput.FontSize == enlarged && linkCaption.FontSize == enlarged &&
                            linkCaption.Bounds.Height > enlarged * 2 &&
                            petActions.All(button => button.Bounds.Right <= firstRow.Bounds.Width + 1),
                            Label("Pet name and translated actions fit a narrow row at 150% text"));
                    }
                    finally
                    {
                        linkCaption.Text = originalLinkCaption;
                        firstRow.ClearValue(Control.MaxWidthProperty);
                        firstRow.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                        await Idle();
                    }
                    var flyout = (MenuFlyout)link.Flyout!;
                    var links = flyout.Items.OfType<MenuItem>().ToArray();
                    flyout.ShowAt(link);
                    await Idle();
                    check(links[0].IsVisible && !links[1].IsVisible && !links[2].IsVisible,
                        Label("An unlinked pet offers Attach Character"));
                    flyout.Hide();
                    var petController = Field<Forms.Panel>(editor, "panPets").Controls.OfType<PetControl>()
                        .Single(control => ReferenceEquals(control.ContactObject, firstPet));
                    var petFiles = System.IO.Directory.CreateTempSubdirectory("chummer-pet-check-");
                    string linkedPath = System.IO.Path.Combine(petFiles.FullName, "linked pet.chum");
                    try
                    {
                        byte[] savedPet = fixture.CaptureSnapshot();
                        System.IO.File.WriteAllBytes(linkedPath, savedPet);
                        dirtyField.SetValue(editor, false);
                        check(!petController.AttachSelectedCharacter("") && string.IsNullOrEmpty(firstPet.FileName) &&
                            !Field<bool>(editor, "_blnIsDirty"),
                            Label("Canceling pet attachment leaves model and dirty state unchanged"));
                        check(petController.AttachSelectedCharacter(linkedPath) && firstPet.FileName == linkedPath &&
                            !string.IsNullOrEmpty(firstPet.RelativeFileName) && Field<bool>(editor, "_blnIsDirty") &&
                            System.IO.File.ReadAllBytes(linkedPath).AsSpan().SequenceEqual(savedPet) &&
                            firstRow.Children.OfType<TextBlock>().Any(text => text.IsVisible && text.Text!.Contains(fixture.Metatype)),
                            Label("Attaching a selected character updates the pet, displays its metatype, and preserves the source file"));
                        string attachedRelativePath = firstPet.RelativeFileName;
                        firstPet.FileName = System.IO.Path.Combine(petFiles.FullName, "moved pet.chum");
                        check(petController.ResolveLinkedCharacter() == linkedPath,
                            Label("Pet links resolve an escaped relative path when the absolute path is stale"));
                        firstPet.RelativeFileName = "";
                        check(petController.ResolveLinkedCharacter() == null,
                            Label("A missing pet link has no resolved character and leaves the pet editable"));
                        firstPet.FileName = linkedPath;
                        firstPet.RelativeFileName = attachedRelativePath;
                        string linkedRelativePath = firstPet.RelativeFileName;
                        dirtyField.SetValue(editor, false);
                        flyout.ShowAt(link);
                        await Idle();
                        check(!links[0].IsVisible && links[1].IsVisible && links[2].IsVisible,
                            Label("A linked pet offers Open and Remove Character"));
                        var decline = NativeContactRemovalChecks.ReplyToConfirmation(Forms.DialogResult.No);
                        links[2].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
                        await decline;
                        check(firstPet.FileName == linkedPath && firstPet.RelativeFileName == linkedRelativePath &&
                            !Field<bool>(editor, "_blnIsDirty"),
                            Label("Declining Remove Character preserves the linked pet and dirty state"));
                        flyout.Hide();
                        flyout.ShowAt(link);
                        var confirm = NativeContactRemovalChecks.ReplyToConfirmation(Forms.DialogResult.Yes);
                        links[2].RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(MenuItem.ClickEvent));
                        await confirm;
                        check(string.IsNullOrEmpty(firstPet.FileName) && string.IsNullOrEmpty(firstPet.RelativeFileName) &&
                            character.Contacts.Contains(firstPet) && Field<bool>(editor, "_blnIsDirty"),
                            Label("Confirming Remove Character clears the link and marks the owning editor dirty"));
                    }
                    finally
                    {
                        flyout.Hide();
                        firstPet.FileName = "";
                        firstPet.RelativeFileName = "";
                        petFiles.Delete(recursive: true);
                    }

                    var notes = petActions.Single(button => button.Name == "NativePetNotes");
                    firstPet.Notes = "Pet notes";
                    dirtyField.SetValue(editor, false);
                    var reply = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                    Dispatcher.UIThread.Post(async () =>
                    {
                        NotesWindow? window = null;
                        try
                        {
                            using var timeout = new System.Threading.CancellationTokenSource(TimeSpan.FromSeconds(10));
                            while (NativeDialogHost.ActiveWindow is not NotesWindow)
                                await Task.Delay(10, timeout.Token);
                            window = (NotesWindow)NativeDialogHost.ActiveWindow!;
                            var workspace = TopLevel.GetTopLevel(notes)!;
                            while (!window.IsVisible || window.Owner == null)
                                await Task.Delay(10, timeout.Token);
                            await Idle();
                            check(window.Input.Text == "Pet notes" && ReferenceEquals(window.Owner, workspace),
                                Label($"Pet Notes opens the selected pet in an owned modal [initial={window.Input.Text}; model={firstPet.Notes}, owner matches={ReferenceEquals(window.Owner, workspace)}]"));
                            window.Input.Text += "\nUpdated pet notes";
                            window.Close();
                            reply.SetResult();
                        }
                        catch (Exception error)
                        {
                            window?.Close();
                            reply.SetException(error);
                        }
                    });
                    notes.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                    await reply.Task;
                    check(firstPet.Notes == "Pet notes\nUpdated pet notes" &&
                        string.IsNullOrEmpty(secondPet.Notes) && TopLevel.GetTopLevel(notes)!.IsEnabled && Field<bool>(editor, "_blnIsDirty"),
                        Label("Pet Notes marks the editor dirty, updates only that pet, and restores the workspace"));
                    addPet.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                    check(petRows.Children.Count == 3 && character.Contacts.Count(contact => contact.EntityType == ContactType.Pet) == 3,
                        Label("Native Add Pet adds a model and ordered row"));
                    character.Options.ConfirmDelete = false;
                    firstRow.Children.OfType<WrapPanel>().Single().Children.OfType<Button>()
                        .Single(button => button.Name == "NativePetDelete")
                        .RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
                    check(petRows.Children.Count == 2 && !character.Contacts.Contains(firstPet) &&
                        character.Contacts.Contains(secondPet) && character.Contacts.Count(contact => contact.EntityType == ContactType.Pet) == 2,
                        Label("Native Delete Pet removes only the selected pet"));
                }
                finally
                {
                    gear.SelectedTab = (Forms.TabPage)gearHeader.Tag!;
                    owner.ClientSize = petWorkspace;
                    character.Options.ConfirmDelete = confirmPetDelete;
                    await Idle();
                }

            }
            finally
            {
                editor.GetType().GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
                editor.Close();
            }
            check(!owner.Controls.OfType<Forms.NativeControlHost>().Any(host =>
                host.Name.StartsWith("Nativetab", StringComparison.Ordinal) || host.Name.StartsWith("Nativesplit", StringComparison.Ordinal)),
                Label("closes all native header and splitter hosts"));

            string Label(string message) => (career ? "Career " : "Creation ") + message;
            Forms.NativeControlHost Host(string name) => owner.Controls.OfType<Forms.NativeControlHost>().Single(host => host.Name == name);
            ScrollViewer TabViewport(Forms.TabControl source) => Host("Native" + source.Name).NativeControl switch
            {
                Grid header => header.Children.OfType<ScrollViewer>().Single(),
                ScrollViewer viewport => viewport,
                _ => throw new InvalidOperationException("Missing tab viewport.")
            };
            TabControl Tabs(Forms.TabControl source) => (TabControl)TabViewport(source).Content!;
            void AssertTabs(Forms.TabControl source, TabControl native, string label)
            {
                var headers = native.Items.OfType<TabItem>().ToArray();
                var strip = Host("Native" + source.Name);
                check(native.IsEffectivelyVisible && headers.Length == source.TabCount &&
                    headers.Select(header => header.Tag).SequenceEqual(source.TabPages.Cast<Forms.TabPage>()) &&
                    headers.All(header => header.Bounds.Height > 0) && strip.Height > 0 && source.SelectedTab!.Top >= strip.Height,
                    Label($"{label} render in order above the existing page content [strip={strip.Bounds}, selected={source.SelectedTab?.Bounds}, headers={headers.Length}]"));
            }
        }
    }

    private static Task Idle() => Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle).GetTask();

    internal static async Task CheckCareerAttributesAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = true;
        using var editor = new frmCareer(character) { MdiParent = owner };
        editor.Show();
        try
        {
            Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabCommon");
            await Idle();
            var host = owner.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "NativeCareerAttributes");
            await CheckBurnEdgeAsync(editor, character, (Grid)((ScrollViewer)host.NativeControl!).Content!, check);
        }
        finally
        {
            typeof(frmCareer).GetField("_blnIsDirty", BindingFlags.Instance | BindingFlags.NonPublic)!.SetValue(editor, false);
            editor.Close();
        }
    }

    private static async Task CheckBurnEdgeAsync(Forms.Form editor, Character character, Grid attributesGrid, Action<bool, string> check)
    {
        var burn = attributesGrid.GetVisualDescendants().OfType<Button>()
            .Single(button => button.Name == "NativecmdBurnEdge");
        burn.BringIntoView();
        await Idle();
        int priorEdge = character.EDG.Value;
        int expenseCount = character.ExpenseEntries.Count;
        int unspentKarma = character.Karma;
        int bodyAfterImprovement = character.BOD.Value;
        check(priorEdge > 0 && burn.IsEffectivelyVisible && burn.IsEnabled,
            "Burn Edge remains reachable in the scrolled Attributes pane");
        var dismissed = NativeContactRemovalChecks.ReplyToConfirmation(null);
        burn.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await dismissed;
        check(character.EDG.Value == priorEdge && character.Karma == unspentKarma &&
            character.ExpenseEntries.Count == expenseCount,
            "Dismissing Burn Edge preserves Edge, karma, and expense history");
        var approved = NativeContactRemovalChecks.ReplyToConfirmation(Forms.DialogResult.Yes);
        burn.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
        await approved;
        var edge = attributesGrid.Children.OfType<TextBlock>().Single(text => text.Name == "NativelblEDG");
        check(character.EDG.Value == priorEdge - 1 && character.BOD.Value == bodyAfterImprovement &&
            edge.Text == Field<Forms.Label>(editor, "lblEDG").Text && character.Karma == unspentKarma &&
            character.ExpenseEntries.Count == expenseCount,
            "Confirming Burn Edge updates only the Edge rating and its live readout");
    }

    private static T Field<T>(object owner, string name) =>
        (T)owner.GetType().GetField(name, BindingFlags.Instance | BindingFlags.NonPublic)!.GetValue(owner)!;
}
