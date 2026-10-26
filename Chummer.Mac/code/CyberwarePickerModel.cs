#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Chummer;

internal sealed record CyberwarePickerOptions(bool Bioware = false, CyberwareSelectionContext? Context = null,
    string InitialCategory = "", string InitialGrade = "", bool LockGrade = false, int MaximumCapacity = -1,
    double CharacterEssenceMultiplier = 1, double BasicBiowareEssenceMultiplier = 1,
    double GenetechCostMultiplier = 1, double TransgenicCostMultiplier = 1);
internal sealed record CyberwarePickerDetails(string Availability, string Cost, string AvailabilityTest,
    string Essence, string Capacity, string Source, string SourceTooltip) : ICatalogPickerDetails;
internal sealed record CyberwarePickerSelection(CatalogSelection Item, Grade Grade, int EssenceDiscount, bool Transgenic);

// An unattached purchase draft. Existing callers retain inventory creation,
// expense/essence accounting and undo after explicit acceptance.
internal sealed class CyberwarePickerModel : CatalogPickerModel<CyberwarePickerDetails>, IRatedCatalogPicker
{
    private static string lastCategory = "", lastGrade = "";
    private readonly CyberwarePickerOptions options;
    private readonly CyberwareSelectionContext context;
    private Grade? selectedGrade;
    private int rating = 1, essenceDiscount;
    private bool transgenic, publishingInput, gradesChanged;
    public bool IsBioware => options.Bioware;
    internal bool HasCompatibleEntries => CyberwareSelectionPolicy.Entries(Character, Catalog, options.Bioware, context)
        .Any(definition => !string.IsNullOrEmpty(definition["name"]?.InnerText));
    public IReadOnlyList<Grade> Grades { get; private set; } = Array.Empty<Grade>();
    public Grade? SelectedGrade
    {
        get => selectedGrade;
        set
        {
            if (publishingInput || !IsGradeEnabled || value == null || !Grades.Any(grade => grade.Name == value.Name) || selectedGrade?.Name == value.Name) return;
            selectedGrade = Grades.First(grade => grade.Name == value.Name); UpdateDetails();
        }
    }
    public int MinimumRating { get; private set; }
    public int MaximumRating { get; private set; }
    public bool IsRatingEnabled => CanAccept && MaximumRating > MinimumRating;
    public int Rating
    {
        get => rating;
        set { if (publishingInput) return; int next = Math.Clamp(value, MinimumRating, MaximumRating); if (next == rating) return; rating = next; UpdateDetails(); }
    }
    public bool IsEssenceDiscountVisible => Character.Options.AllowCyberwareESSDiscounts;
    public bool IsEssenceDiscountEnabled => IsEssenceDiscountVisible && CanAccept;
    public int EssenceDiscount
    {
        get => essenceDiscount;
        set
        {
            if (publishingInput) return;
            int next = IsEssenceDiscountVisible ? Math.Clamp(value, -100, 100) : 0;
            if (next == essenceDiscount) return;
            essenceDiscount = next; UpdateDetails();
        }
    }
    public bool IsTransgenicVisible => options.Bioware && Character.Options.AllowCustomTransgenics;
    public bool Transgenic
    {
        get => transgenic;
        set { if (publishingInput) return; bool next = value && IsTransgenicVisible; if (transgenic == next) return; transgenic = next; UpdateDetails(); }
    }
    public bool IsFreeCostVisible => IsCareer || Transgenic ||
        SelectedEntry?.Category.Contains("Genetech:", StringComparison.Ordinal) == true && options.TransgenicCostMultiplier != 1;
    protected override bool AllowsFreeCost => IsFreeCostVisible;
    public bool IsGradeEnabled => CanAccept && !options.LockGrade && !Transgenic &&
        SelectedEntry is { } entry && entry.Definition["forcegrade"] == null && !StandardOnly(entry.Category);
    public string MaximumCapacityText => options.MaximumCapacity < 0 ? "" :
        LanguageManager.Instance.GetString("Label_MaximumCapacityAllowed") + " " + options.MaximumCapacity;

