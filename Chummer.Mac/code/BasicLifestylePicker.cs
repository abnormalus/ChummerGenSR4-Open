#nullable enable
using System;
using System.Globalization;
using System.Linq;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Data;
using Avalonia.Layout;
namespace Chummer;

internal sealed record BasicLifestyleSelection(string Name, int Cost, int Dice, int Multiplier,
    string Source, string Page, int Roommates, int Percentage, string ResidenceName, string Notes, bool Purchased)
{
    internal void ApplyTo(Lifestyle target)
    {
        target.Name = Name; target.Cost = Cost; target.Dice = Dice; target.Multiplier = Multiplier;
        target.Source = Source; target.Page = Page; target.Roommates = Roommates;
        target.Percentage = Percentage; target.LifestyleName = ResidenceName;
        target.Notes = Notes; target.Purchased = Purchased;
        // The existing controller owns identity, months and insertion/replacement.
    }
}
internal static class BasicLifestylePrice
{
    internal static int Calculate(int cost, int roommates, int percentage) => Convert.ToInt32(
        (double)cost * (1 + Convert.ToDouble((decimal)roommates / 10)) * Convert.ToDouble((decimal)percentage / 100));
}
internal sealed record BasicLifestyleDetails(string Cost, string StartingMoney, string Source, string SourceTooltip) : ICatalogPickerDetails;
internal sealed class BasicLifestylePickerModel : CatalogPickerModel<BasicLifestyleDetails>
{
    private readonly string? lockedName;
    private readonly string residenceName;
    private readonly string notes;
    private readonly bool purchased;
    private int? roommates = 0, percentage = 100;
    public bool IsEditing => lockedName != null;
    public int? Roommates { get => roommates; set { value = value.HasValue ? Math.Clamp(value.Value, 0, 10) : null; if (roommates == value) return; roommates = value; UpdateDetails(); } }
    public int? Percentage { get => percentage; set { value = value.HasValue ? Math.Clamp(value.Value, 0, 900) : null; if (percentage == value) return; percentage = value; UpdateDetails(); } }
    protected override bool HasCompleteInput => roommates.HasValue && percentage.HasValue;
    internal BasicLifestylePickerModel(Character character, Lifestyle? source = null, Lifestyle? newTarget = null)
        : base(character, false, XmlManager.Instance.Load("lifestyles.xml"), "/chummer/lifestyles/lifestyle", "", "basic-lifestyle-picker", false)
    {
        lockedName = source?.Name;
        residenceName = source?.LifestyleName ?? newTarget?.LifestyleName ?? "";
        // These fields have no editor here. Capture their values, rather than
        // retaining a mutable source reference or dropping them on replacement.
        notes = source?.Notes ?? newTarget?.Notes ?? "";
        purchased = source?.Purchased ?? newTarget?.Purchased ?? false;
        if (source != null) { roommates = Math.Clamp(source.Roommates, 0, 10); percentage = Math.Clamp(source.Percentage, 0, 900); }
        Refresh();
        // The old basic selector opens on the first enabled XML definition
        // (normally Street), even though the shared catalog presents a sorted list.
        if (source == null)
        {
            var first = Catalog.SelectNodes("/chummer/lifestyles/lifestyle[(" + character.Options.BookXPath() + ")]")!.Cast<XmlNode>().FirstOrDefault();
            if (first != null) SelectedEntry = Items.FirstOrDefault(item => item.Name == first["name"]!.InnerText);
        }
    }
    protected override bool IncludeEntry(XmlNode node) => lockedName == null || node["name"]?.InnerText == lockedName;
    protected override void RememberCategory(string category) { }
    protected override void NotifyAdditionalProperties() { Notify(nameof(Roommates)); Notify(nameof(Percentage)); }
    protected override BasicLifestyleDetails CalculateDetails(CatalogEntry entry)
    {
        var node = entry.Definition;
        int Read(string name) => Convert.ToInt32(node[name]!.InnerText, CultureInfo.InvariantCulture);
        string source = node["source"]!.InnerText, page = node["altpage"]?.InnerText ?? node["page"]!.InnerText;
        return new BasicLifestyleDetails(HasCompleteInput ? string.Format("{0:###,###,##0¥}", BasicLifestylePrice.Calculate(Read("cost"), roommates!.Value, percentage!.Value)) : "",
            Read("dice") + "D6 x " + string.Format("{0:###,###,##0¥}", Read("multiplier")),
            Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page);
    }
    internal BasicLifestyleSelection PrepareAccepted(CatalogSelection accepted)
    {
        if (!CanAccept || SelectedEntry!.Name != accepted.Name) throw new InvalidOperationException("Lifestyle selection changed after confirmation.");
        var node = SelectedEntry.Definition;
        int Read(string name) => Convert.ToInt32(node[name]!.InnerText, CultureInfo.InvariantCulture);
        return new BasicLifestyleSelection(accepted.Name, Read("cost"), Read("dice"), Read("multiplier"), node["source"]!.InnerText, node["page"]!.InnerText,
            roommates!.Value, percentage!.Value, residenceName, notes, purchased);
    }
}
internal sealed class BasicLifestylePickerWindow : CatalogPickerWindow<BasicLifestylePickerModel, BasicLifestyleDetails>
{
    internal NumericUpDown RoommatesInput { get; }
    internal NumericUpDown PercentageInput { get; }
    internal BasicLifestylePickerWindow(BasicLifestylePickerModel model, Character character)
        : base(model, character, "Title_SelectLifestyle", "basicLifestyle", new[] { ("Label_Cost", "Cost"), ("Label_SelectLifestyle_StartingNuyen", "StartingMoney") })
    {
        ItemList.IsEnabled = !model.IsEditing; SearchBox.IsEnabled = !model.IsEditing;
        RoommatesInput = NativeIntegerInput.Create("lifestyleRoommates", 0, 10);
        PercentageInput = NativeIntegerInput.Create("lifestylePercentage", 0, 900);
        RoommatesInput.Bind(NumericUpDown.ValueProperty, new Binding(nameof(model.Roommates)) { Mode = BindingMode.TwoWay });
        PercentageInput.Bind(NumericUpDown.ValueProperty, new Binding(nameof(model.Percentage)) { Mode = BindingMode.TwoWay });
        var rows = new StackPanel { Spacing = 10 };
        void Add(string key, NumericUpDown input)
        {
            var row = new StackPanel { Orientation = Orientation.Horizontal, Spacing = 12 };
            row.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString(key), VerticalAlignment = VerticalAlignment.Center }); row.Children.Add(input); rows.Children.Add(row);
        }
        Add("Label_SelectLifestyle_Roommates", RoommatesInput); Add("Label_SelectLifestyle_PercentToPay", PercentageInput);
        DetailPanel.Children.Insert(1, rows);
    }
}
