#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Chummer;

internal sealed record GearPickerOptions(string AllowedCategories = "", string AdditionalCategories = "",
    string InitialCategory = "", string SelectedGear = "", GearSelectionContext? Context = null,
    int AvailabilityModifier = 0, int CostMultiplier = 1, int MaximumCapacity = -1,
    CapacityStyle CapacityStyle = CapacityStyle.Standard, bool? EnableStack = null,
    string AmmunitionCategory = "");
internal sealed record GearPickerDetails(string Availability, string Cost, string AvailabilityTest,
    string Capacity, string Response, string Signal, string System, string Firewall,
    string Source, string SourceTooltip) : ICatalogPickerDetails;
internal sealed record GearPickerSelection(CatalogSelection Item, bool DoItYourself, bool Hacked,
    bool Stack, bool InherentProgram, bool Aerodynamic);

// Unattached purchase draft. Core Gear creation, charging and undo stay in the callers.
internal sealed class GearPickerModel : CatalogPickerModel<GearPickerDetails>, IRatedCatalogPicker, IRangedQuantityCatalogPicker
{
    private static string lastCategory = "";
    private readonly GearPickerOptions options;
    private string? previousName, previousCategory;
    private int rating = 1, quantity = 1;
    private bool publishingInput, doItYourself, hacked, stack, inherentProgram, aerodynamic;
    internal GearSelectionContext SelectionContext { get; set; }
    public int MinimumRating { get; private set; }
    public int MaximumRating { get; private set; }
    public bool IsRatingEnabled => CanAccept && MaximumRating > MinimumRating;
    public int MinimumQuantity => 1;
    public int MaximumQuantity => 100000;
    public int QuantityIncrement { get; private set; } = 1;
    public bool IsQuantityEnabled => CanAccept && (options.AllowedCategories.Length == 0 || options.AllowedCategories == "Ammunition");
    public int Rating
    {
        get => rating;
        set { if (publishingInput) return; int next = Math.Clamp(value, MinimumRating, MaximumRating); if (rating == next) return; rating = next; UpdateDetails(); }
    }
    public int Quantity
    {
        get => quantity;
        set { if (publishingInput) return; int next = Math.Clamp(value, MinimumQuantity, MaximumQuantity); if (quantity == next) return; quantity = next; UpdateDetails(); }
    }
    public bool IsDoItYourselfVisible => Category?.Code == "Commlink Upgrade";
    public bool IsHackedVisible => SelectedEntry is { } entry && SoftwareCategory(entry.Category);
    public bool IsStackVisible => options.EnableStack ?? Character.Created;
    public bool IsAerodynamicVisible => SelectedEntry?.Name.StartsWith("Grenade:", StringComparison.Ordinal) == true;
    public bool IsInherentProgramVisible => IsDigitalCharacter && Character.Options.BookEnabled("UN") && SelectedEntry is { } entry &&
        new[] { "Matrix Programs", "Skillsofts", "Autosofts", "Autosofts, Agent", "Autosofts, Drone" }.Contains(entry.Category) &&
        !entry.Name.StartsWith("Suite:", StringComparison.Ordinal);
    public bool IsInherentProgramEnabled => !Hacked;
    private bool IsDigitalCharacter => Character.Metatype == "A.I." || Character.MetatypeCategory is "Technocritters" or "Protosapients";
    public bool DoItYourself { get => doItYourself; set { if (doItYourself == value) return; doItYourself = value; UpdateDetails(); } }
    public bool Hacked { get => hacked; set { if (hacked == value) return; hacked = value; UpdateDetails(); } }
    public bool Stack { get => stack; set { stack = value && IsStackVisible; Notify(nameof(Stack)); } }
    public bool InherentProgram { get => inherentProgram; set { inherentProgram = value && IsInherentProgramVisible && IsInherentProgramEnabled; Notify(nameof(InherentProgram)); } }
    public bool Aerodynamic { get => aerodynamic; set { aerodynamic = value && IsAerodynamicVisible; Notify(nameof(Aerodynamic)); } }
    public string MaximumCapacityText => options.MaximumCapacity < 0 ? "" :
        LanguageManager.Instance.GetString("Label_MaximumCapacityAllowed") + " " + options.MaximumCapacity;

