#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeCyberwareCallerChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Character original, Action<bool,string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = career;
            foreach (string book in new[] { "SR4", "AR", "AU" }) if (!character.Options.Books.Contains(book)) character.Options.Books.Add(book);
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true, MdiParent = owner } : new frmCreate(character) { Loading = true, MdiParent = owner };
            editor.Show();
            try
            {
                var vehicle = new Vehicle(character); var vehicleNode = new Forms.TreeNode();
                var catalog = XmlManager.Instance.Load("vehicles.xml");
                vehicle.Create(catalog.SelectSingleNode("/chummer/vehicles/vehicle[name='Dodge Scoot (Scooter)']")!, vehicleNode, null, null, null, null, null, false);
                var mod = new VehicleMod(character); var modNode = new Forms.TreeNode();
                mod.Create(catalog.SelectSingleNode("/chummer/mods/mod[name='Mechanical Arm']")!, modNode, 0);
                vehicle.Mods.Add(mod); character.Vehicles.Add(vehicle);
                var tree = Field<Forms.TreeView>(editor, "treVehicles"); tree.Nodes[0].Nodes.Add(vehicleNode); vehicleNode.Nodes.Add(modNode);
                Field<Forms.TabControl>(editor, "tabCharacterTabs").SelectedTab = Field<Forms.TabPage>(editor, "tabVehicles"); tree.SelectedNode = modNode;
                var history = Field<InventoryUndo>(editor, "_inventoryUndo"); history.Observe();
                byte[] before = character.CaptureSnapshot(); bool dirty = Field<bool>(editor, "_blnIsDirty"); int count = history.Count;
                bool context = false, owned = false; Exception? callbackError = null;
                var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                Dispatcher.UIThread.Post(async () =>
                {
                    try
                    {
                        var limit = DateTime.UtcNow.AddSeconds(20);
                        while (NativeDialogHost.ActiveWindow is not CyberwarePickerWindow)
                        { if (DateTime.UtcNow >= limit) throw new TimeoutException("Vehicle Cyberware caller did not open native window."); await Task.Delay(20); }
                        var window = (CyberwarePickerWindow)NativeDialogHost.ActiveWindow!;
                        var allowed = mod.Subsystems.Split(',').Select(value => value.Trim()).ToArray();
                        window.SearchBox.Text = "";
                        var grade = window.GetVisualDescendants().OfType<ComboBox>().Single(input => input.Name == "cyberwarePickerGrade");
                        context = window.Model.Items.Count > 0 && window.Model.Categories.All(category => allowed.Contains(category.Code)) &&
                            window.Model.Items.All(item => allowed.Contains(item.Category)) && !grade.IsEnabled && window.Model.SelectedGrade?.Name == "Standard";
                        owned = !Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled;
                        window.Close();
                    }
                    catch (Exception failure) { callbackError = failure; NativeDialogHost.CancelAll(); }
                    finally { completion.TrySetResult(); }
                });
                editor.GetType().GetMethod("tsVehicleAddCyberware_Click", Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
                if (callbackError != null) throw new InvalidOperationException("Native vehicle Cyberware callback failed.", callbackError);
                check(context && owned, "Native vehicle Cyberware actual caller restricts nonempty Mechanical Arm catalog and locks Standard career=" + career);
                check(character.CaptureSnapshot().SequenceEqual(before) && Field<bool>(editor, "_blnIsDirty") == dirty && history.Count == count &&
                    Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled && NativeDialogHost.Count == 0,
                    "Native vehicle Cyberware actual caller cancellation restores full snapshot dirty undo and owner career=" + career);
            }
            finally { editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close(); }
        }
    }
}