    private sealed record Prepared(CyberwarePickerOptions Options, XmlDocument Catalog, IReadOnlyList<CatalogCategory> Categories);
    private static Prepared Prepare(CyberwarePickerOptions? options, XmlDocument? catalog)
    {
        options ??= new(); catalog ??= XmlManager.Instance.Load(options.Bioware ? "bioware.xml" : "cyberware.xml");
        return new(options, catalog, CyberwareSelectionPolicy.Categories(catalog, options.Context ?? new()));
    }
    internal CyberwarePickerModel(Character character, bool career, CyberwarePickerOptions? options = null, XmlDocument? catalog = null)
        : this(character, career, Prepare(options, catalog)) { }
    private CyberwarePickerModel(Character character, bool career, Prepared prepared)
        : base(character, career, prepared.Catalog, prepared.Options.Bioware ? "/chummer/biowares/bioware" : "/chummer/cyberwares/cyberware",
            prepared.Options.InitialCategory.Length > 0 ? prepared.Options.InitialCategory : lastCategory,
            "cyberware-picker", categories: prepared.Categories)
    {
        options = prepared.Options; context = options.Context ?? new(); Refresh();
    }
    protected override bool IncludeEntry(XmlNode definition) => !string.IsNullOrEmpty(definition["name"]?.InnerText) && CyberwareSelectionPolicy.AllowsEntry(definition, context);
    protected override void RememberCategory(string category) => lastCategory = category;
    private static bool StandardOnly(string category) => category.StartsWith("Genetech:", StringComparison.Ordinal) || category is "Symbiont" or "Genetic Infusions";
    private IEnumerable<Grade> AvailableGrades(CatalogEntry entry) =>
        (options.Bioware ? GlobalOptions.BiowareGrades : GlobalOptions.CyberwareGrades)
        .Where(grade => !grade.Adapsin || Character.AdapsinEnabled)
        .Where(grade => !options.Bioware || entry.Category != "Cultured" || !grade.SecondHand);
    private string RequiredGrade(CatalogEntry entry) => entry.Definition["forcegrade"]?.InnerText ??
        (Transgenic || StandardOnly(entry.Category) ? "Standard" : options.LockGrade ? options.InitialGrade : "");

    protected override CyberwarePickerDetails CalculateDetails(CatalogEntry entry)
    {
        if (!IsTransgenicVisible) transgenic = false;
        if (!IsEssenceDiscountVisible) essenceDiscount = 0;
        if (!IsFreeCostVisible) ClearFreeCost();
        var available = AvailableGrades(entry).ToArray();
        if (!Grades.SequenceEqual(available)) { Grades = available; gradesChanged = true; }
        string required = RequiredGrade(entry);
        string desired = required.Length > 0 ? required : selectedGrade?.Name ??
            (options.InitialGrade.Length > 0 ? options.InitialGrade : lastGrade);
        selectedGrade = Grades.FirstOrDefault(grade => grade.Name == desired);
        if (selectedGrade == null && required.Length > 0) throw new InvalidOperationException("Required grade is unavailable.");
        selectedGrade ??= Grades.FirstOrDefault(grade => grade.Name == "Standard") ?? Grades.FirstOrDefault();
        if (selectedGrade == null) throw new InvalidOperationException("No eligible grades.");
        var definition = entry.Definition;
        MaximumRating = definition["rating"] == null ? 0 : Math.Max(0, Convert.ToInt32(definition["rating"]!.InnerText));
        MinimumRating = MaximumRating == 0 ? 0 : Convert.ToInt32(definition["minrating"]?.InnerText ?? "1");
        rating = Math.Clamp(rating, MinimumRating, MaximumRating);
        var preview = CyberwarePreview.Calculate(Character, Catalog, definition, new(
            rating, options.Bioware, selectedGrade.Cost, Convert.ToDouble(selectedGrade.Essence), selectedGrade.Avail,
            options.CharacterEssenceMultiplier, options.BasicBiowareEssenceMultiplier,
            options.GenetechCostMultiplier, options.TransgenicCostMultiplier, IsEssenceDiscountVisible, essenceDiscount, Transgenic, FreeCost));
        string source = definition["source"]!.InnerText, page = definition["altpage"]?.InnerText ?? definition["page"]!.InnerText;
        return new(preview.Availability, preview.Cost, preview.AvailabilityTest, preview.Essence, preview.Capacity,
            Character.Options.LanguageBookShort(source) + " " + page,
            Character.Options.LanguageBookLong(source) + " " + LanguageManager.Instance.GetString("String_Page") + " " + page);
    }
    protected override bool ValidateSelection(CatalogEntry entry)
    {
        string required = RequiredGrade(entry);
        if (selectedGrade == null || !AvailableGrades(entry).Any(grade => grade.Name == selectedGrade.Name) ||
            required.Length > 0 && required != selectedGrade.Name) return RejectChangedSelection();
        return true;
    }
    internal CyberwarePickerSelection? AcceptCyberware(bool addAgain)
    {
        var selection = Accept(addAgain);
        if (selection == null || selectedGrade == null) return null;
        return PurchaseDraft(selection);
    }
    internal CyberwarePickerSelection PurchaseDraft(CatalogSelection selection)
    {
        if (selectedGrade == null) throw new InvalidOperationException("No accepted grade.");
        lastGrade = selectedGrade.Name;
        return new(selection with { Markup = 0 }, selectedGrade, IsEssenceDiscountVisible ? essenceDiscount : 0, Transgenic);
    }
    protected override void NotifyAdditionalProperties()
    {
        publishingInput = true;
        try
        {
            if (gradesChanged) { gradesChanged = false; Notify(nameof(Grades)); }
            foreach (string property in new[] { nameof(SelectedGrade), nameof(IsGradeEnabled), nameof(MinimumRating), nameof(MaximumRating),
                nameof(Rating), nameof(IsRatingEnabled), nameof(EssenceDiscount), nameof(IsEssenceDiscountVisible), nameof(IsEssenceDiscountEnabled),
                nameof(Transgenic), nameof(IsTransgenicVisible), nameof(IsFreeCostVisible), nameof(MaximumCapacityText) }) Notify(property);
        }
        finally { publishingInput = false; }
    }
}
