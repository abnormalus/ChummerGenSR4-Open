#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;
namespace Chummer;
internal static class NativeVehiclePickerChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static Task Reply(Forms.DialogResult answer)
    {
        var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        Avalonia.Threading.Dispatcher.UIThread.Post(async () => {
            try
            {
                var deadline = DateTime.UtcNow.AddSeconds(20);
                while (!Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Any())
                {
                    if (DateTime.UtcNow >= deadline) throw new TimeoutException("Vehicle confirmation did not open.");
                    await Task.Delay(10);
                }
                var message = Forms.Application.OpenForms.OfType<Forms.MessageBoxForm>().Last(); message.DialogResult = answer; message.Close(); completion.TrySetResult();
            }
            catch (Exception error) { NativeDialogHost.CancelAll(); completion.TrySetException(error); }
        });
        return completion.Task;
    }
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool,string> check)
    {
        string previousCategory = VehiclePickerModel.LastCategory;
        try
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Options.Books.Clear(); character.Options.Books.Add("SR4");
            var before = character.CaptureSnapshot();
            using (var prompt = new frmSelectVehicle(character, true))
            {
                prompt.Model.SearchText = "Dodge Scoot"; var pending = prompt.ShowDialogAsync(owner); var window = (VehiclePickerWindow)NativeDialogHost.ActiveWindow!; await Task.Delay(100);
                check(window.DiscountInput.Minimum == 20 && window.DiscountInput.Maximum == 50 && window.DiscountInput.Value == 20,
                    "Native vehicle used discount retains historical range and initial value");
                window.UsedInput.IsChecked = true; window.DiscountInput.Text = "35"; await Task.Delay(30); prompt.Model.Markup = 25;
                check(prompt.Model.UsedVehicle && prompt.Model.UsedDiscount == 35 && prompt.Model.CanAccept,
                    "Native vehicle used controls update the shared purchase preview");
                var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previous = nativeOwner.RequestedThemeVariant;
                try
                {
                    foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                    {
                        nativeOwner.RequestedThemeVariant = theme; await Task.Delay(100);
                        check(window.ActualThemeVariant == theme && window.DiscountInput.Bounds.Width >= 110,
                            "Native vehicle picker follows " + (theme == ThemeVariant.Dark ? "dark" : "light") + " owner theme");
                        using var image = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width*2, (int)window.ClientSize.Height*2), new Vector(192,192));
                        image.Render(window); image.Save(Path.Combine(output,"native-vehicle-picker-"+(theme == ThemeVariant.Dark ? "dark" : "light")+".png"),PngBitmapEncoderOptions.Default);
                    }
                }
                finally { nativeOwner.RequestedThemeVariant = previous; }
                window.Accept(true);
                check(await pending == Forms.DialogResult.OK && prompt.SelectedVehicle == "Dodge Scoot (Scooter)" && prompt.UsedVehicle && prompt.UsedCost == 1950 && prompt.UsedAvail == "4" && prompt.Markup == 25 && prompt.AddAgain,
                    "Native vehicle acceptance returns unmarked used price canonical availability and Add More");
                check(character.CaptureSnapshot().AsSpan().SequenceEqual(before), "Native vehicle browsing and acceptance do not purchase inventory");
            }
            using (var prompt = new frmSelectVehicle(character))
            {
                var pending = prompt.ShowDialogAsync(owner); var window = (VehiclePickerWindow)NativeDialogHost.ActiveWindow!;
                window.SearchBox.Text = "missing \"quoted\" vehicle"; await Task.Delay(30);
                check(prompt.Model.IsEmpty && !window.AcceptButton.IsEnabled && prompt.Model.Details == null, "Native vehicle literal search clears stale details and acceptance");
                window.Close(); check(await pending == Forms.DialogResult.Cancel && character.CaptureSnapshot().AsSpan().SequenceEqual(before), "Native vehicle cancellation preserves the complete character");
            }
            foreach (bool career in new[] { false,true })
            {
                var model = new Character(); model.LoadSnapshot(character.CaptureSnapshot()); model.Created = career; model.Nuyen = 100000;
                using Forms.Form editor = career ? new frmCareer(model) { Loading=true,MdiParent=owner } : new frmCreate(model) { Loading=true,MdiParent=owner }; editor.Show();
                try
                {
                    var method = editor.GetType().GetMethod("cmdAddVehicle_Click",Flags)!;
                    void Execute(bool accept, bool free = false)
                    {
                        Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                            var window = (VehiclePickerWindow)NativeDialogHost.ActiveWindow!; window.Model.SearchText="Dodge Scoot";
                            window.Model.UsedVehicle=true; window.Model.UsedDiscount=35; window.Model.Markup=career?25:0; window.Model.FreeCost=free;
                            if(accept) window.Accept(false); else window.Close();
                        });
                        method.Invoke(editor,new object[]{editor,EventArgs.Empty});
                    }
                    int count=model.Vehicles.Count, entries=model.ExpenseEntries.Count; int money=model.Nuyen;
                    Execute(false); check(model.Vehicles.Count==count && model.Nuyen==money && model.ExpenseEntries.Count==entries,
                        "Native "+(career?"career":"creation")+" canceled vehicle purchase preserves inventory funds and ledger");
                    Execute(true); var vehicle=model.Vehicles.Last();
                    check(model.Vehicles.Count==count+1 && vehicle.Cost=="1950" && vehicle.Avail=="4", "Native "+(career?"career":"creation")+" used vehicle purchase preserves saved price and availability");
                    var reopened=new Character(); reopened.LoadSnapshot(model.CaptureSnapshot());
                    check(reopened.Vehicles.Last().InternalId==vehicle.InternalId && reopened.Vehicles.Last().Cost=="1950", "Native "+(career?"career":"creation")+" used vehicle survives legacy persistence round trip");
                    if(career)
                    {
                        int expected=Convert.ToInt32(vehicle.TotalCost*1.25);
                        check(model.Nuyen==money-expected && model.ExpenseEntries.Count==entries+1 && model.ExpenseEntries.Last().Amount==-expected && model.ExpenseEntries.Last().Undo.NuyenType==NuyenExpenseType.AddVehicle,
                            "Native used vehicle career purchase charges markup once and records canonical undo");
                        var purchase = model.ExpenseEntries.Last();
                        int paid=model.Nuyen; Execute(true,true);
                        check(model.Nuyen==paid && model.ExpenseEntries.Count==entries+1 && model.Vehicles.Count==count+2,
                            "Native free vehicle purchase adds inventory without charging or duplicating expenses");
                        void SelectPurchase()
                        {
                            var careerEditor = (frmCareer)editor; careerEditor.PopulateExpenseList();
                            var list = (Forms.ListView)editor.GetType().GetField("lstNuyen",Flags)!.GetValue(editor)!;
                            foreach (Forms.ListViewItem row in list.Items) row.Selected = row.SubItems[3].Text == purchase.InternalId;
                        }
                        SelectPurchase(); var answer = Reply(Forms.DialogResult.Cancel);
                        editor.GetType().GetMethod("tsUndoNuyenExpense_Click",Flags)!.Invoke(editor,new object[]{editor,EventArgs.Empty}); await answer;
                        check(model.Nuyen==paid && model.Vehicles.Contains(vehicle) && model.ExpenseEntries.Contains(purchase),
                            "Native vehicle purchase undo cancellation preserves inventory balance and ledger");
                        SelectPurchase(); answer = Reply(Forms.DialogResult.Yes);
                        editor.GetType().GetMethod("tsUndoNuyenExpense_Click",Flags)!.Invoke(editor,new object[]{editor,EventArgs.Empty}); await answer;
                        check(model.Nuyen==money && !model.Vehicles.Contains(vehicle) && !model.ExpenseEntries.Contains(purchase) && model.Vehicles.Count==count+1,
                            "Native vehicle purchase undo removes the paid canonical vehicle and refunds once");
                        model.Nuyen=0; int remaining=model.Vehicles.Count; var improvements=model.Improvements.Select(item=>item).ToArray();
                        answer = Reply(Forms.DialogResult.OK); Execute(true); await answer;
                        check(model.Nuyen==0 && model.Vehicles.Count==remaining && model.ExpenseEntries.Count==entries && model.Improvements.SequenceEqual(improvements),
                            "Native unaffordable vehicle purchase preserves inventory ledger and existing improvements");
                    }
                }
                finally { editor.GetType().GetField("_blnIsDirty",Flags)!.SetValue(editor,false); editor.Close(); }
            }
        }
        finally { VehiclePickerModel.LastCategory=previousCategory; }
    }
}
