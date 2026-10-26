#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq;
using System.Xml;

namespace Chummer;

internal sealed record CatalogCategory(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}
internal sealed record CatalogEntry(string Name, string Category, string DisplayName, XmlNode Definition);
internal sealed record CatalogSelection(string Name, string Category, bool FreeCost, int Markup, bool AddAgain, int Rating = 0, int Quantity = 1);
internal interface ILabeledRatingCatalogPicker
{
    string RatingLabel { get; }
}
internal interface IQuantityCatalogPicker
{
    int Quantity { get; set; }
    bool IsQuantityEnabled { get; }
}

// Opt-in pack/range support. Existing quantity pickers retain their original defaults.
internal interface IRangedQuantityCatalogPicker : IQuantityCatalogPicker
{
    int MinimumQuantity { get; }
    int MaximumQuantity { get; }
    int QuantityIncrement { get; }
}

internal interface ICatalogPickerDetails
{
    string Source { get; }
    string SourceTooltip { get; }
}

// Presentation-only catalog state. Reuse framework bindings without moving item rules
// into the window; derived models calculate unattached previews and never buy items.
internal abstract class CatalogPickerModel<TDetails> : INotifyPropertyChanged where TDetails : class, ICatalogPickerDetails
{
    protected Character Character { get; }
    protected XmlDocument Catalog { get; }
    private readonly string itemXPath;
    private readonly string diagnosticName;
    private CatalogCategory? category;
    private CatalogEntry? selectedEntry;
    private string searchText = "";
    private int markup;
    private bool freeCost;
    private bool replacingItems;
    private TDetails? details;
    private string error = "";
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsCareer { get; }
    public IReadOnlyList<CatalogCategory> Categories { get; }
    public IReadOnlyList<CatalogEntry> Items { get; private set; } = Array.Empty<CatalogEntry>();
    public TDetails? Details => details;
    public string Error => error;
    public bool HasError => error.Length > 0;
    public bool IsEmpty => Items.Count == 0;
    protected virtual bool HasCompleteInput => true;
    public bool CanAccept => selectedEntry != null && details != null && !HasError && HasCompleteInput;
    public CatalogCategory? Category
    {
        get => category;
        set { if (category == value) return; category = value; searchText = ""; Refresh(); }
    }
    public string SearchText
    {
        get => searchText;
        set { value ??= ""; if (searchText == value) return; searchText = value; Refresh(); }
    }
    public CatalogEntry? SelectedEntry
    {
        get => selectedEntry;
        set { if (replacingItems || ReferenceEquals(selectedEntry, value)) return; selectedEntry = value; UpdateDetails(); }
    }
    public int Markup
    {
        get => markup;
        set { value = Math.Clamp(value, -99, 1000); if (markup == value) return; markup = value; UpdateDetails(); }
    }
    protected virtual bool AllowsFreeCost => IsCareer;
    // Normalize a contextual option during detail calculation without recursively
    // entering UpdateDetails; Changed publishes the resulting draft afterwards.
    protected void ClearFreeCost() => freeCost = false;
    protected virtual bool ValidateSelection(CatalogEntry entry) => true;
    public bool FreeCost
    {
        get => freeCost;
        set { value &= AllowsFreeCost; if (freeCost == value) return; freeCost = value; UpdateDetails(); }
    }

    protected CatalogPickerModel(Character character, bool career, XmlDocument catalog,
        string itemXPath, string lastCategory, string diagnosticName, bool hasCategories = true, IReadOnlyList<CatalogCategory>? categories = null)
    {
        Character = character; Catalog = catalog; this.itemXPath = itemXPath;
        this.diagnosticName = diagnosticName; IsCareer = career;
        Categories = !hasCategories ? Array.Empty<CatalogCategory>() : categories ?? catalog.SelectNodes("/chummer/categories/category")!.Cast<XmlNode>()
            .Select(node => new CatalogCategory(node.InnerText, node.Attributes?["translate"]?.InnerText ?? node.InnerText)).ToArray();
        category = Categories.FirstOrDefault(item => item.Code == lastCategory) ?? Categories.FirstOrDefault();
        // Derived constructors call Refresh after their own initialization is complete.
    }
    protected virtual string DisplayName(XmlNode definition) => definition["translate"]?.InnerText ?? definition["name"]!.InnerText;
    protected virtual bool MatchesSearch(XmlNode definition, string text) => CatalogSearch.MatchesDisplayName(definition, text);
    protected virtual bool IncludeInSearch(XmlNode definition) => true;
    protected virtual bool IncludeEntry(XmlNode definition) => true;
    protected abstract TDetails CalculateDetails(CatalogEntry entry);
    protected abstract void RememberCategory(string category);

