#nullable enable
using System;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

// Existing purchase contract over a directly owned standard Avalonia window.
// Configuration is captured at Show time; accepted options never mutate inventory here.
public sealed class frmSelectGear : IDisposable
{
    private readonly Character character;
    private readonly bool career;
    private readonly int availabilityModifier, costMultiplier;
    private GearPickerWindow? window;
    private GearPickerSelection? selection;
    private string initialSelection = "", additionalCategories = "";
    private int maximumResponse, maximumSignal, currentFirewall, currentSystem, maximumCapacity = -1;
    private bool positiveCapacity, negativeCapacity, armorCapacity;
    private bool? enableStack;
    private CapacityStyle capacityStyle;
    public DialogResult DialogResult { get; private set; } = DialogResult.Cancel;
    public string InitialCategory { get; set; } = "";
    public string AmmunitionCategory { get; set; } = "";
    public string AllowedCategories { get; set; } = "";
    public string SelectedGear { get => selection?.Item.Name ?? ""; set => initialSelection = value; }
    public string SelectedCategory => selection?.Item.Category ?? "";
    public int SelectedRating => selection?.Item.Rating ?? 0;
    public int SelectedQty => selection?.Item.Quantity ?? 0;
    public int Markup => selection?.Item.Markup ?? 0;
    public bool FreeCost => selection?.Item.FreeCost ?? false;
    public bool AddAgain => selection?.Item.AddAgain ?? false;
    public bool DoItYourself => selection?.DoItYourself ?? false;
    public bool Hacked => selection?.Hacked ?? false;
    public bool Stack => selection?.Stack ?? false;
    public bool InherentProgram => selection?.InherentProgram ?? false;
    public bool Aerodynamic => selection?.Aerodynamic ?? false;
    public int CommlinkResponse { set => maximumResponse = value + 2; }
    public int CommlinkSignal { set => maximumSignal = value + 2; }
    public int CommlinkFirewall { set => currentFirewall = value; }
    public int CommlinkSystem { set => currentSystem = value; }
    public int MaximumCapacity { set => maximumCapacity = value; }
    public bool ShowPositiveCapacityOnly { set { positiveCapacity = value; if (value) negativeCapacity = false; } }
    public bool ShowNegativeCapacityOnly { set { negativeCapacity = value; if (value) positiveCapacity = false; } }
    public bool ShowArmorCapacityOnly { set => armorCapacity = value; }
    public bool EnableStack { set => enableStack = value; }
    public CapacityStyle CapacityDisplayStyle { set => capacityStyle = value; }
    public frmSelectGear(Character character, bool blnCareer = false, int intAvailModifier = 0, int intCostMultiplier = 1)
    {
        this.character = character; career = blnCareer; availabilityModifier = intAvailModifier;
        costMultiplier = intCostMultiplier; capacityStyle = CapacityStyle.Standard;
    }
    public void AddCategory(string categories) => additionalCategories += (additionalCategories.Length == 0 ? "" : ",") + categories;
    internal void PreferCategoryForParent(Gear parent) { if (parent is Commlink) InitialCategory = "Commlink Operating System"; }
    internal GearPickerModel CreateModel() => new(character, career, new GearPickerOptions(AllowedCategories,
        additionalCategories, InitialCategory, initialSelection,
        new GearSelectionContext(positiveCapacity, negativeCapacity, armorCapacity, maximumResponse, maximumSignal, currentFirewall, currentSystem),
        availabilityModifier, costMultiplier, maximumCapacity, capacityStyle, enableStack, AmmunitionCategory));
    public DialogResult ShowDialog(Form owner)
    {
        Task<DialogResult> pending = ShowDialogAsync(owner);
        Majorsilence.Forms.Backends.Platform.Backend.RunModalLoop(pending);
        return pending.GetAwaiter().GetResult();
    }
    public async Task<DialogResult> ShowDialogAsync(Form owner)
    {
        if (window != null) throw new InvalidOperationException("This picker is already open.");
        DialogResult = DialogResult.Cancel; selection = null;
        var model = CreateModel();
        window = new GearPickerWindow(model, character);
        try
        {
            var accepted = await NativeDialogHost.ShowAsync<CatalogSelection>(window, owner);
            if (accepted != null) { selection = model.PurchaseDraft(accepted); DialogResult = DialogResult.OK; }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() { window?.Close(); window = null; }
}
