#nullable enable
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Threading;
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
        static NumericUpDown Input(Forms.NativeControlHost host) => (NumericUpDown)((Border)host.NativeControl!).Child!;

        try
        {
            var outer = Field<Forms.TabControl>("tabCharacterTabs");
            var tabs = Field<Forms.TabControl>("tabStreetGearTabs");
            var gearTab = Field<Forms.TabPage>("tabGear");
            outer.SelectedTab = Field<Forms.TabPage>("tabStreetGear");
            tabs.SelectedTab = gearTab;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var ratingHost = Host("nativenudGearRating");
            var quantityHost = Host("nativenudGearQty");
            var rating = Input(ratingHost);
            var quantity = Input(quantityHost);
            check(ratingHost.Visible && quantityHost.Visible && TopLevel.GetTopLevel(rating) != null &&
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

            var restored = new Character();
            restored.LoadSnapshot(character.CaptureSnapshot());
            check(restored.Gear.Single(item => item.InternalId == rated.InternalId).Rating == 6 &&
                restored.Gear.Single(item => item.InternalId == pack.InternalId).Quantity == 20,
                "Carried Gear edits survive character serialization");
            tabs.SelectedTab = Field<Forms.TabPage>("tabArmor");
            check(!ratingHost.Visible && !quantityHost.Visible, "Leaving Gear hides both native inputs");
        }
        finally
        {
            dirty.SetValue(editor, false);
            editor.Close();
        }
    }
}
