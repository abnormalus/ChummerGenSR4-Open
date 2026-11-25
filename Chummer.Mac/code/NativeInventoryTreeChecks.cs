#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeInventoryTreeChecks
{
    internal static async Task RunAsync(frmMain main, Character fixture, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var model = new Character();
            model.LoadSnapshot(fixture.CaptureSnapshot());
            model.Created = career;
            using Forms.Form editor = career ? new frmCareer(model) : new frmCreate(model);
            editor.MdiParent = main;
            editor.Show();
            T Field<T>(string name) => (T)editor.GetType().GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(editor)!;
            var tabs = Field<Forms.TabControl>("tabCharacterTabs");
            var gearTabs = Field<Forms.TabControl>("tabStreetGearTabs");

            var weapon1 = new Weapon(model) { Name = "Synthetic weapon A", RC = "0", Avail = "0" };
            var weapon2 = new Weapon(model) { Name = "Synthetic weapon B", RC = "0", Avail = "0" };
            model.Weapons.AddRange(new[] { weapon1, weapon2 });
            await CheckDrag("Weapons", "tabWeapons", weapon1.InternalId, weapon2.InternalId,
                () => model.Weapons.IndexOf(weapon2) < model.Weapons.IndexOf(weapon1));

            var armor1 = new Armor(model) { Name = "Synthetic armor A", Cost = 0, Avail = "0" };
            var armor2 = new Armor(model) { Name = "Synthetic armor B", Cost = 0, Avail = "0" };
            model.Armor.AddRange(new[] { armor1, armor2 });
            await CheckDrag("Armor", "tabArmor", armor1.InternalId, armor2.InternalId,
                () => model.Armor.IndexOf(armor2) < model.Armor.IndexOf(armor1));

            var lifestyle1 = new Lifestyle(model) { Name = "Synthetic lifestyle A" };
            var lifestyle2 = new Lifestyle(model) { Name = "Synthetic lifestyle B" };
            model.Lifestyles.AddRange(new[] { lifestyle1, lifestyle2 });
            await CheckDrag("Lifestyles", "tabLifestyle", lifestyle1.InternalId, lifestyle2.InternalId,
                () => model.Lifestyles.IndexOf(lifestyle2) < model.Lifestyles.IndexOf(lifestyle1));

            var gear1 = new Gear(model) { Name = "Synthetic gear A", Cost = "0", Avail = "0" };
            var gear2 = new Gear(model) { Name = "Synthetic gear B", Cost = "0", Avail = "0" };
            model.Gear.AddRange(new[] { gear1, gear2 });
            await CheckDrag("Gear", "tabGear", gear1.InternalId, gear2.InternalId,
                () => model.Gear.IndexOf(gear2) < model.Gear.IndexOf(gear1));

            var vehicle1 = new Vehicle(model) { Name = "Synthetic vehicle A", Cost = "0", Avail = "0", Accel = "10/20", Speed = 30 };
            var vehicle2 = new Vehicle(model) { Name = "Synthetic vehicle B", Cost = "0", Avail = "0", Accel = "10/20", Speed = 30 };
            model.Vehicles.AddRange(new[] { vehicle1, vehicle2 });
            await CheckDrag("Vehicles", "tabVehicles", vehicle1.InternalId, vehicle2.InternalId,
                () => model.Vehicles.IndexOf(vehicle2) < model.Vehicles.IndexOf(vehicle1));

            if (career)
            {
                var first = new Improvement { SourceName = "Synthetic first improvement", ImproveSource = Improvement.ImprovementSource.Custom };
                var second = new Improvement { SourceName = "Synthetic second improvement", ImproveSource = Improvement.ImprovementSource.Custom };
                model.Improvements.AddRange(new[] { first, second });
                await CheckDrag("Improvements", "tabImprovements", first.SourceName, second.SourceName,
                    () => second.SortOrder < first.SortOrder);
            }

            editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!
                .SetValue(editor, false);
            editor.Close();

            async Task CheckDrag(string name, string tab, string firstId, string secondId, Func<bool> reordered)
            {
                var page = Field<Forms.TabPage>(tab);
                if (gearTabs.TabPages.Contains(page))
                {
                    tabs.SelectedTab = Field<Forms.TabPage>("tabStreetGear");
                    gearTabs.SelectedTab = page;
                }
                else
                    tabs.SelectedTab = page;
                string label = (career ? "Career" : "Creation") + name;
                var source = Field<Forms.TreeView>("tre" + name);
                var host = main.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "Native" + label);
                var pane = host.NativeControl as Grid;
                var treePane = name switch
                {
                    "Gear" or "Armor" or "Lifestyles" or "Weapons" or "Vehicles" or "Improvements" =>
                        (Grid)((Grid)pane!.Children[0]).Children[0],
                    _ => pane
                };
                var tree = treePane?.Children.OfType<TreeView>().Single() ?? (TreeView)host.NativeControl!;
                if (name is "Armor" or "Gear" or "Lifestyles" or "Improvements")
                {
                    var commands = treePane!.Children.OfType<WrapPanel>().Single();
                    check(commands.Children.Count == (name switch { "Lifestyles" => 2, "Improvements" or "Gear" => 4, _ => 3 }) && commands.Bounds.Bottom <= tree.Bounds.Top,
                        $"{(career ? "Career" : "Creation")} {name} keeps the wrapping commands above its tree");
                    if (name == "Gear")
                    {
                        var addController = Field<Forms.Button>("cmdAddGear");
                        var addGroup = commands.Children.OfType<DockPanel>().Single(group =>
                            group.Children.OfType<Button>().Any(button => button.Name == "NativecmdAddGear"));
                        var add = addGroup.Children.OfType<Button>().Single(button => button.Name == "NativecmdAddGear");
                        var menu = addGroup.Children.OfType<Button>().Single(button => button.Name == "NativecmdAddGearMenu");
                        string originalCaption = addController.Text;
                        try
                        {
                            commands.MaxWidth = 180;
                            Avalonia.Controls.Documents.TextElement.SetFontSize(commands, add.FontSize * 1.5);
                            addController.Text = "Translated command for adding and configuring character equipment";
                            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                            check(addGroup.Bounds.Right <= commands.Bounds.Width + 1 && add.Bounds.Right <= menu.Bounds.Left + 1 &&
                                menu.Bounds.Width > 0 && menu.IsEffectivelyEnabled &&
                                ((TextBlock)add.Content!).Bounds.Height > add.FontSize * 2 &&
                                commands.Bounds.Bottom <= tree.Bounds.Top &&
                                Avalonia.Automation.AutomationProperties.GetName(menu) == addController.Text,
                                label + " wraps translated Add Gear beside its usable menu at 150% text");
                        }
                        finally
                        {
                            addController.Text = originalCaption;
                            commands.ClearValue(Control.MaxWidthProperty);
                            commands.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        }
                        var filter = commands.Children.OfType<CheckBox>().Single();
                        var controller = Field<Forms.CheckBox>("chkCommlinks");
                        var commlink = new Commlink(model) { Name = "Synthetic commlink", Category = "Commlink", Cost = "0", Avail = "0" };
                        model.Gear.Add(commlink);
                        bool ContainsGear(string id) => source.Nodes.Cast<Forms.TreeNode>()
                            .SelectMany(root => root.Nodes.Cast<Forms.TreeNode>()).Any(row => Equals(row.Tag, id));
                        filter.IsChecked = true;
                        check(filter.IsEffectivelyVisible && controller.Checked &&
                            ContainsGear(commlink.InternalId) && !ContainsGear(firstId),
                            label + " displays commlinks and hides other carried gear when filtered");
                        filter.IsChecked = false;
                        check(ContainsGear(commlink.InternalId) && ContainsGear(firstId),
                            label + " restores other carried gear when the filter is cleared");
                    }
                }
                var drag = (NativeTreeDrag)tree.Tag!;
                var root = source.Nodes[0];
                if (name == "Vehicles")
                {
                    var vehicleLayout = (Grid)pane!.Children[0];
                    var summary = vehicleLayout.Children.OfType<ScrollViewer>().Single();
                    vehicleLayout.Width = 480;
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(Grid.GetRow(summary) == 1 && treePane!.Bounds.Bottom <= summary.Bounds.Top &&
                        tree.Bounds.Height > 0 && summary.Bounds.Height > 0,
                        label + " stacks the tree above the scrolling vehicle details on narrow pages");
                    vehicleLayout.ClearValue(Control.WidthProperty);
                    var fields = (StackPanel)summary.Content!;
                    source.SelectedNode = null;
                    check(!fields.IsEffectivelyVisible, label + " hides item readouts without a selection");
                    var selectedVehicle = new Forms.TreeNode("Synthetic vehicle A") { Tag = firstId };
                    root.Nodes.Add(selectedVehicle);
                    source.SelectedNode = selectedVehicle;
                    var nameLabel = Field<Forms.Label>("lblVehicleName");
                    string original = nameLabel.Text;
                    summary.Width = 145;
                    var nameReadout = (TextBlock)((WrapPanel)fields.Children[0]).Children[0];
                    double largerText = nameReadout.FontSize * 1.5;
                    Avalonia.Controls.Documents.TextElement.SetFontSize(summary, largerText);
                    nameLabel.Text = "A long vehicle name that needs to wrap in a narrow details pane";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(host.Visible && nameReadout.FontSize == largerText && nameReadout.Bounds.Height >= 30 &&
                        nameReadout.Bounds.Right <= summary.Viewport.Width + 1 &&
                        fields.Children[0].Bounds.Bottom <= fields.Children[1].Bounds.Top,
                        label + " wraps the vehicle name above the remaining statistics at 150% text");
                    summary.ClearValue(Control.WidthProperty);
                    summary.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    nameLabel.Text = original;
                    var mountedName = Field<Forms.Label>("lblVehicleWeaponName");
                    string previousName = mountedName.Text;
                    bool visible = mountedName.Visible;
                    summary.Width = 145;
                    mountedName.Visible = true;
                    mountedName.Text = "A mounted weapon with a long name that must wrap";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var mountedRow = fields.Children.OfType<WrapPanel>()
                        .Single(row => row.Children.OfType<TextBlock>().Any(text => text.Name == "NativelblVehicleWeaponName"));
                    var mountedReadout = (TextBlock)mountedRow.Children[0];
                    check(host.Visible && mountedReadout.Bounds.Height >= 30 &&
                        mountedRow.Bounds.Bottom <= fields.Children[fields.Children.IndexOf(mountedRow) + 1].Bounds.Top,
                        label + " wraps a long mounted weapon name before its other details");
                    if (career)
                    {
                        var ammo = fields.Children.OfType<StackPanel>().Single(panel => panel.Name == "NativeMountedAmmoType");
                        var picker = ammo.Children.OfType<ComboBox>().Single();
                        var heading = ammo.Children.OfType<TextBlock>().Single();
                        var pickerSource = Field<Forms.ComboBox>("cboVehicleWeaponAmmo");
                        var headingSource = Field<Forms.Label>("lblVehicleWeaponAmmoTypeLabel");
                        bool sourceVisible = pickerSource.Visible, headingVisible = headingSource.Visible;
                        string previousHeading = headingSource.Text;
                        try
                        {
                            pickerSource.Visible = headingSource.Visible = true;
                            headingSource.Text = "Translated ammunition selector for this mounted weapon";
                            double enlarged = picker.FontSize * 1.5;
                            Avalonia.Controls.Documents.TextElement.SetFontSize(summary, enlarged);
                            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                            check(picker.IsEffectivelyVisible && picker.FontSize == enlarged && heading.Text == headingSource.Text &&
                                heading.Bounds.Bottom <= picker.Bounds.Top && picker.Bounds.Right <= summary.Viewport.Width + 1,
                                "Career mounted ammunition caption wraps above its selector at 150% text");
                        }
                        finally
                        {
                            pickerSource.Visible = sourceVisible;
                            headingSource.Visible = headingVisible;
                            headingSource.Text = previousHeading;
                            summary.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                        }
                    }
                    summary.ClearValue(Control.WidthProperty);
                    mountedName.Text = previousName;
                    mountedName.Visible = visible;
                    source.SelectedNode = null;
                    root.Nodes.Remove(selectedVehicle);
                }
                if (name == "Weapons")
                {
                    var weapons = (Grid)pane!.Children[0];
                    var details = weapons.Children.OfType<ScrollViewer>().Single();
                    weapons.Width = 480;
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(Grid.GetRow(details) == 1 && treePane!.Bounds.Bottom <= details.Bounds.Top &&
                        tree.Bounds.Height > 0 && details.Bounds.Height > 0,
                        label + " stacks its list above the scrolling details at narrow widths");
                    var nameSource = Field<Forms.Label>("lblWeaponName");
                    bool nameVisible = nameSource.Visible;
                    string nameText = nameSource.Text;
                    var readout = ((StackPanel)details.Content!).Children.OfType<WrapPanel>()
                        .SelectMany(row => row.Children).OfType<TextBlock>().Single(text => text.Name == "NativelblWeaponName");
                    try
                    {
                        details.Width = 145;
                        double enlargedText = readout.FontSize * 1.5;
                        Avalonia.Controls.Documents.TextElement.SetFontSize(details, enlargedText);
                        nameSource.Visible = true;
                        nameSource.Text = "Translated long weapon name with extended descriptive text";
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        check(readout.IsEffectivelyVisible && readout.FontSize == enlargedText &&
                            readout.Bounds.Right <= details.Viewport.Width + 1 && readout.Bounds.Height > enlargedText * 2,
                            label + " wraps its weapon name at 150% text inside the details pane");
                    }
                    finally
                    {
                        nameSource.Text = nameText;
                        nameSource.Visible = nameVisible;
                        details.ClearValue(Control.WidthProperty);
                        details.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    }
                    weapons.ClearValue(Control.WidthProperty);
                }
                if (name == "Armor")
                {
                    var armorPage = (Grid)pane!.Children[0];
                    var scroll = armorPage.Children.OfType<ScrollViewer>().Single();
                    var rows = (StackPanel)scroll.Content!;
                    var equippedList = rows.Children.OfType<TextBlock>().Single(text => text.Name == "NativelblArmorEquipped");
                    var actions = rows.Children.OfType<WrapPanel>().Single(row => row.Name == "NativeArmorEquipmentActions");
                    var controller = Field<Forms.Label>("lblArmorEquipped");
                    string previous = controller.Text;
                    bool wasVisible = controller.Visible;
                    scroll.Width = 145;
                    controller.Visible = true;
                    controller.Text = "A deliberately long equipped armor name (4/2)";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(host.Visible && equippedList.Text?.Contains(controller.Text, StringComparison.Ordinal) == true &&
                        equippedList.Bounds.Height >= 30 && equippedList.Bounds.Top >= actions.Bounds.Bottom,
                        label + " wraps the equipped armor breakdown below the equipment commands");
                    var sourceLabel = Field<Forms.Label>("lblArmorSource");
                    var included = Field<Forms.CheckBox>("chkIncludedInArmor");
                    var sourceRow = rows.Children.OfType<WrapPanel>().Single(row => row.Name == "NativeArmorSourceRow");
                    var citation = sourceRow.Children.OfType<Button>().Single(button => button.Name == "NativeArmorSource");
                    var includedOption = sourceRow.Children.OfType<CheckBox>().Single();
                    string originalCitation = sourceLabel.Text;
                    bool citationVisible = sourceLabel.Visible;
                    bool includedVisible = included.Visible;
                    double enlargedArmorText = citation.FontSize * 1.5;
                    Avalonia.Controls.Documents.TextElement.SetFontSize(scroll, enlargedArmorText);
                    sourceLabel.Visible = included.Visible = true;
                    sourceLabel.Text = "A deliberately long armor sourcebook citation, page 255";
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(citation.IsEnabled && citation.FontSize == enlargedArmorText &&
                        citation.Bounds.Right <= scroll.Viewport.Width + 1 && ((TextBlock)citation.Content!).Bounds.Height >= 30 &&
                        includedOption.Bounds.Top >= citation.Bounds.Bottom && includedOption.IsChecked == included.Checked,
                        label + " wraps the citation above the Included in Armor checkbox at 150% text");
                    if (career)
                    {
                        var ballistic = rows.Children.OfType<WrapPanel>().Single(row => row.Name == "NativeArmorBallisticRow");
                        var buttons = ballistic.Children.OfType<Button>().ToArray();
                        var controllers = buttons.Select(button => Field<Forms.Button>(button.Name!["Native".Length..])).ToArray();
                        var visibility = controllers.Select(command => command.Visible).ToArray();
                        foreach (var command in controllers)
                            command.Visible = true;
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        check(buttons.Length == 2 && buttons.All(button =>
                            button.Bounds.Width > 0 && button.Bounds.Right <= scroll.Viewport.Width + 1 &&
                            button.IsEnabled == Field<Forms.Button>(button.Name!["Native".Length..]).Enabled) &&
                            ballistic.Bounds.Bottom <= sourceRow.Bounds.Top,
                            "Career Armor wraps its ballistic adjustment buttons above the source reference");
                        for (int index = 0; index < controllers.Length; index++)
                            controllers[index].Visible = visibility[index];
                    }
                    scroll.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                    sourceLabel.Text = originalCitation;
                    sourceLabel.Visible = citationVisible;
                    included.Visible = includedVisible;
                    scroll.ClearValue(Control.WidthProperty);
                    armorPage.Width = 480;
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(Grid.GetRow(scroll) == 1 && Grid.GetColumn(scroll) == 0 &&
                        treePane!.Bounds.Bottom <= scroll.Bounds.Top && tree.Bounds.Height > 0 && scroll.Bounds.Height > 0,
                        label + " stacks the tree above the armor details in a narrow page");
                    armorPage.ClearValue(Control.WidthProperty);
                    controller.Text = previous;
                    controller.Visible = wasVisible;
                }
                var first = new Forms.TreeNode { Text = "Synthetic row A", Tag = firstId };
                var second = new Forms.TreeNode { Text = "Synthetic row B", Tag = secondId };
                root.Nodes.Add(first);
                root.Nodes.Add(second);
                root.Expand();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(source.Parent == null && host.Visible && tree.IsEffectivelyVisible &&
                    TopLevel.GetTopLevel(tree) != null && tree.Items.Count == source.Nodes.Count,
                    label + " displays the native inventory tree");

                if (name == "Improvements")
                {
                    tree.SelectedItem = tree.Items[0];
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var improvementPage = (Grid)pane!.Children[0];
                    var scroller = improvementPage.Children.OfType<ScrollViewer>().Single();
                    var details = (StackPanel)scroller.Content!;
                    var group = details.Children.OfType<WrapPanel>().Single();
                    check(scroller.IsEffectivelyVisible && !details.Children.OfType<CheckBox>().Single().IsVisible &&
                        group.Children.OfType<Button>().Count(button => button.IsEffectivelyVisible) == 2,
                        label + " displays both group commands in the scrolling details pane");
                    var workspaceSize = main.ClientSize;
                    try
                    {
                        main.ClientSize = new System.Drawing.Size(workspaceSize.Width, 380);
                        improvementPage.Width = 480;
                        await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                        check(host.Bottom <= main.ClientSize.Height && Grid.GetRow(scroller) == 1 &&
                            tree.Bounds.Height > 0 && scroller.Bounds.Height > 0,
                            label + " keeps the tree and details reachable in a short, narrow workspace");
                    }
                    finally
                    {
                        improvementPage.ClearValue(Control.WidthProperty);
                        main.ClientSize = workspaceSize;
                    }
                }

                byte[] before = model.CaptureSnapshot();
                check(drag.Begin(first, Forms.MouseButtons.Left), label + " accepts a movable row");
                drag.Complete(second, inside: false);
                check(first.Index < second.Index && before.AsSpan().SequenceEqual(model.CaptureSnapshot()),
                    label + " leaving the tree cancels the drag without changing the character");
                check(drag.Begin(first, Forms.MouseButtons.Left), label + " allows retry after cancellation");
                drag.Complete(second, inside: true);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                var rendered = ((TreeViewItem)tree.Items[0]!).Items.Cast<TreeViewItem>().Select(row => row.Tag).ToArray();
                check(first.Index > second.Index && reordered() && rendered.SequenceEqual(root.Nodes.Cast<object>()),
                    label + " reorders the original objects and refreshes the native rows");

                if (name == "Gear")
                {
                    var parentGear = model.Gear.Single(gear => gear.InternalId == firstId);
                    var childGear = model.Gear.Single(gear => gear.InternalId == secondId);
                    var attachedRows = ((TreeViewItem)tree.Items[0]!).Items.OfType<TreeViewItem>();
                    var parentRow = attachedRows.Single(item => ReferenceEquals(item.Tag, first));
                    var childRow = attachedRows.Single(item => ReferenceEquals(item.Tag, second));
                    var window = TopLevel.GetTopLevel(tree)!;
                    Point InViewport(TreeViewItem row)
                    {
                        var position = row.TranslatePoint(new Point(0, 12), tree);
                        return new Point(tree.Bounds.Width / 2, position?.Y ?? -1);
                    }

                    // Sorting can detach and reattach the same native row. Wait for
                    // layout and scroll the replay's two adjacent rows into the viewport.
                    DateTime deadline = DateTime.UtcNow.AddSeconds(3);
                    parentRow.BringIntoView();
                    childRow.BringIntoView();
                    while (DateTime.UtcNow < deadline &&
                        (!ReferenceEquals(NativeEditorTree.RowAt(tree, InViewport(parentRow)), parentRow) ||
                         !ReferenceEquals(NativeEditorTree.RowAt(tree, InViewport(childRow)), childRow)))
                        await Task.Delay(20);
                    check(ReferenceEquals(NativeEditorTree.RowAt(tree, InViewport(parentRow)), parentRow) &&
                        ReferenceEquals(NativeEditorTree.RowAt(tree, InViewport(childRow)), childRow),
                        label + " hit-tests both drag endpoints across the native rows");

                    var pointer = new Avalonia.Input.Pointer(42, PointerType.Mouse, true);
                    var start = tree.TranslatePoint(InViewport(childRow), window)!.Value;
                    var finish = tree.TranslatePoint(InViewport(parentRow), window)!.Value;
                    childRow.RaiseEvent(new PointerPressedEventArgs(childRow, pointer, window, start, 1,
                        new PointerPointProperties(RawInputModifiers.RightMouseButton, PointerUpdateKind.RightButtonPressed),
                        KeyModifiers.None, 1));
                    childRow.RaiseEvent(new PointerEventArgs(InputElement.PointerMovedEvent, childRow, pointer, window, finish, 2,
                        new PointerPointProperties(RawInputModifiers.RightMouseButton, PointerUpdateKind.Other), KeyModifiers.None));
                    check(ReferenceEquals(pointer.Captured, tree), label + " captures the pointer for a right-button drag");
                    tree.RaiseEvent(new PointerReleasedEventArgs(tree, pointer, window, finish, 3,
                        new PointerPointProperties(RawInputModifiers.None, PointerUpdateKind.RightButtonReleased),
                        KeyModifiers.None, MouseButton.Right));
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    check(pointer.Captured == null, label + " releases the pointer after dropping");
                    check(ReferenceEquals(childGear.Parent, parentGear) && !model.Gear.Contains(childGear) &&
                        parentGear.Children.Contains(childGear), label + " reparents the original gear objects");
                    check(parentRow.Items.OfType<TreeViewItem>().Any(row => ReferenceEquals(row.Tag, second)),
                        label + " preserves the native row identity after reparenting");
                }

                if (name == "Vehicles")
                {
                    var vehicle = model.Vehicles.Single(item => item.InternalId == firstId);
                    var mounted = new Gear(model) { Name = "Synthetic mounted gear", Cost = "0", Avail = "0" };
                    vehicle.Gear.Add(mounted);
                    var mountedNode = new Forms.TreeNode { Text = mounted.Name, Tag = mounted.InternalId };
                    first.Nodes.Add(mountedNode);
                    check(drag.Begin(mountedNode, Forms.MouseButtons.Right), label + " accepts mounted gear as a drag origin");
                    var mountedBefore = model.CaptureSnapshot();
                    drag.Complete(root, inside: true);
                    check(ReferenceEquals(mountedNode.Parent, first) &&
                        mountedBefore.AsSpan().SequenceEqual(model.CaptureSnapshot()),
                        label + " rejects a mounted gear drop onto the inventory root");
                }
            }
        }
    }
}
