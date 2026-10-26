#nullable enable
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Chummer;

// Standard catalog presentation only. The adapter/callers retain modal ownership
// and purchasing; model options decide which contextual controls are available.
internal sealed class GearPickerWindow : CatalogPickerWindow<GearPickerModel, GearPickerDetails>
{
    internal GearPickerWindow(GearPickerModel model, Character character)
        : base(model, character, "Title_SelectGear", "gearPicker", new[] {
            ("Label_Avail", "Availability"), ("Label_Cost", "Cost"), ("Label_Test", "AvailabilityTest"),
            ("Label_Capacity", "Capacity"), ("Label_Response", "Response"), ("Label_Signal", "Signal"),
            ("Label_System", "System"), ("Label_Firewall", "Firewall") })
    {
        void Option(string name, string label, string property, string visible, string? enabled = null)
        {
            var input = new CheckBox { Name = name, Content = LanguageManager.Instance.GetString(label) };
            input.Bind(ToggleButton.IsCheckedProperty, new Binding(property) { Mode = BindingMode.TwoWay });
            input.Bind(IsVisibleProperty, new Binding(visible));
            if (enabled != null) input.Bind(IsEnabledProperty, new Binding(enabled));
            DetailPanel.Children.Add(input);
        }
        Option("gearPickerDoItYourself", "Label_SelectGear_DoItYourself", nameof(model.DoItYourself), nameof(model.IsDoItYourselfVisible));
        Option("gearPickerHacked", "Label_SelectGear_Hacked", nameof(model.Hacked), nameof(model.IsHackedVisible));
        Option("gearPickerStack", "Label_SelectGear_Stack", nameof(model.Stack), nameof(model.IsStackVisible));
        Option("gearPickerInherentProgram", "Label_SelectGear_InherentProgram", nameof(model.InherentProgram),
            nameof(model.IsInherentProgramVisible), nameof(model.IsInherentProgramEnabled));
        Option("gearPickerAerodynamic", "Checkbox_Aerodynamic", nameof(model.Aerodynamic), nameof(model.IsAerodynamicVisible));
        var maximumCapacity = new TextBlock { Name = "gearPickerMaximumCapacity", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        maximumCapacity.Bind(TextBlock.TextProperty, new Binding(nameof(model.MaximumCapacityText)));
        DetailPanel.Children.Add(maximumCapacity);
    }
}
