using System;
using System.Linq;
using Majorsilence.Forms;
namespace Chummer;

internal static class CreationInputLifecycleChecks
{
    internal static void Run(Action<bool, string> check)
    {
        using var attributes = new Panel(); using var parent = new Panel { Width = 500 };
        var body = new NumericUpDown { Name = "nudBOD", Minimum = 1, Maximum = 6, Value = 1 };
        var money = new NumericUpDown { Name = "nudNuyen", Minimum = 0, Maximum = 50, Value = 1 };
        int writes = 0; money.ValueChanged += (_, _) => writes++;
        attributes.Controls.Add(body); parent.Controls.Add(money);
        using var tips = new ToolTip();
        var baseHeader = new Label { Text = "Base" }; var augmentedHeader = new Label { Text = "Augmented" }; var limitsHeader = new Label { Text = "Limits" };
        var augmented = new Label { Text = "Synthetic augmented value" }; var limits = new Label { Text = "Synthetic limits" };
        var section = new CreationInputSection(null, attributes, parent,
            new[] { (body, new Label { Text = "Body" }, limits, augmented) }, money, new Label(), new Label(), tips,
            (baseHeader, augmentedHeader, limitsHeader));
        var grid = (Avalonia.Controls.Grid)section.Inputs["nudBOD"].Parent!;
        Avalonia.Controls.TextBlock Cell(string text) => grid.Children.OfType<Avalonia.Controls.TextBlock>().Single(item => item.Text == text);
        check(Avalonia.Controls.Grid.GetColumn(Cell("Augmented")) == Avalonia.Controls.Grid.GetColumn(Cell("Synthetic augmented value")) &&
            Avalonia.Controls.Grid.GetColumn(Cell("Limits")) == Avalonia.Controls.Grid.GetColumn(Cell("Synthetic limits")) &&
            Avalonia.Controls.Grid.GetColumn(Cell("Augmented")) < Avalonia.Controls.Grid.GetColumn(Cell("Limits")),
            "Creation augmented values and metatype limits remain under their matching native headings");
        augmented.Text = "Updated synthetic augmented value";
        check(Avalonia.Controls.Grid.GetRow(Cell("Updated synthetic augmented value")) > Avalonia.Controls.Grid.GetRow(Cell("Augmented")) &&
            baseHeader.Parent == null && augmentedHeader.Parent == null && limitsHeader.Parent == null,
            "Creation native headings share the data grid and preserve dynamic captions without legacy duplicates");
        section.Inputs["nudBOD"].Value = 2.5m;
        check(body.Value == 1 && section.Inputs["nudBOD"].Value == 1,
            "Creation attribute rejects fractional input without changing controller state");
        section.Inputs["nudNuyen"].Value = 1.5m;
        check(money.Value == 1.5m, "Creation money allocation preserves fractional input");
        writes = 0;
        section.Inputs["nudNuyen"].Value = 2;
        check(money.Value == 2 && writes == 1, "Creation native input routes one accepted change through its owned controller state");
        money.Enabled = false;
        int disabledWrites = writes; decimal disabledValue = money.Value;
        section.Inputs["nudNuyen"].Value = 3;
        check(writes == disabledWrites && money.Value == disabledValue && section.Inputs["nudNuyen"].Value == disabledValue,
            "Disabled creation input rejects and restores a queued native edit");
        money.Value = 4;
        check(writes == disabledWrites + 1 && section.Inputs["nudNuyen"].Value == 4 && !section.Inputs["nudNuyen"].IsEnabled,
            "Controller refresh still updates a disabled creation input without a feedback write");
        money.Enabled = true; section.Inputs["nudNuyen"].Value = 5;
        check(writes == disabledWrites + 2 && money.Value == 5,
            "Re-enabled creation input accepts a subsequent valid native edit");
        section.Dispose();
        int before = writes; decimal value = money.Value;
        section.Inputs["nudNuyen"].Value = 3;
        check(writes == before && money.Value == value,
            "Disposed creation input section rejects a queued native value change");
        RunPresentationChecks(check);
    }
    private static void RunPresentationChecks(Action<bool, string> check)
    {
        using var main = new Form { IsMdiContainer = true, Width = 900, Height = 600 };
        main.Show();
        using var editor = new Form { MdiParent = main, Width = 650, Height = 500 };
        using var common = new Panel { Left = 12, Top = 20, Width = 600, Height = 400 };
        using var attributes = new Panel { Left = 10, Top = 30, Width = 324, Height = 291 };
        common.Controls.Add(attributes); editor.Controls.Add(common); editor.Show();
        var body = new NumericUpDown { Name = "nudBOD", Minimum = 1, Maximum = 6, Value = 1 };
        var money = new NumericUpDown { Name = "nudNuyen", Maximum = 50, Left = 400, Top = 25 };
        attributes.Controls.Add(body); common.Controls.Add(money);
        using var tips = new ToolTip();
        using var section = new CreationInputSection(editor, attributes, common,
            new[] { (body, new Label(), new Label(), new Label()) }, money, new Label { Left = 350, Top = 25 }, new Label(), tips);
        var hosts = main.Controls.OfType<NativeControlHost>().ToArray();
        check(hosts.Length == 2 && !editor.Controls.OfType<NativeControlHost>().Any(),
            "Creation regions belong to the visible presenter rather than the unshown document backend");
        var attributeHost = hosts.Single(host => host.Name == "nativeCreationAttributes");
        check(attributeHost.PointToScreen(System.Drawing.Point.Empty) == attributes.PointToScreen(System.Drawing.Point.Empty),
            "Creation region coordinates match the hosted document content");
        common.Left += 23;
        check(attributeHost.PointToScreen(System.Drawing.Point.Empty) == attributes.PointToScreen(System.Drawing.Point.Empty),
            "Creation region follows movement of its document ancestors");
        common.Visible = false;
        check(hosts.All(host => !host.Visible), "Hidden Common content hides both creation regions");
        common.Visible = true;
        using (var other = new Form { MdiParent = main })
        {
            other.Show();
            check(hosts.All(host => !host.Visible), "Switching documents hides inactive creation regions");
            editor.Select();
            check(hosts.All(host => host.Visible), "Returning to a document restores its creation regions");
        }
        section.Dispose();
        check(!main.Controls.OfType<NativeControlHost>().Any(),
            "Disposing a creation section removes both presenter regions");
    }
}
