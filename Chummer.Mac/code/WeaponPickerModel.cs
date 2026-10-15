#nullable enable
using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Globalization;
using System.Linq;
using System.Xml;
using Majorsilence.Forms;

namespace Chummer;

internal sealed record WeaponCategory(string Code, string DisplayName)
{
    public override string ToString() => DisplayName;
}
internal sealed record WeaponCatalogEntry(string Name, string Category, string DisplayName, XmlNode Definition);
internal sealed record WeaponSelection(string Name, string Category, bool FreeCost, int Markup, bool AddAgain);
internal sealed record WeaponPickerDetails(string Reach, string Damage, string ArmorPenetration, string Mode,
    string RecoilCompensation, string Ammo, string Availability, string Cost, string AvailabilityTest,
    string Source, string SourceTooltip, string Included);

// Owns presentation state, not inventory. Browsing builds an unattached Weapon using the
// established calculations; only the caller creates/purchases one after explicit acceptance.
internal sealed class WeaponPickerModel : INotifyPropertyChanged
{
    private readonly Character character;
    private readonly XmlDocument catalog;
    private WeaponCategory? category;
    private WeaponCatalogEntry? selectedEntry;
    private string searchText = "";
    private int markup;
    private bool freeCost;
    private bool replacingItems;
    private WeaponPickerDetails? details;
    private string error = "";
    internal static string LastCategory { get; set; } = "";
    public event PropertyChangedEventHandler? PropertyChanged;
    public bool IsCareer { get; }
    public IReadOnlyList<WeaponCategory> Categories { get; }
    public IReadOnlyList<WeaponCatalogEntry> Items { get; private set; } = Array.Empty<WeaponCatalogEntry>();
    public WeaponPickerDetails? Details => details;
    public string Error => error;
    public bool HasError => error.Length > 0;
    public bool IsEmpty => Items.Count == 0;
    public bool CanAccept => selectedEntry != null && details != null && !HasError;
    public WeaponCategory? Category
    {
        get => category;
        set { if (category == value) return; category = value; searchText = ""; Refresh(); }
    }
    public string SearchText
    {
        get => searchText;
        set { value ??= ""; if (searchText == value) return; searchText = value; Refresh(); }
    }
    public WeaponCatalogEntry? SelectedEntry
    {
        get => selectedEntry;
        set { if (replacingItems || ReferenceEquals(selectedEntry, value)) return; selectedEntry = value; UpdateDetails(); }
    }
    public int Markup
    {
        get => markup;
        set { value = Math.Clamp(value, -99, 1000); if (markup == value) return; markup = value; UpdateDetails(); }
    }
    public bool FreeCost
    {
        get => freeCost;
        set { value &= IsCareer; if (freeCost == value) return; freeCost = value; UpdateDetails(); }
    }

    internal WeaponPickerModel(Character character, bool career, XmlDocument? catalog = null)
    {
        this.character = character;
        this.catalog = catalog ?? XmlManager.Instance.Load("weapons.xml");
        IsCareer = career;
        Categories = this.catalog.SelectNodes("/chummer/categories/category")!.Cast<XmlNode>()
            .Select(node => new WeaponCategory(node.InnerText, node.Attributes?["translate"]?.InnerText ?? node.InnerText)).ToArray();
        category = Categories.FirstOrDefault(item => item.Code == LastCategory) ?? Categories.FirstOrDefault();
        Refresh();
    }

