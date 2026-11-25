#nullable enable
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeGearInputChecks
{
    internal static async Task RunAsync(frmMain owner, Character fixture, System.Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = false;
        using var editor = new frmCreate(character) { Loading = true, MdiParent = owner };
        editor.Show();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        T Field<T>(string name) => (T)typeof(frmCreate).GetField(name, flags)!.GetValue(editor)!;
        var dirty = typeof(frmCreate).GetField("_blnIsDirty", flags)!;
        Forms.NativeControlHost Host(string name) =>
            owner.Controls.OfType<Forms.NativeControlHost>().Single(control => control.Name == name);

        try
        {
            var outer = Field<Forms.TabControl>("tabCharacterTabs");
            var tabs = Field<Forms.TabControl>("tabStreetGearTabs");
            var gearTab = Field<Forms.TabPage>("tabGear");
            outer.SelectedTab = Field<Forms.TabPage>("tabStreetGear");
            tabs.SelectedTab = gearTab;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var summaryHost = Host("NativeCreationGear");
            var summary = ((Control)summaryHost.NativeControl!).GetVisualDescendants().OfType<ScrollViewer>()
                .Single(view => view.Name == "NativeCreationGearSummary");
            var rows = (StackPanel)summary.Content!;
            var ratingField = ((WrapPanel)rows.Children[2]).Children.OfType<Grid>().Single();
            var quantityField = ((WrapPanel)rows.Children[4]).Children.OfType<Grid>().Single();
            var rating = ratingField.Children.OfType<NumericUpDown>().Single();
            var quantity = quantityField.Children.OfType<NumericUpDown>().Single();
            check(summaryHost.Visible && TopLevel.GetTopLevel(rating) != null &&
                rating.Bounds.Width > 0 && quantity.Bounds.Width > 0 && !gearTab.Controls.OfType<Forms.NumericUpDown>().Any(),
                "The carried Gear tab presents both native spinners");

            var tree = Field<Forms.TreeView>("treGear");
            var root = tree.Nodes[0];
            tree.SelectedNode = root;
            check(!rating.IsEnabled && !quantity.IsEnabled, "The Gear group header disables both inputs");

            var bonus = new XmlDocument();
            bonus.LoadXml("<bonus><spellcategory><name precedence='0'>Combat</name><val>Rating</val></spellcategory></bonus>");
            var rated = new Gear(character)
            {
                Name = "Synthetic rated gear", MinRating = 2, MaxRating = 6, Rating = 3,
                Quantity = 1, Cost = "Rating * 100", Avail = "0", Bonus = bonus.DocumentElement
            };
            var pack = new Gear(character) { Name = "Synthetic gear pack", Quantity = 1, CostFor = 10, Cost = "100", Avail = "0" };
            var plug = new Gear(character) { Name = "Synthetic plug-in", Quantity = 1, Cost = "0", Avail = "0", Parent = pack };
            pack.Children.Add(plug);
            character.Gear.Add(rated);
            character.Gear.Add(pack);
            var ratedNode = new Forms.TreeNode { Text = rated.Name, Tag = rated.InternalId };
            var packNode = new Forms.TreeNode { Text = pack.Name, Tag = pack.InternalId };
            var plugNode = new Forms.TreeNode { Text = plug.Name, Tag = plug.InternalId };
            root.Nodes.Add(ratedNode);
            root.Nodes.Add(packNode);
            packNode.Nodes.Add(plugNode);

            dirty.SetValue(editor, false);
            tree.SelectedNode = ratedNode;
            check(rating.IsEnabled && rating.Minimum == 2 && rating.Maximum == 6 && rating.Value == 3 &&
                quantity.IsEnabled && quantity.Value == 1 && !(bool)dirty.GetValue(editor)!,
                "Selecting rated gear restores its limits and values without marking the character changed");

            summary.Width = 145;
            Avalonia.Controls.Documents.TextElement.SetFontSize(summary, rating.FontSize * 1.5);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(new[] { rating, quantity }.All(input =>
            {
                var field = (Grid)input.Parent!;
                var label = field.Children.OfType<TextBlock>().Single();
                return Grid.GetRow(input) == 1 && label.Bounds.Bottom <= input.Bounds.Top &&
                    input.Bounds.Right <= field.Bounds.Width + 1 && label.FontSize == input.FontSize;
            }), "Carried Gear keeps Rating and Quantity below their captions at 145 logical pixels and enlarged text");
            summary.ClearValue(Control.WidthProperty);
            summary.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);

            string DisplayedCost() => Field<Forms.Label>("lblGearCost").Text;
            void SetAndCheckRating(int input, int expected)
            {
                rating.Value = input;
                check(rating.Value == expected && rated.Rating == expected,
                    "Rating input clips to the expected limit " + expected);
                check(character.Improvements.Single(improvement => improvement.SourceName == rated.InternalId).Value == expected &&
                    DisplayedCost() == string.Format("{0:###,###,##0¥}", expected * 100) && (bool)dirty.GetValue(editor)!,
                    "Rating replaces its improvement and refreshes cost at " + expected);
            }
            SetAndCheckRating(1, 2);
            SetAndCheckRating(7, 6);

            tree.SelectedNode = packNode;
            check(!rating.IsEnabled && quantity.IsEnabled && quantity.Increment == 10 &&
                quantity.Minimum == 1 && quantity.Maximum == 9000 && quantity.Value == 1,
                "Switching between equal quantities refreshes the pack increment and clears rating");
            dirty.SetValue(editor, false);
            quantity.Value = 20;
            check(pack.Quantity == 20 && DisplayedCost() == string.Format("{0:###,###,##0¥}", 200) &&
                (bool)dirty.GetValue(editor)!, "Quantity edits update the owned stack and displayed total");
            tree.SelectedNode = plugNode;
            check(!quantity.IsEnabled && pack.Quantity == 20 && plug.Quantity == 1,
                "Selecting nested gear disables quantity without changing either item");

            var options = rows.Children.OfType<WrapPanel>().Single(row => row.Name == "NativeGearOptions");
            var equipped = options.Children.OfType<CheckBox>().Single(toggle => toggle.Name == "NativechkGearEquipped");
            tree.SelectedNode = packNode;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            bool wasEquipped = pack.Equipped;
            check(summaryHost.Visible && equipped.IsEffectivelyVisible && equipped.IsChecked == wasEquipped,
                "The native Equipped option reflects the selected gear");
            equipped.IsChecked = !wasEquipped;
            check(pack.Equipped != wasEquipped, "The native Equipped option updates the selected gear");
            equipped.IsChecked = wasEquipped;

            summary.Width = 145;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(rating.Bounds.Right <= summary.Viewport.Width + 1 &&
                quantity.Bounds.Right <= summary.Viewport.Width + 1 &&
                rows.Children[2].Bounds.Bottom <= rows.Children[3].Bounds.Top &&
                rows.Children[3].Bounds.Bottom <= rows.Children[4].Bounds.Top,
                "The native Gear summary keeps both inputs within the narrow scrolling layout");
            summary.ClearValue(Control.WidthProperty);

            var restored = new Character();
            restored.LoadSnapshot(character.CaptureSnapshot());
            check(restored.Gear.Single(item => item.InternalId == rated.InternalId).Rating == 6 &&
                restored.Gear.Single(item => item.InternalId == pack.InternalId).Quantity == 20,
                "Carried Gear edits survive character serialization");
            tabs.SelectedTab = Field<Forms.TabPage>("tabArmor");
            check(!summaryHost.Visible, "Leaving Gear hides both native inputs");
        }
        finally
        {
            dirty.SetValue(editor, false);
            editor.Close();
        }
    }
}
