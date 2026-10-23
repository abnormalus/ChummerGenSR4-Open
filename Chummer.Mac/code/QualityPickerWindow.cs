#nullable enable
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Data;

namespace Chummer;

// Standard catalog controls share selection, keyboard navigation, owner theme and PDF access.
// Eligibility remains in the model; this window never mutates a character.
internal sealed class QualityPickerWindow : CatalogPickerWindow<QualityPickerModel, QualityPickerDetails>
{
    internal QualityPickerWindow(QualityPickerModel model, Character character)
        : base(model, character, "Title_SelectQuality", "qualityPicker",
            new[] { (character.Created || character.BuildMethod == CharacterBuildMethod.Karma ? "Label_Karma" : "Label_BP", "Cost") },
            freeDuringCreation: true, showMarkup: false)
    {
        var limit = new CheckBox { Name = "qualityPickerLimitList",
            Content = LanguageManager.Instance.GetString("Checkbox_SelectQuality_LimitList") };
        limit.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(model.LimitList)) { Mode = BindingMode.TwoWay });
        DetailPanel.Children.Add(limit);
        var metagenetic = new CheckBox { Name = "qualityPickerMetagenetic",
            Content = LanguageManager.Instance.GetString("Checkbox_SelectQuality_Metagenetic") };
        metagenetic.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(model.MetageneticOnly)) { Mode = BindingMode.TwoWay });
        metagenetic.Bind(IsVisibleProperty, new Binding(nameof(model.CanFilterMetagenetic)));
        DetailPanel.Children.Add(metagenetic);
    }
}
