#nullable enable
using System;
using System.Linq;
using System.IO;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Input;
using Avalonia.Interactivity;
using Avalonia.LogicalTree;
using Avalonia.VisualTree;
using Avalonia.Media.Imaging;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeArmorModPickerChecks
{
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(owner)!;
    private static async Task WaitForAsync(Func<bool> condition)
    {
        DateTime limit = DateTime.UtcNow.AddSeconds(15);
        while (!condition()) { if (DateTime.UtcNow >= limit) throw new TimeoutException("Native rating input did not update."); await Task.Delay(20); }
    }
    internal static async Task RunAsync(Forms.Form owner, Character character, string output, Action<bool, string> check)
    {
        using (var picker = new frmSelectArmorMod(character, true) { AllowedCategories = "Clothing", ArmorCost = 5000, CapacityDisplayStyle = CapacityStyle.Standard })
        {
            var pending = picker.ShowDialogAsync(owner); var window = (ArmorModPickerWindow)NativeDialogHost.ActiveWindow!;
            window.SearchBox.Text = "Fire Resistance";
            var rating = window.GetLogicalDescendants().OfType<NumericUpDown>().Single(control => control.Name == "armorModRating");
            await WaitForAsync(() => rating.IsEnabled);
            check(!Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled && rating.Bounds.Width >= 110, "Native rated picker owns a usable standard numeric editor");
            rating.Text = "4"; await WaitForAsync(() => window.Model.Rating == 4);
            check(window.Model.Details!.Capacity == "[2]", "Native rating accepts typed whole numbers and refreshes details");
            rating.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_IncreaseButton").RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await WaitForAsync(() => window.Model.Rating == 5);
            check(window.Model.Rating == 5, "Native rating spinner increases the bound rating");
            rating.GetVisualDescendants().OfType<TextBox>().Single().RaiseEvent(new KeyEventArgs { RoutedEvent = InputElement.KeyDownEvent, Key = Key.Down });
            await WaitForAsync(() => window.Model.Rating == 4);
            check(window.Model.Rating == 4, "Native rating supports keyboard decreases");
            window.SearchBox.Text = "Auto-Injector"; await WaitForAsync(() => !rating.IsEnabled);
            check(rating.Value == 1 && window.Model.Rating == 1, "Native rating clamps and disables when a fixed item is selected");
            window.SearchBox.Text = "missing \"quoted\" mod";
            check(!window.AcceptButton.IsEnabled && window.Model.Details == null, "Native rated picker clears empty search state");
            window.SearchBox.Text = "Fire Resistance"; rating.Text = "4"; await WaitForAsync(() => window.Model.Rating == 4);
            window.Model.Markup = 25; window.Model.FreeCost = true;
            window.Width = 760; window.Height = 500; await Task.Delay(100);
            using (var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192)))
            { bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-armor-mod-rating-2x.png"), PngBitmapEncoderOptions.Default); }
            window.AddMoreButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(await pending == Forms.DialogResult.OK && picker.SelectedRating == 4 && picker.AddAgain && picker.FreeCost && picker.Markup == 25, "Native rated picker returns the accepted rating and purchase options");
        }
        using (var cancelled = new frmSelectArmorMod(character))
        {
            var pending = cancelled.ShowDialogAsync(owner); NativeDialogHost.ActiveWindow!.Close();
            check(await pending == Forms.DialogResult.Cancel && cancelled.SelectedArmorMod == "" && cancelled.SelectedRating == 0, "Native rated picker title-bar close discards its selection");
        }
        foreach (bool career in new[] { false, true })
        {
            var copy = new Character(); copy.LoadSnapshot(character.CaptureSnapshot()); copy.Created = career; copy.Nuyen = 10000;
            using Forms.Form editor = career ? new frmCareer(copy) { Loading = true } : new frmCreate(copy) { Loading = true }; editor.MdiParent = owner; editor.Show();
            try
            {
                var armor = new Armor(copy); var node = new Forms.TreeNode();
                armor.Create(XmlManager.Instance.Load("armor.xml").SelectSingleNode("/chummer/armors/armor[name='Armor Jacket']")!, node, null);
                copy.Armor.Add(armor); var tree = Field<Forms.TreeView>(editor, "treArmor"); tree.Nodes[0].Nodes.Add(node); tree.SelectedNode = node;
                Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabStreetGear");
                Field<Forms.TabControl>(editor, "tabStreetGearTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabArmor");
                int money = copy.Nuyen;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => { var window = (ArmorModPickerWindow)NativeDialogHost.ActiveWindow!; window.SearchBox.Text = "Fire Resistance"; window.Model.Rating = 3; window.Accept(false); });
                void Buy() => editor.GetType().GetMethod("tsAddArmorMod_Click", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                Buy();
                check(armor.ArmorMods.Any(mod => mod.Name == "Fire Resistance" && mod.Rating == 3) && copy.Nuyen == money - (career ? 300 : 0), "Native " + (career ? "career" : "creation") + " buys the selected armor modification rating");
                int count = armor.ArmorMods.Count; money = copy.Nuyen; tree.SelectedNode = node;
                Avalonia.Threading.Dispatcher.UIThread.Post(() => NativeDialogHost.ActiveWindow!.Close()); Buy();
                check(armor.ArmorMods.Count == count && copy.Nuyen == money, "Native " + (career ? "career" : "creation") + " cancellation preserves armor modifications and funds");
            }
            finally { editor.GetType().GetField("_blnIsDirty", BindingFlags.NonPublic | BindingFlags.Instance)!.SetValue(editor, false); editor.Close(); }
        }
    }
}