    // Search is a literal string operation, never interpolated XPath. This handles quotes
    // safely and preserves the legacy cross-category search and translated display names.
    private IEnumerable<WeaponCatalogEntry> AvailableEntries()
    {
        var categoryNames = Categories.ToDictionary(item => item.Code, item => item.DisplayName);
        bool searching = searchText.Length > 0;
        return catalog.SelectNodes("/chummer/weapons/weapon[(" + character.Options.BookXPath() + ")]")!.Cast<XmlNode>()
            .Where(node => searching
                ? node["category"]?.InnerText is not ("Cyberware" or "Gear") &&
                  (node["translate"]?.InnerText ?? node["name"]?.InnerText ?? "").IndexOf(searchText, StringComparison.CurrentCultureIgnoreCase) >= 0
                : node["category"]?.InnerText == category?.Code)
            .Where(node => categoryNames.ContainsKey(node["category"]?.InnerText ?? ""))
            .Select(node => new WeaponCatalogEntry(node["name"]!.InnerText, node["category"]!.InnerText,
                (node["translate"]?.InnerText ?? node["name"]!.InnerText) +
                (searching ? " [" + categoryNames[node["category"]!.InnerText] + "]" : ""), node))
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

    private void Notify(string property) => PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(property));

    private void Changed()
    {
        // Never republish ItemsSource during a selection/detail update: doing so can
        // discard the selection that the user or search just chose.
        foreach (string property in new[] { nameof(Category), nameof(SearchText), nameof(SelectedEntry),
            nameof(Markup), nameof(FreeCost), nameof(Details), nameof(Error), nameof(HasError),
            nameof(IsEmpty), nameof(CanAccept) }) Notify(property);
    }

    private void UpdateDetails()
    {
        details = null; error = "";
        if (selectedEntry == null) { Changed(); return; }
        try
        {
            XmlNode definition = selectedEntry.Definition;
            var weapon = new Weapon(character);
            weapon.Create(definition, character, new TreeNode(), null, null, null);
            double cost = Convert.ToDouble(definition["cost"]!.InnerText, GlobalOptions.Instance.CultureInfo) * (1 + markup / 100.0);
            int testCost = Convert.ToInt32(cost);
            if (freeCost) { cost = 0; testCost = 0; }
            string source = definition["source"]!.InnerText;
            string page = definition["altpage"]?.InnerText ?? definition["page"]!.InnerText;
            var included = definition.SelectNodes("accessories/accessory|mods/mod")!.Cast<XmlNode>()
                .Select(node =>
                {
                    string family = node.Name == "accessory" ? "accessories/accessory" : "mods/mod";
                    XmlNode? item = catalog.SelectNodes("/chummer/" + family)!.Cast<XmlNode>()
                        .FirstOrDefault(candidate => candidate["name"]?.InnerText == node.InnerText);
                    return item?["translate"]?.InnerText ?? item?["name"]?.InnerText ?? node.InnerText;
                }).ToArray();
            details = new WeaponPickerDetails(weapon.TotalReach.ToString(CultureInfo.CurrentCulture), weapon.CalculatedDamage(),
                weapon.TotalAP, weapon.CalculatedMode, weapon.TotalRC, weapon.CalculatedAmmo(), weapon.TotalAvail,
                string.Format(CultureInfo.CurrentCulture, "{0:###,###,##0¥}", cost), character.AvailTest(testCost, weapon.TotalAvail),
                character.Options.LanguageBookShort(source) + " " + page,
                character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page,
                included.Length == 0 ? LanguageManager.Instance.GetString("String_None") : string.Join(Environment.NewLine, included));
        }
        catch (Exception exception)
        {
            // A broken definition must clear stale details and prevent acceptance, not
            // close the application or leave the previous weapon's cost on screen.
            AppDiagnostics.Record("weapon-picker.details-failed", exception);
            error = LanguageManager.Instance.GetString("WeaponPicker_DetailError");
        }
        Changed();
    }

    internal WeaponSelection? Accept(bool addAgain)
    {
        if (!CanAccept) return null;
        // Settings may change through another workflow while this picker is open.
        // Reject a now-disabled source rather than accepting a newly auto-selected row.
        if (!AvailableEntries().Any(item => item.Name == selectedEntry!.Name && item.Category == selectedEntry.Category))
        {
            Refresh();
            error = LanguageManager.Instance.GetString("WeaponPicker_SelectionChanged"); Changed(); return null;
        }
        LastCategory = selectedEntry!.Category;
        return new WeaponSelection(selectedEntry.Name, selectedEntry.Category, freeCost, markup, addAgain);
    }
}
