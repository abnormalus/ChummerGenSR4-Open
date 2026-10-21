#nullable enable
using System;
using System.Collections.Generic;
using Avalonia;
using Avalonia.Controls;
using Avalonia.Controls.Primitives;
using Forms = Majorsilence.Forms;

namespace Chummer;

public partial class frmCreate
{
    private readonly Dictionary<string, ToggleButton> creationSkillFilterButtons = new();
    private void SelectCreationSkillFilter(string value)
    {
        if (IsDisposed) return;
        // Reuse the original filter handler and its canonical values. Filtering
        // changes presentation only, including controller visibility/edit guards.
        if (cboSkillFilter.SelectedValue?.ToString() == value)
            cboSkillFilter_SelectedIndexChanged(cboSkillFilter, EventArgs.Empty);
        else
            cboSkillFilter.SelectedValue = value;
    }
    private void InitializeCreationSkillFilterBar()
    {
        const int height = 34;
        foreach (Forms.Control child in tabSkills.Controls)
        {
            if ((child.Anchor & Forms.AnchorStyles.Top) == 0) continue;
            child.Top += height;
            if ((child.Anchor & Forms.AnchorStyles.Bottom) != 0) child.Height = Math.Max(0, child.Height - height);
        }
        var bar = new Grid { ColumnDefinitions = new ColumnDefinitions("*,*,*,*,*,*,*,*,*,*,*,*,*") };
        NativeControlPresentation.AddTheme(bar.Styles);
        var values = new[] { "0", "BOD", "AGI", "REA", "STR", "CHA", "INT", "LOG", "WIL", "EDG", "MAG", "RES", "1" };
        for (int index = 0; index < values.Length; index++)
        {
            string value = values[index];
            string label = value == "0" ? LanguageManager.Instance.GetString("String_SkillFilterAllShort") :
                value == "1" ? LanguageManager.Instance.GetString("String_SkillFilterRankedShort") : LanguageManager.Instance.GetString("String_Attribute" + value + "Short");
            var button = new ToggleButton { Content = label, Padding = new Thickness(3, 2), MinHeight = 28,
                HorizontalAlignment = Avalonia.Layout.HorizontalAlignment.Stretch };
            button.Click += (_, _) => { SelectCreationSkillFilter(value); RefreshButtons(); };
            creationSkillFilterButtons.Add(value, button);
            Grid.SetColumn(button, index); bar.Children.Add(button);
        }
        void RefreshButtons()
        {
            string? selected = cboSkillFilter.SelectedValue?.ToString();
            foreach (var pair in creationSkillFilterButtons) pair.Value.IsChecked = pair.Key == selected;
        }
        EventHandler changed = (_, _) => RefreshButtons();
        cboSkillFilter.SelectedIndexChanged += changed;
        var presenter = new CreationRegionPresenter(this, tabSkills, bar, "nativeCreationSkillFilters",
            () => new System.Drawing.Rectangle(0, 0, tabSkills.ClientSize.Width, height));
        Disposed += (_, _) => { cboSkillFilter.SelectedIndexChanged -= changed; presenter.Dispose(); };
        RefreshButtons();
    }
}
