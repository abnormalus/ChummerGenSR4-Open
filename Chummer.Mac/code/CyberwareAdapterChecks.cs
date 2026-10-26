#nullable enable
using System;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal static class CyberwareAdapterChecks
{
    internal static void Run(Character original, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        foreach (bool bio in new[] { false, true })
        {
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = career;
            if (!character.Options.Books.Contains("SR4")) character.Options.Books.Add("SR4");
            character.Options.AllowCyberwareESSDiscounts = true; character.Options.EssenceDecimals = 2;
            byte[] before = character.CaptureSnapshot();
            using var adapter = new frmSelectCyberware(character, career) {
                WindowMode = bio ? frmSelectCyberware.Mode.Bioware : frmSelectCyberware.Mode.Cyberware,
                SetGrade = "Alphaware", MaximumCapacity = 1, CharacterESSMultiplier = 0.8,
                BasicBiowareESSMultiplier = 0.9, GenetechCostMultiplier = 0.7, TransgenicsBiowareCostMultiplier = 0.8 };
            adapter.LockGrade(); var model = adapter.CreateModel();
            model.Category = model.Categories.Single(item => item.Code == (bio ? "Basic" : "Headware"));
            model.SelectedEntry = model.Items.Single(item => item.Name == (bio ? "Muscle Toner" : "Datajack"));
            model.Rating = bio ? 2 : 0;
            check(model.IsBioware == bio && !model.IsGradeEnabled && model.SelectedGrade?.Name == "Alphaware" && model.MaximumCapacityText.Length > 0,
                "Cyberware adapter captures mode caller grade lock and capacity at model creation career=" + career + " bio=" + bio);
            check(model.Details?.Cost == string.Format("{0:###,###,##0¥}", bio ? 32000 : 1000) && model.Details.Essence == (bio ? 0.2 : 0.06).ToString(),
                "Cyberware adapter preserves rated price and composed character Basic grade essence modifiers career=" + career + " bio=" + bio);
            model.EssenceDiscount = 10;
            check(model.Details?.Essence == (bio ? 0.16 : 0.05).ToString(), "Cyberware adapter preserves signed percentage discount in preview career=" + career + " bio=" + bio);
            model.FreeCost = true; var draft = model.AcceptCyberware(true);
            check(draft != null && draft.Item.FreeCost == career && draft.Item.AddAgain && adapter.DialogResult == DialogResult.Cancel &&
                adapter.SelectedCyberware == "" && adapter.SelectedCategory == "" && adapter.SelectedGrade == null && !adapter.AddAgain && !adapter.FreeCost &&
                character.CaptureSnapshot().SequenceEqual(before),
                "Cyberware unattached model draft cannot publish adapter purchase before owned acceptance career=" + career + " bio=" + bio);
        }
        var source = new Character(); source.LoadSnapshot(original.CaptureSnapshot());
        if (!source.Options.Books.Contains("SR4")) source.Options.Books.Add("SR4");
        using var owner = new Form(); owner.Show();
        var notice = AppErrors.Notify;
        try
        {
            int notices = 0, nativeBefore = NativeDialogHost.Count; byte[] before = source.CaptureSnapshot();
            AppErrors.Notify = (_, _, _) => notices++;
            using var empty = new frmSelectCyberware(source) { PluginSelection = true, ShowOnlySubsystems = true, Subsystems = "No matching category" };
            var pending = empty.ShowDialogAsync(owner);
            check(pending.IsCompletedSuccessfully && pending.Result == DialogResult.Cancel && notices == 1 && owner.Enabled &&
                NativeDialogHost.Count == nativeBefore && source.CaptureSnapshot().SequenceEqual(before),
                "Cyberware adapter empty plugin context cancels with notice before native backend and preserves owner snapshot");
            empty.Dispose(); bool rejectedDisposed = false;
            try { empty.ShowDialogAsync(owner).GetAwaiter().GetResult(); }
            catch (ObjectDisposedException) { rejectedDisposed = true; }
            check(rejectedDisposed && NativeDialogHost.Count == nativeBefore, "disposed Cyberware adapter rejects reopening without creating a native window");
        }
        finally { AppErrors.Notify = notice; }
        var catalog = (XmlDocument)XmlManager.Instance.Load("cyberware.xml").CloneNode(true);
        var emptyCategory = catalog.CreateElement("category"); emptyCategory.InnerText = "Synthetic Empty Category";
        catalog.SelectSingleNode("/chummer/categories")!.PrependChild(emptyCategory);
        var acrossCategories = new CyberwarePickerModel(source, false, new(InitialCategory: "Synthetic Empty Category", InitialGrade: "Standard",
            Context: new(PluginSelection: true, SubsystemsOnly: true, Subsystems: "Synthetic Empty Category,Headware")), catalog);
        check(acrossCategories.Items.Count == 0 && acrossCategories.HasCompatibleEntries,
            "Cyberware empty initial category does not hide compatible entries in another allowed category");
    }
}
