#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeVehicleInputChecks
{
    internal static async Task RunAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = false;
        using var editor = new frmCreate(character) { Loading = true, MdiParent = owner };
        editor.Show();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        T Field<T>(string name) => (T)typeof(frmCreate).GetField(name, flags)!.GetValue(editor)!;
        var dirty = typeof(frmCreate).GetField("_blnIsDirty", flags)!;

        try
        {
            var tabs = Field<Forms.TabControl>("tabCharacterTabs");
            var vehicleTab = Field<Forms.TabPage>("tabVehicles");
            tabs.SelectedTab = vehicleTab;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            var host = owner.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "NativeCreationVehicles");
            var layout = (Grid)((Grid)host.NativeControl!).Children[0];
            var details = layout.Children.OfType<ScrollViewer>().Single();
            var panel = (StackPanel)details.Content!;
            var fields = panel.Children.OfType<WrapPanel>().SelectMany(row => row.Children).OfType<Grid>().ToArray();
            var inputs = fields.SelectMany(field => field.Children).OfType<NumericUpDown>().ToArray();
            var rating = inputs.Single(input => input.Name == "nativenudVehicleRating");
            var quantity = inputs.Single(input => input.Name == "nativenudVehicleGearQty");
            check(host.Visible && TopLevel.GetTopLevel(panel) != null && !panel.IsEffectivelyVisible &&
                !vehicleTab.Controls.OfType<Forms.NumericUpDown>().Any(), "The Vehicles tab hides item details until a selection");

            var tree = Field<Forms.TreeView>("treVehicles");
            var vehicle = new Vehicle(character);
            var vehicleNode = new Forms.TreeNode();
            vehicle.Create(XmlManager.Instance.Load("vehicles.xml").SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!,
                vehicleNode, null, null, null, null, null, false);
            character.Vehicles.Add(vehicle);
            tree.Nodes[0].Nodes.Add(vehicleNode);

            (Gear Model, Forms.TreeNode Node) AddGear(string name, int maxRating, int pack)
            {
                var gear = new Gear(character)
                {
                    Name = name, MaxRating = maxRating, Rating = maxRating > 0 ? 2 : 0,
                    Quantity = pack, CostFor = pack, Cost = "100", Avail = "0"
                };
                var node = new Forms.TreeNode { Tag = gear.InternalId, Text = gear.DisplayName };
                vehicle.Gear.Add(gear);
                vehicleNode.Nodes.Add(node);
                return (gear, node);
            }

            var (rated, ratedNode) = AddGear("Synthetic rated vehicle gear", 6, 1);
            var (stack, stackNode) = AddGear("Synthetic vehicle gear pack", 0, 10);
            tree.SelectedNode = vehicleNode;
            check(!rating.IsEnabled && !quantity.IsEnabled, "Selecting the vehicle disables item inputs");

            tree.SelectedNode = ratedNode;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(panel.IsEffectivelyVisible && rating.Bounds.Width > 0 && quantity.Bounds.Width > 0 &&
                rating.IsEnabled && rating.Maximum == 6 && rating.Value == 2 && quantity.IsEnabled && quantity.Value == 1,
                "Rated gear displays its catalog bounds and stored values");
            var ratingField = fields.Single(field => field.Children.Contains(rating));
            var ratingCaption = ratingField.Children.OfType<TextBlock>().Single();
            var ratingController = Field<Forms.Label>("lblVehicleRatingLabel");
            string originalRatingCaption = ratingController.Text;
            details.Width = 145;
            Avalonia.Controls.Documents.TextElement.SetFontSize(details, rating.FontSize * 1.5);
            ratingController.Text = "A translated vehicle equipment rating";
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(Grid.GetRow(rating) == 1 && ratingCaption.Bounds.Bottom <= rating.Bounds.Top &&
                rating.Bounds.Right <= ratingField.Bounds.Width + 1 && rating.FontSize == ratingCaption.FontSize,
                "A long vehicle rating caption stacks above its input and inherits enlarged text at 145 logical pixels");
            ratingController.Text = originalRatingCaption;
            details.ClearValue(Control.WidthProperty);
            details.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);

            dirty.SetValue(editor, false);
            rating.Value = 9;
            check(rating.Value == 6 && rated.Rating == 6 && ratedNode.Text == rated.DisplayName && (bool)dirty.GetValue(editor)!,
                "Changing rating clips to the limit and updates the item and dirty state");

            tree.SelectedNode = stackNode;
            check(!rating.IsEnabled && quantity.IsEnabled && quantity.Increment == 10 && quantity.Value == 10 &&
                quantity.Maximum == 100000 && rated.Rating == 6, "Changing selection loads the stack step and disables unavailable rating");
            dirty.SetValue(editor, false);
            quantity.Value += quantity.Increment;
            check(stack.Quantity == 20 && stackNode.Text == stack.DisplayName && stack.TotalCost == 200 &&
                Field<Forms.Label>("lblVehicleCost").Text == string.Format("{0:###,###,##0¥}", stack.TotalCost) && (bool)dirty.GetValue(editor)!,
                "Quantity changes update the owned stack, displayed cost and dirty state");

            var restored = new Character();
            restored.LoadSnapshot(character.CaptureSnapshot());
            var savedGear = restored.Vehicles.Single(item => item.InternalId == vehicle.InternalId).Gear;
            check(savedGear.Single(item => item.InternalId == rated.InternalId).Rating == 6 &&
                savedGear.Single(item => item.InternalId == stack.InternalId).Quantity == 20,
                "Vehicle gear edits survive character serialization");
            tabs.SelectedTab = Field<Forms.TabPage>("tabStreetGear");
            check(!host.Visible, "Leaving Vehicles hides the native inputs");
        }
        finally
        {
            dirty.SetValue(editor, false);
            editor.Close();
        }
    }
}
