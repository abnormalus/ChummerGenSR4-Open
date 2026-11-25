#nullable enable
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Controls.Templates;
using Avalonia.Data;
using Avalonia.Layout;
using Avalonia.Media;

namespace Chummer;

// The shared window owns standard layout, selection, scrolling, theme and PDF
// access. Grade and numeric constraints belong exclusively to the draft model.
internal sealed class CyberwarePickerWindow : CatalogPickerWindow<CyberwarePickerModel, CyberwarePickerDetails>
{
    internal CyberwarePickerWindow(CyberwarePickerModel model, Character character)
        : base(model, character, model.IsBioware ? "Title_SelectCyberware_Bioware" : "Title_SelectCyberware", "cyberwarePicker",
            new[] { ("Label_Essence", "Essence"), ("Label_Capacity", "Capacity"), ("Label_Avail", "Availability"),
                ("Label_Cost", "Cost"), ("Label_Test", "AvailabilityTest") },
            freeDuringCreation: true, showMarkup: false, freeVisibilityProperty: nameof(model.IsFreeCostVisible))
    {
        var gradeRow = new StackPanel { Name = "CyberwareGradeLayout", Spacing = 4 };
        var gradeCaption = new TextBlock { Text = T("Label_Grade"), TextWrapping = TextWrapping.Wrap };
        gradeRow.Children.Add(gradeCaption);
        var grade = new ComboBox { Name = "cyberwarePickerGrade", HorizontalAlignment = HorizontalAlignment.Stretch,
            ItemTemplate = new FuncDataTemplate<Grade>((item, _) => new TextBlock { Text = item?.DisplayName, TextWrapping = TextWrapping.Wrap }) };
        grade.Bind(ItemsControl.ItemsSourceProperty, new Binding(nameof(model.Grades)));
        grade.Bind(SelectingItemsControl.SelectedItemProperty, new Binding(nameof(model.SelectedGrade)) { Mode = BindingMode.TwoWay });
        grade.Bind(IsEnabledProperty, new Binding(nameof(model.IsGradeEnabled)));
        Avalonia.Automation.AutomationProperties.SetLabeledBy(grade, gradeCaption);
        gradeRow.Children.Add(grade);
        DetailPanel.Children.Insert(1, gradeRow);

        var discountRow = new Grid { Name = "CyberwareDiscountLayout", ColumnDefinitions = new ColumnDefinitions("*,Auto"),
            RowDefinitions = new RowDefinitions("Auto,Auto"), ColumnSpacing = 8, RowSpacing = 4 };
        discountRow.Bind(IsVisibleProperty, new Binding(nameof(model.IsEssenceDiscountVisible)));
        var discountCaption = new TextBlock { Text = T("Label_SelectCyberware_ESSDiscount"),
            TextWrapping = TextWrapping.Wrap };
        Grid.SetColumnSpan(discountCaption, 2);
        discountRow.Children.Add(discountCaption);
        var discount = NativeIntegerInput.Create("cyberwarePickerEssenceDiscount", -100, 100);
        discount.Bind(NumericUpDown.ValueProperty, new Binding(nameof(model.EssenceDiscount)) { Mode = BindingMode.TwoWay });
        discount.Bind(IsEnabledProperty, new Binding(nameof(model.IsEssenceDiscountEnabled)));
        discount.ClearValue(Layoutable.WidthProperty);
        Avalonia.Automation.AutomationProperties.SetLabeledBy(discount, discountCaption);
        Grid.SetRow(discount, 1);
        discountRow.Children.Add(discount);
        var percent = new TextBlock { Text = "%", VerticalAlignment = VerticalAlignment.Center };
        Grid.SetColumn(percent, 1);
        Grid.SetRow(percent, 1);
        discountRow.Children.Add(percent);
        DetailPanel.Children.Insert(3, discountRow);

        var transgenic = new CheckBox { Name = "cyberwarePickerTransgenic", Content = new TextBlock { Text = T("Checkbox_Transgenic"), TextWrapping = TextWrapping.Wrap } };
        transgenic.Bind(ToggleButton.IsCheckedProperty, new Binding(nameof(model.Transgenic)) { Mode = BindingMode.TwoWay });
        transgenic.Bind(IsVisibleProperty, new Binding(nameof(model.IsTransgenicVisible)));
        transgenic.Bind(IsEnabledProperty, new Binding(nameof(model.CanAccept)));
        DetailPanel.Children.Insert(4, transgenic);
        var maximum = new TextBlock { Name = "cyberwarePickerMaximumCapacity", TextWrapping = Avalonia.Media.TextWrapping.Wrap };
        maximum.Bind(TextBlock.TextProperty, new Binding(nameof(model.MaximumCapacityText))); DetailPanel.Children.Add(maximum);
    }
}
