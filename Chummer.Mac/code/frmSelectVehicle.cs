#nullable enable
using System;
using System.Linq;
using System.Threading.Tasks;
using System.Xml;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;
using Avalonia.Layout;
using Forms = Majorsilence.Forms;
namespace Chummer;

// Preserve the purchase API while the caller remains the sole inventory/ledger owner.
public sealed class frmSelectVehicle : IDisposable
{
    private readonly Character character;
    internal VehiclePickerModel Model { get; }
    private VehiclePickerWindow? window;
    private CatalogSelection? selection;
    public Forms.DialogResult DialogResult { get; private set; } = Forms.DialogResult.Cancel;
    public string SelectedVehicle => selection?.Name ?? "";
    public bool AddAgain => selection?.AddAgain ?? false;
    public bool FreeCost => selection?.FreeCost ?? false;
    public int Markup => selection?.Markup ?? 0;
    public bool UsedVehicle { get; private set; }
    public int UsedCost { get; private set; }
    public string UsedAvail { get; private set; } = "";
    internal XmlNode SelectedDefinition => XmlManager.Instance.Load("vehicles.xml").SelectNodes("/chummer/vehicles/vehicle")!.Cast<XmlNode>()
        .First(node => node["name"]?.InnerText == selection?.Name && node["category"]?.InnerText == selection?.Category);
    public frmSelectVehicle(Character objCharacter, bool blnCareer = false) { character = objCharacter; Model = new VehiclePickerModel(character, blnCareer); }
    public Forms.DialogResult ShowDialog(Forms.Form owner)
    {
        var pending = ShowDialogAsync(owner); Forms.Backends.Platform.Backend.RunModalLoop(pending); return pending.GetAwaiter().GetResult();
    }
    internal async Task<Forms.DialogResult> ShowDialogAsync(Forms.Form owner)
    {
        if (window != null) throw new InvalidOperationException("Vehicle picker is already open.");
        DialogResult = Forms.DialogResult.Cancel; selection = null; UsedVehicle = false; UsedCost = 0; UsedAvail = "";
        window = new VehiclePickerWindow(Model, character);
        try
        {
            selection = await NativeDialogHost.ShowAsync<CatalogSelection>(window, owner);
            if (selection != null)
            {
                UsedVehicle = Model.UsedVehicle;
                if (UsedVehicle) { UsedCost = Model.UsedCost; UsedAvail = Model.UsedAvailability; }
                DialogResult = Forms.DialogResult.OK;
            }
            return DialogResult;
        }
        finally { window = null; }
    }
    public void Dispose() => window?.Close();
}
internal sealed class VehiclePickerWindow : CatalogPickerWindow<VehiclePickerModel, VehiclePickerDetails>
{
    internal NumericUpDown DiscountInput { get; }
    internal CheckBox UsedInput { get; }
    internal VehiclePickerWindow(VehiclePickerModel model, Character character)
        : base(model, character, "Title_SelectVehicle", "vehicle", new[] {
            ("Label_Handling", "Handling"), ("Label_Accel", "Acceleration"), ("Label_Speed", "Speed"),
            ("Label_Pilot", "Pilot"), ("Label_Body", "Body"), ("Label_Armor", "Armor"), ("Label_Sensor", "Sensor"),
            ("Label_Avail", "Availability"), ("Label_Cost", "Cost"), ("Label_Test", "AvailabilityTest") })
    {
        UsedInput = new CheckBox { Content = LanguageManager.Instance.GetString("Checkbox_SelectVehicle_UsedVehicle") };
        UsedInput.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(model.UsedVehicle)) { Mode = BindingMode.TwoWay });
        DiscountInput = NativeIntegerInput.Create("vehicleUsedDiscount", 20, 50);
        DiscountInput.Bind(NumericUpDown.ValueProperty, new Binding(nameof(model.UsedDiscount)) { Mode = BindingMode.TwoWay });
        var row = new WrapPanel(); row.Children.Add(UsedInput);
        row.Children.Add(new TextBlock { Text = LanguageManager.Instance.GetString("Label_SelectVehicle_Discount"), Margin = new Avalonia.Thickness(10,0), VerticalAlignment = VerticalAlignment.Center });
        row.Children.Add(DiscountInput); row.Children.Add(new TextBlock { Text = "%", VerticalAlignment = VerticalAlignment.Center });
        DetailPanel.Children.Insert(1, row);
    }
}
