#nullable enable
using System;
using System.Xml;
using Avalonia;
using Avalonia.Automation;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Avalonia.Layout;
using Avalonia.Media;

namespace Chummer;

internal sealed partial class CatalogValidationWindow
{
    private readonly ComboBox cboTest;
    private readonly TextBox txtOutput;
    private readonly ProgressBar pgbProgress;
    private readonly Func<string, XmlDocument> loadCatalog;
    internal ComboBox CatalogInput => cboTest;
    internal TextBox Output => txtOutput;
    internal ProgressBar Progress => pgbProgress;
    internal Button TestButton { get; }

    internal CatalogValidationWindow(Func<string, XmlDocument>? loadCatalog = null)
    {
        this.loadCatalog = loadCatalog ?? (file => XmlManager.Instance.Load(file));
        Title = "XML Test";
        Width = 951;
        Height = 604;
        MinWidth = 280;
        MinHeight = 220;
        WindowStartupLocation = WindowStartupLocation.CenterOwner;
        NativeControlPresentation.AddTheme(Styles);

        cboTest = new ComboBox
        {
            ItemsSource = new[]
            {
                "armor.xml", "bioware.xml", "critters.xml", "cyberware.xml", "gear.xml",
                "metatypes.xml", "qualities.xml", "vehicles.xml", "weapons.xml"
            },
            HorizontalAlignment = HorizontalAlignment.Stretch
        };
        AutomationProperties.SetName(cboTest, "Catalog to validate");
        TestButton = new Button { Content = "_Test", IsEnabled = false };
        txtOutput = new TextBox
        {
            Text = "",
            AcceptsReturn = true,
            TextWrapping = TextWrapping.Wrap,
            VerticalContentAlignment = VerticalAlignment.Top
        };
        AutomationProperties.SetName(txtOutput, "Catalog validation output");
        ScrollViewer.SetVerticalScrollBarVisibility(txtOutput, ScrollBarVisibility.Auto);
        ScrollViewer.SetHorizontalScrollBarVisibility(txtOutput, ScrollBarVisibility.Disabled);
        pgbProgress = new ProgressBar();
        AutomationProperties.SetName(pgbProgress, "Catalog validation progress");

        bool validating = false;
        cboTest.SelectionChanged += (_, _) => TestButton.IsEnabled = !validating && cboTest.SelectedItem is string;
        TestButton.Click += async (_, _) =>
        {
            if (validating || cboTest.SelectedItem is not string)
                return;

            validating = true;
            TestButton.IsEnabled = cboTest.IsEnabled = false;
            try { await ValidateCatalogAsync(); }
            finally
            {
                validating = false;
                cboTest.IsEnabled = TestButton.IsEnabled = true;
            }
        };

        var commands = new Grid { ColumnDefinitions = new ColumnDefinitions("*,Auto"), ColumnSpacing = 8 };
        commands.Children.Add(cboTest);
        Grid.SetColumn(TestButton, 1);
        commands.Children.Add(TestButton);
        var layout = new Grid { Margin = new Thickness(12), RowDefinitions = new RowDefinitions("Auto,Auto,*"), RowSpacing = 8 };
        layout.Children.Add(commands);
        Grid.SetRow(pgbProgress, 1);
        layout.Children.Add(pgbProgress);
        Grid.SetRow(txtOutput, 2);
        layout.Children.Add(txtOutput);
        Content = layout;
    }
}
