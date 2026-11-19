#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeDialogTreeChecks
{
    internal static async Task RunAsync(Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        byte[] before = character.CaptureSnapshot();

        await CheckDialog(new frmSelectSpell(character), "treSpells");
        await CheckDialog(new frmSelectProgram(character), "trePrograms");
        await CheckDialog(new frmSelectCritterPower(character), "trePowers");
        await CheckDialog(new frmSelectPACKSKit(character), "treContents");
        await CheckDialog(new frmPrintMultiple(), "treCharacters");
        await CheckDialog(new frmSelectAdvancedLifestyle(new Lifestyle(character), character),
            "trePositiveQualities", "treNegativeQualities");
        await CheckDialog(new frmOptions(), "treSourcebook");
        check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
            "Closing the tree dialogs without accepting preserves the character snapshot");

        async Task CheckDialog(Forms.Form dialog, params string[] fields)
        {
            using (dialog)
            {
                dialog.Show();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                foreach (string field in fields)
                {
                    var source = (Forms.TreeView)dialog.GetType()
                        .GetField(field, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(dialog)!;
                    var host =  dialog.Controls.Cast<Forms.Control>().SelectMany(child => Descendants(child).Prepend(child)).OfType<Forms.NativeControlHost>()
                        .Single(item => item.Name == "Native" + field);
                    for (Forms.Control? parent = host.Parent; parent != null; parent = parent.Parent)
                    {
                        if (parent is Forms.TabPage page && page.Parent is Forms.TabControl tabs)
                            tabs.SelectedTab = page;
                    }

                    if (source.Nodes.Count == 0 && field is "treCharacters" or "treContents")
                        source.Nodes.Add(new Forms.TreeNode { Text = "Synthetic entry" });
                    await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                    var tree = (TreeView)host.NativeControl!;
                    string label = dialog.GetType().Name + "." + field;
                    check(source.Parent == null && host.Visible && tree.IsEffectivelyVisible &&
                        TopLevel.GetTopLevel(tree) != null && tree.Bounds.Width > 0 &&
                        tree.Items.Count == source.Nodes.Count && tree.Items.Count > 0,
                        label + " renders the native tree with its rows");

                    var first = (TreeViewItem)tree.Items[0]!;
                    var row = first.Items.OfType<TreeViewItem>().FirstOrDefault() ?? first;
                    var node = (Forms.TreeNode)row.Tag!;
                    if (row != first)
                        first.IsExpanded = true;
                    tree.SelectedItem = row;
                    check(ReferenceEquals(source.SelectedNode, node), label + " routes selection to the existing dialog");
                    if (row.Header is CheckBox box)
                    {
                        bool previous = node.Checked;
                        box.IsChecked = !previous;
                        check(node.Checked == !previous, label + " routes a checkbox change to its dialog");
                        box.IsChecked = previous;
                    }
                    source.Enabled = false;
                    check(!tree.IsEnabled, label + " follows the enabled state");
                    source.Enabled = true;
                }
                dialog.Close();
            }
        }

        static System.Collections.Generic.IEnumerable<Forms.Control> Descendants(Forms.Control parent) =>
            parent.Controls.Cast<Forms.Control>().SelectMany(child => Descendants(child).Prepend(child));
    }
}
