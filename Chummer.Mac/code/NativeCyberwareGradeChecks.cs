#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;
namespace Chummer;

internal static class NativeCyberwareGradeChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, Action<bool,string> check)
    {
        async Task ExpectAsync(Func<bool> condition, string label)
        {
            var deadline = DateTime.UtcNow.AddSeconds(20);
            while (!condition())
            {
                if (DateTime.UtcNow >= deadline) { check(false, label); throw new TimeoutException("Native Cyberware grade assertion timed out."); }
                await Task.Delay(20);
            }
            check(true, label);
        }
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot());
        if (!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
        var adapsin = new Improvement { ImproveType = Improvement.ImprovementType.Adapsin, Enabled = true };
        character.Improvements.Add(adapsin); byte[] before = character.CaptureSnapshot();
        using (var dialog = new frmSelectCyberware(character) { SetGrade = "Alphaware (Adapsin)" })
        {
            var pending = dialog.ShowDialogAsync(owner); var window = (CyberwarePickerWindow)NativeDialogHost.ActiveWindow!;
            try
            {
                window.SearchBox.Text = "Datajack";
                window.ItemList.SelectedItem = window.Model.Items.Single(item => item.Name == "Datajack");
                await ExpectAsync(() => window.IsVisible && window.Model.SelectedGrade?.Name == "Alphaware (Adapsin)" &&
                    !Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled, "Native Cyberware exposes available Adapsin grade on owned window");
                adapsin.Enabled = false;
                window.AcceptButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
                await ExpectAsync(() => !pending.IsCompleted && window.Model.HasError && dialog.SelectedCyberware == "",
                    "Native Cyberware acceptance rejects Adapsin invalidated after selection");
                window.Close();
                check(await pending == Forms.DialogResult.Cancel && Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled,
                    "Native Cyberware invalid grade cancellation restores owner without accepted draft");
            }
            finally { if (!pending.IsCompleted) window.Close(); adapsin.Enabled = true; }
        }
        check(character.CaptureSnapshot().SequenceEqual(before), "Native Cyberware invalid grade preserves complete character snapshot");

        var catalog = (XmlDocument)XmlManager.Instance.Load("cyberware.xml").CloneNode(true);
        var definition = catalog.CreateElement("cyberware");
        definition.InnerXml = "<name/><category>Headware</category><avail>0</avail><cost>100</cost><ess>0.1</ess><capacity>[1]</capacity><source>SR4</source><page>1</page><forcegrade>Betaware</forcegrade>";
        const string name = "Synthetic \"Ware's\" Forced Node"; definition["name"]!.InnerText = name;
        catalog.SelectSingleNode("/chummer/cyberwares")!.AppendChild(definition);
        var model = new CyberwarePickerModel(character, false, new(InitialCategory: "Headware", InitialGrade: "Standard"), catalog);
        var forcedWindow = new CyberwarePickerWindow(model, character);
        var forcedPending = NativeDialogHost.ShowAsync<CatalogSelection>(forcedWindow, owner);
        try
        {
            forcedWindow.SearchBox.Text = name;
            forcedWindow.ItemList.SelectedItem = model.Items.Single(item => item.Name == name);
            var grade = forcedWindow.GetVisualDescendants().OfType<ComboBox>().Single(input => input.Name == "cyberwarePickerGrade");
            await ExpectAsync(() => forcedWindow.IsVisible && !grade.IsEnabled && model.SelectedGrade?.Name == "Betaware",
                "Native Cyberware quoted forced-grade entry locks canonical Betaware selection");
            forcedWindow.AcceptButton.RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            await ExpectAsync(() => forcedPending.IsCompleted, "Native Cyberware forced-grade OK completes owned window");
            var accepted = await forcedPending; var draft = accepted == null ? null : model.PurchaseDraft(accepted);
            check(draft?.Item.Name == name && draft.Item.Category == "Headware" && draft.Grade.Name == "Betaware" &&
                character.CaptureSnapshot().SequenceEqual(before) && Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner).IsEnabled,
                "Native Cyberware forced-grade OK retains quoted canonical draft and full owner snapshot");
        }
        finally { if (!forcedPending.IsCompleted) forcedWindow.Close(); }
    }
}