    // Search is a literal string operation, never interpolated XPath. This handles quotes
    // safely and preserves the legacy cross-category search and translated display names.
    private IEnumerable<CatalogEntry> AvailableEntries()
    {
        var categoryNames = Categories.ToDictionary(item => item.Code, item => item.DisplayName);
        bool searching = searchText.Length > 0;
        return Catalog.SelectNodes(itemXPath + "[(" + Character.Options.BookXPath() + ")]")!.Cast<XmlNode>()
            .Where(IncludeEntry)
            .Where(node => searching
                ? IncludeInSearch(node) &&
                  MatchesSearch(node, searchText)
                : Categories.Count == 0 || node["category"]?.InnerText == category?.Code)
            .Where(node => Categories.Count == 0 || categoryNames.ContainsKey(node["category"]?.InnerText ?? ""))
            .Select(node => new CatalogEntry(node["name"]!.InnerText, node["category"]?.InnerText ?? "",
                DisplayName(node) +
                (searching && Categories.Count > 0 ? " [" + categoryNames[node["category"]!.InnerText] + "]" : ""), node))
            .OrderBy(item => item.DisplayName, StringComparer.CurrentCulture);
    }

    internal void Refresh()
    {
        var previous = selectedEntry;
        // Replacing ItemsSource clears the view's selection synchronously. Ignore that
        // temporary feedback, then publish the intended selection after the new rows.
        replacingItems = true;
        try
        {
            Items = AvailableEntries().ToArray();
            Notify(nameof(Items));
        }
        finally { replacingItems = false; }
        selectedEntry = Items.FirstOrDefault(item => item.Name == previous?.Name && item.Category == previous.Category)
            ?? Items.FirstOrDefault();
        UpdateDetails();
    }

    protected void Notify(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));
    protected bool RejectChangedSelection()
    {
        Refresh();
        error = LanguageManager.Instance.GetString("CatalogPicker_SelectionChanged");
        Changed();
        return false;
    }

    protected virtual void NotifyAdditionalProperties() { }

    private void Changed()
    {
        // Never republish ItemsSource during a selection/detail update: doing so can
        // discard the selection that the user or search just chose.
        foreach (string property in new[] { nameof(Category), nameof(SearchText), nameof(SelectedEntry),
            nameof(Markup), nameof(FreeCost), nameof(Details), nameof(Error), nameof(HasError),
            nameof(IsEmpty), nameof(CanAccept) }) Notify(property);
        NotifyAdditionalProperties();
    }

    protected void UpdateDetails()
    {
        details = null; error = "";
        if (selectedEntry == null) { Changed(); return; }
        try
        {
            details = CalculateDetails(selectedEntry);
        }
        catch (Exception exception)
        {
            // A broken definition must clear stale details and prevent acceptance, not
            // close the application or leave the previous item's cost on screen.
            AppDiagnostics.Record(diagnosticName + ".details-failed", exception);
            error = LanguageManager.Instance.GetString("CatalogPicker_DetailError");
        }
        Changed();
    }

    internal CatalogSelection? Accept(bool addAgain)
    {
        if (!CanAccept) return null;
        // Settings may change through another workflow while this picker is open.
        // Reject a now-disabled source rather than accepting a newly auto-selected row.
        if (!AvailableEntries().Any(item => item.Name == selectedEntry!.Name && item.Category == selectedEntry.Category))
        {
            RejectChangedSelection(); return null;
        }
        if (!ValidateSelection(selectedEntry!)) return null;
        RememberCategory(selectedEntry!.Category);
        return new CatalogSelection(selectedEntry.Name, selectedEntry.Category, freeCost, markup, addAgain, this is IRatedCatalogPicker rated ? rated.Rating : 0, this is IQuantityCatalogPicker counted ? counted.Quantity : 1);
    }
}
