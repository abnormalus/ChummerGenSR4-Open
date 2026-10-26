#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;
namespace Chummer;

// Invoke the real Add buttons. The existing caller creates improvements, charges
// money and records expenses; no test substitutes a result or bypasses that code.
internal static class NativeCyberwarePurchaseChecks
{
    private const BindingFlags Flags = BindingFlags.Instance | BindingFlags.NonPublic;
    private static T Field<T>(object owner, string name) => (T)owner.GetType().GetField(name, Flags)!.GetValue(owner)!;
    internal static async Task RunAsync(Forms.Form owner, Character original, Action<bool,string> check)
    {
        foreach (bool career in new[] { false, true })
        foreach (bool bio in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = career; character.Nuyen = 1000000;
            if (!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
            string catalogName = bio ? "bioware.xml" : "cyberware.xml";
            XmlManager.Instance.Load(catalogName); // Ensure the public catalog cache exists.
            // Load returns a copy. Add the synthetic fixture to the cached application
            // catalog so independently loaded picker/caller definitions agree. This
            // changes no files and is removed in finally below.
            var references = (System.Collections.IEnumerable)typeof(XmlManager)
                .GetField("_lstXmlDocuments", BindingFlags.NonPublic | BindingFlags.Static)!.GetValue(null)!;
            var reference = references.Cast<object>().Single(item =>
                (string)item.GetType().GetProperty("FileName")!.GetValue(item)! == catalogName);
            var catalog = (System.Xml.XmlDocument)reference.GetType().GetProperty("XmlContent")!.GetValue(reference)!;
            string collection = bio ? "biowares" : "cyberwares", element = bio ? "bioware" : "cyberware";
            var source = catalog.SelectSingleNode("/chummer/" + collection + "/" + element + "[name='" + (bio ? "Muscle Toner" : "Datajack") + "']")!;
            var synthetic = source.CloneNode(true); const string name = "Synthetic \"Ware's\" Purchase";
            synthetic["name"]!.InnerText = name;
            if (synthetic["id"] != null) synthetic["id"]!.InnerText = Guid.NewGuid().ToString();
            source.ParentNode!.AppendChild(synthetic);
            using Forms.Form editor = career ? new frmCareer(character) { Loading = true, MdiParent = owner } : new frmCreate(character) { Loading = true, MdiParent = owner };
            editor.Show();
            try
            {
                var history = Field<InventoryUndo>(editor, "_inventoryUndo"); history.Observe();
                byte[] before = character.CaptureSnapshot(); bool dirty = Field<bool>(editor, "_blnIsDirty"); int undo = history.Count;
                int count = character.Cyberware.Count, funds = character.Nuyen, expenses = character.ExpenseEntries.Count;
                async Task Pick(bool accept)
                {
                    var completion = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously); Exception? error = null;
                    Dispatcher.UIThread.Post(async () =>
                    {
                        try
                        {
                            var deadline = DateTime.UtcNow.AddSeconds(20);
                            while (NativeDialogHost.ActiveWindow is not CyberwarePickerWindow)
                            { if (DateTime.UtcNow >= deadline) throw new TimeoutException("Main Cyberware purchase caller did not open."); await Task.Delay(20); }
                            var window = (CyberwarePickerWindow)NativeDialogHost.ActiveWindow!;
                            window.SearchBox.Text = name;
                            window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == name);
                            window.Model.SelectedGrade = window.Model.Grades.Single(grade => grade.Name == "Standard");
                            if (bio) window.Model.Rating = 2;
                            if (accept) window.AcceptButton.RaiseEvent(new RoutedEventArgs(Avalonia.Controls.Button.ClickEvent));
                            else window.Close();
                        }
                        catch (Exception failure) { error = failure; NativeDialogHost.CancelAll(); }
                        finally { completion.TrySetResult(); }
                    });
                    editor.GetType().GetMethod(bio ? "cmdAddBioware_Click" : "cmdAddCyberware_Click", Flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
                    await completion.Task.WaitAsync(TimeSpan.FromSeconds(20));
                    if (error != null)
                    {
                        check(false, "Native main ware purchase callback failed type=" + error.GetType().Name + " career=" + career + " bio=" + bio);
                        throw new InvalidOperationException("Native Cyberware purchase callback failed.", error);
                    }
                }
                await Pick(false);
                check(character.CaptureSnapshot().SequenceEqual(before) && Field<bool>(editor, "_blnIsDirty") == dirty && history.Count == undo &&
                    Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled && NativeDialogHost.Count == 0,
                    "Native main ware caller cancellation preserves full snapshot dirty undo owner career=" + career + " bio=" + bio);
                await Pick(true);
                var bought = character.Cyberware.Last();
                check(character.Cyberware.Count == count + 1 && bought.Name == name && bought.Category == (bio ? "Basic" : "Headware") &&
                    bought.Rating == (bio ? 2 : 0) && bought.SourceType == (bio ? Improvement.ImprovementSource.Bioware : Improvement.ImprovementSource.Cyberware),
                    "Native main ware caller purchases quoted canonical name category mode rating career=" + career + " bio=" + bio);
                check(character.Nuyen == funds - (career ? bought.TotalCost : 0) && character.ExpenseEntries.Count == expenses + (career ? 1 : 0) &&
                    (!career || character.ExpenseEntries.Any(entry => entry.Amount == -bought.TotalCost && entry.Undo?.ObjectId == bought.InternalId)),
                    "Native main ware quoted purchase preserves original funds expense and undo identity career=" + career + " bio=" + bio);
                var loaded = new Character(); loaded.LoadSnapshot(character.CaptureSnapshot());
                check(loaded.Cyberware.Any(item => item.InternalId == bought.InternalId && item.Name == name && item.Category == bought.Category && item.Rating == bought.Rating) &&
                    Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled && NativeDialogHost.Count == 0,
                    "Native main ware quoted purchase round trips canonical identity and restores owner career=" + career + " bio=" + bio);
            }
            finally
            {
                synthetic.ParentNode?.RemoveChild(synthetic);
                editor.GetType().GetField("_blnIsDirty", Flags)!.SetValue(editor, false); editor.Close();
            }
        }
    }
}