    private sealed record Prepared(GearPickerOptions Options, XmlDocument Catalog, IReadOnlyList<CatalogCategory> Categories);
    private static Prepared Prepare(Character character, GearPickerOptions? options, XmlDocument? catalog)
    {
        options ??= new GearPickerOptions(); catalog ??= XmlManager.Instance.Load("gear.xml");
        return new Prepared(options, catalog, CategoriesFor(character, catalog, options));
    }
    internal GearPickerModel(Character character, bool career, GearPickerOptions? options = null, XmlDocument? catalog = null)
        : this(character, career, Prepare(character, options, catalog)) { }

    private GearPickerModel(Character character, bool career, Prepared prepared)
        : base(character, career, prepared.Catalog, "/chummer/gears/gear",
            prepared.Categories.Any(item => item.Code == prepared.Options.InitialCategory) ? prepared.Options.InitialCategory : lastCategory,
            "gear-picker", categories: prepared.Categories)
    {
        options = prepared.Options;
        SelectionContext = options.Context ?? DefaultContext;
        stack = character.Created && options.EnableStack != false;
        Refresh();
        if (options.SelectedGear.Length > 0)
            SelectedEntry = Items.FirstOrDefault(entry => entry.Name == options.SelectedGear) ?? SelectedEntry;
    }

    private static GearSelectionContext DefaultContext => new(false, false, false, 0, 0, 0, 0);
    private static bool AmmunitionAllowed(Character character, XmlNode definition, string ammunitionCategory) => ammunitionCategory.Length == 0 ||
        definition["category"]?.InnerText == "Ammunition" && AmmunitionSelectionPolicy.IsCompatible(definition["name"]!.InnerText, ammunitionCategory, character.Options);
    private static IReadOnlyList<CatalogCategory> CategoriesFor(Character character, XmlDocument catalog, GearPickerOptions options)
    {
        var allowed = options.AllowedCategories.Split(',', StringSplitOptions.RemoveEmptyEntries);
        var eligible = catalog.SelectNodes("/chummer/gears/gear[(" + character.Options.BookXPath() + ")]")!.Cast<XmlNode>()
            .Where((options.Context ?? DefaultContext).Allows).Where(node => AmmunitionAllowed(character, node, options.AmmunitionCategory))
            .Select(node => node["category"]?.InnerText).ToHashSet();
        // AddCategory historically seeds entries before the catalog list is loaded.
        var result = options.AdditionalCategories.Split(',', StringSplitOptions.RemoveEmptyEntries).Distinct(StringComparer.Ordinal)
            .Select(code => new CatalogCategory(code, code)).ToList();
        foreach (XmlNode node in catalog.SelectNodes("/chummer/categories/category")!)
        {
            string code = node.InnerText;
            if (options.AllowedCategories.Length > 0 && !allowed.Contains(code, StringComparer.Ordinal)) continue;
            string? show = node.Attributes?["show"]?.InnerText;
            bool explicitlyAllowedHidden = show == "false" && allowed.Contains(code, StringComparer.Ordinal);
            if (show != null && !explicitlyAllowedHidden) continue;
            if (!eligible.Contains(code) && !explicitlyAllowedHidden) continue;
            if (!result.Any(item => item.Code == code)) result.Add(new CatalogCategory(code, node.Attributes?["translate"]?.InnerText ?? code));
        }
        return result.OrderBy(item => item.DisplayName, StringComparer.CurrentCulture).ToArray();
    }
    protected override bool IncludeEntry(XmlNode definition) => Categories.Count > 0 && SelectionContext.Allows(definition) &&
        AmmunitionAllowed(Character, definition, options.AmmunitionCategory);
    protected override bool MatchesSearch(XmlNode definition, string text)
    {
        string label = Categories.FirstOrDefault(item => item.Code == definition["category"]?.InnerText)?.DisplayName ?? "";
        return CatalogSearch.MatchesNameOrCategory(definition, text, label) ||
            CatalogSearch.MatchesNameOrCategory(definition, CatalogSearch.GearQuery(text), label);
    }
    protected override void RememberCategory(string category) => lastCategory = category;
    internal GearPickerSelection? AcceptGear(bool addAgain)
    {
        var selected = Accept(addAgain);
        return selected == null ? null : PurchaseDraft(selected);
    }
    internal GearPickerSelection PurchaseDraft(CatalogSelection selected) => new(selected, DoItYourself, Hacked, Stack,
        InherentProgram && IsInherentProgramVisible && IsInherentProgramEnabled, Aerodynamic);
    private static bool SoftwareCategory(string category) => category.EndsWith("Software", StringComparison.Ordinal) ||
        category.EndsWith("Programs", StringComparison.Ordinal) || category == "Program Options" ||
        category.StartsWith("Autosofts", StringComparison.Ordinal) || category.StartsWith("Skillsoft", StringComparison.Ordinal) ||
        category is "Program Packages" or "Software Suites";

