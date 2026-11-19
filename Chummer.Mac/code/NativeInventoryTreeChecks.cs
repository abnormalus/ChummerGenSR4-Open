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
                var tree = (TreeView)host.NativeControl!;
                var drag = (NativeTreeDrag)tree.Tag!;
                var root = source.Nodes[0];
                var first = new Forms.TreeNode { Text = "Synthetic row A", Tag = firstId };
                var second = new Forms.TreeNode { Text = "Synthetic row B", Tag = secondId };
                root.Nodes.Add(first);
                root.Nodes.Add(second);
                root.Expand();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(source.Parent == null && host.Visible && tree.IsEffectivelyVisible &&
                    TopLevel.GetTopLevel(tree) != null && tree.Items.Count == source.Nodes.Count,
                    label + " displays the native inventory tree");

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
