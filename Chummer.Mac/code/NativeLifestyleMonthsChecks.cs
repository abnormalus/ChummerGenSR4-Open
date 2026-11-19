#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeLifestyleMonthsChecks
{
    internal static async Task RunAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = false;
        using var editor = new frmCreate(character) { Loading = true, MdiParent = owner };
        editor.Show();

        const BindingFlags privateFields = BindingFlags.Instance | BindingFlags.NonPublic;
        T Field<T>(string name) => (T)typeof(frmCreate).GetField(name, privateFields)!.GetValue(editor)!;
        var dirty = typeof(frmCreate).GetField("_blnIsDirty", privateFields)!;
        var outerTabs = Field<Forms.TabControl>("tabCharacterTabs");
        var streetGearTabs = Field<Forms.TabControl>("tabStreetGearTabs");
        var lifestyleTab = Field<Forms.TabPage>("tabLifestyle");
        var tree = Field<Forms.TreeView>("treLifestyles");
        var host = owner.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "nativeCreationLifestyleMonths");
        var row = (Grid)host.NativeControl!;
        var input = row.Children.OfType<NumericUpDown>().Single();
        var total = row.Children.OfType<TextBlock>().Single(label => label.Name == "nativeLifestyleTotalCost");

        (Lifestyle Model, Forms.TreeNode Node) AddLifestyle(string name, int months)
        {
            var model = new Lifestyle(character) { Name = name, Cost = 1000, Months = months };
            var node = new Forms.TreeNode { Text = name, Tag = model.InternalId };
            character.Lifestyles.Add(model);
            tree.Nodes[0].Nodes.Add(node);
            return (model, node);
        }

        try
        {
            outerTabs.SelectedTab = Field<Forms.TabPage>("tabStreetGear");
            streetGearTabs.SelectedTab = lifestyleTab;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(host.Visible && TopLevel.GetTopLevel(row) != null && input.Bounds.Width > 0 &&
                !lifestyleTab.Controls.OfType<Forms.NumericUpDown>().Any(), "Lifestyle renders the native editor on the active tab");

            tree.SelectedNode = tree.Nodes[0];
            check(!input.IsEnabled && total.Text == "", "No selected lifestyle disables the editor and clears its cost");

            var (first, firstNode) = AddLifestyle("Synthetic lifestyle A", 2);
            var (second, secondNode) = AddLifestyle("Synthetic lifestyle B", 4);
            var (legacy, legacyNode) = AddLifestyle("Synthetic lifestyle with zero stored months", 0);
            dirty.SetValue(editor, false);
            foreach (var node in new[] { firstNode, secondNode, legacyNode, firstNode })
                tree.SelectedNode = node;
            check(input.IsEnabled && input.Value == 2 && second.Months == 4 && legacy.Months == 0 &&
                !(bool)dirty.GetValue(editor)!, "Changing selection preserves saved months and clean state");

            check(input.Minimum == 1 && input.Maximum == 100 && input.Increment == 1,
                "The Lifestyle spinner keeps whole-month steps and the 1 to 100 range");
            input.Value = 3;
            check(first.Months == 3 && first.TotalCost == 3000 && total.Text == string.Format("= {0:###,###,##0¥}", first.TotalCost) &&
                (bool)dirty.GetValue(editor)!, "Editing months updates the selected lifestyle, displayed total and dirty state");

            input.Value = 101;
            check(input.Value == 100 && first.Months == 100 && first.TotalCost == 100000,
                "Out-of-range input clips to the permanent lifestyle value");
            var reloaded = new Character();
            reloaded.LoadSnapshot(character.CaptureSnapshot());
            check(reloaded.Lifestyles.Single(item => item.InternalId == first.InternalId).Months == 100 &&
                reloaded.Lifestyles.Single(item => item.InternalId == second.InternalId).Months == 4,
                "Lifestyle changes round-trip through character serialization");

            streetGearTabs.SelectedTab = Field<Forms.TabPage>("tabArmor");
            check(!host.Visible, "Leaving Lifestyle hides the native editor");
        }
        finally
        {
            dirty.SetValue(editor, false);
            editor.Close();
        }
    }
}