    protected override GearPickerDetails CalculateDetails(CatalogEntry entry)
    {
        var definition = entry.Definition;
        if (entry.Name != previousName || entry.Category != previousCategory)
        {
            QuantityIncrement = Convert.ToInt32(definition["costfor"]?.InnerText ?? "1");
            if (QuantityIncrement < 1) throw new FormatException("Invalid quantity pack size.");
            quantity = Math.Clamp(QuantityIncrement, MinimumQuantity, MaximumQuantity);
            previousName = entry.Name; previousCategory = entry.Category;
        }
        if (!IsDoItYourselfVisible) doItYourself = false;
        if (!IsAerodynamicVisible) aerodynamic = false;
        if (IsDigitalCharacter && Hacked) inherentProgram = false;
        MaximumRating = Math.Max(0, Convert.ToInt32(definition["rating"]!.InnerText));
        // The legacy optional minimum falls back to one when absent or malformed.
        MinimumRating = MaximumRating == 0 ? 0 : int.TryParse(definition["minrating"]?.InnerText, out int minimum) ? minimum : 1;
        rating = Math.Clamp(rating, MinimumRating, MaximumRating);
        var preview = GearPreview.Calculate(Character, Catalog, definition, new GearPreviewRequest(rating, quantity, QuantityIncrement,
            Markup, DoItYourself, Hacked, FreeCost, options.AvailabilityModifier, options.CostMultiplier,
            SelectionContext.ArmorCapacityOnly, options.CapacityStyle));
        string source = definition["source"]!.InnerText, page = definition["altpage"]?.InnerText ?? definition["page"]!.InnerText;
        bool hardware = entry.Category is "Commlink" or "Commlink Upgrade";
        bool operatingSystem = entry.Category is "Commlink Operating System" or "Commlink Operating System Upgrade";
        return new GearPickerDetails(preview.Availability, preview.Cost, preview.AvailabilityTest, preview.Capacity,
            hardware ? definition["response"]!.InnerText : "", hardware ? definition["signal"]!.InnerText : "",
            operatingSystem ? definition["system"]!.InnerText : "", operatingSystem ? definition["firewall"]!.InnerText : "",
            Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page);
    }
    protected override void NotifyAdditionalProperties()
    {
        publishingInput = true;
        try
        {
            foreach (string property in new[] { nameof(MinimumRating), nameof(MaximumRating), nameof(Rating), nameof(IsRatingEnabled),
                nameof(MinimumQuantity), nameof(MaximumQuantity), nameof(QuantityIncrement), nameof(Quantity), nameof(IsQuantityEnabled),
                nameof(IsDoItYourselfVisible), nameof(DoItYourself), nameof(IsHackedVisible), nameof(Hacked), nameof(IsStackVisible), nameof(Stack),
                nameof(IsAerodynamicVisible), nameof(Aerodynamic), nameof(IsInherentProgramVisible), nameof(IsInherentProgramEnabled), nameof(InherentProgram), nameof(MaximumCapacityText) }) Notify(property);
        }
        finally { publishingInput = false; }
    }
}
