#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeLifestyleMonthsChecks
{
    internal static async Task RunAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = false;
        using var editor = new frmCreate(character) { Loading = true, MdiParent = owner };
        editor.Show();

        const BindingFlags privateFields = BindingFlags.Instance | BindingFlags.NonPublic;
        T Field<T>(string name) => (T)typeof(frmCreate).GetField(name, privateFields)!.GetValue(editor)!;
        var dirty = typeof(frmCreate).GetField("_blnIsDirty", privateFields)!;
        var outerTabs = Field<Forms.TabControl>("tabCharacterTabs");
        var streetGearTabs = Field<Forms.TabControl>("tabStreetGearTabs");
        var lifestyleTab = Field<Forms.TabPage>("tabLifestyle");
        var tree = Field<Forms.TreeView>("treLifestyles");
        var host = owner.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "NativeCreationLifestyles");
        var layout = (Grid)((Grid)host.NativeControl!).Children[0];
        var details = layout.Children.OfType<ScrollViewer>().Single();
        var row = ((StackPanel)details.Content!).Children.OfType<Grid>().Single(panel => panel.Name == "NativeLifestyleMonths");
        var input = row.Children.OfType<NumericUpDown>().Single();
        var total = row.Children.OfType<TextBlock>().Single(label => label.Name == "nativeLifestyleTotalCost");

        (Lifestyle Model, Forms.TreeNode Node) AddLifestyle(string name, int months)
        {
            var model = new Lifestyle(character) { Name = name, Cost = 1000, Months = months };
            var node = new Forms.TreeNode { Text = name, Tag = model.InternalId };
            character.Lifestyles.Add(model);
            tree.Nodes[0].Nodes.Add(node);
            return (model, node);
        }

        try
        {
            outerTabs.SelectedTab = Field<Forms.TabPage>("tabStreetGear");
            streetGearTabs.SelectedTab = lifestyleTab;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            tree.SelectedNode = tree.Nodes[0];
            check(host.Visible && !row.IsEffectivelyVisible &&
                !lifestyleTab.Controls.OfType<Forms.NumericUpDown>().Any(), "Lifestyle hides placeholder details without a selected item");

            var (first, firstNode) = AddLifestyle("Synthetic lifestyle A", 2);
            tree.SelectedNode = firstNode;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(row.IsEffectivelyVisible && input.Bounds.Width > 0, "Selecting a lifestyle shows its native months editor");

            layout.Width = 480;
            await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
            check(Grid.GetRow(details) == 1 && layout.Children[0].Bounds.Bottom <= details.Bounds.Top,
                "The Lifestyle tree stacks above its scrolling details at narrow widths");
            var caption = row.Children.OfType<TextBlock>().First();
            var captionSource = Field<Forms.Label>("lblLifestyleMonthsLabel");
            string originalCaption = captionSource.Text;
            try
            {
                row.MaxWidth = 150;
                captionSource.Text = "Translated number of months to purchase";
                double enlarged = input.FontSize * 1.5;
                Avalonia.Controls.Documents.TextElement.SetFontSize(row, enlarged);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(input.FontSize == enlarged && caption.FontSize == enlarged && caption.Text == captionSource.Text &&
                    input.Bounds.Width >= 104 && caption.Bounds.Bottom <= input.Bounds.Top && input.Bounds.Bottom <= total.Bounds.Top,
                    "Lifestyle months and total stack without overlap at 150% text");
            }
            finally
            {
                captionSource.Text = originalCaption;
                row.ClearValue(Avalonia.Controls.Documents.TextElement.FontSizeProperty);
                row.ClearValue(Control.MaxWidthProperty);
            }
            layout.ClearValue(Control.WidthProperty);

            tree.SelectedNode = tree.Nodes[0];
            check(!input.IsEnabled && total.Text == "", "No selected lifestyle disables the editor and clears its cost");

            var (second, secondNode) = AddLifestyle("Synthetic lifestyle B", 4);
            var (legacy, legacyNode) = AddLifestyle("Synthetic lifestyle with zero stored months", 0);
            dirty.SetValue(editor, false);
            foreach (var node in new[] { firstNode, secondNode, legacyNode, firstNode })
                tree.SelectedNode = node;
            check(input.IsEnabled && input.Value == 2 && second.Months == 4 && legacy.Months == 0 &&
                !(bool)dirty.GetValue(editor)!, "Changing selection preserves saved months and clean state");

            check(input.Minimum == 1 && input.Maximum == 100 && input.Increment == 1,
                "The Lifestyle spinner keeps whole-month steps and the 1 to 100 range");
            input.Value = 3;
            check(first.Months == 3 && first.TotalCost == 3000 && total.Text == string.Format("= {0:###,###,##0¥}", first.TotalCost) &&
                (bool)dirty.GetValue(editor)!, "Editing months updates the selected lifestyle, displayed total and dirty state");

            input.Value = 101;
            check(input.Value == 100 && first.Months == 100 && first.TotalCost == 100000,
                "Out-of-range input clips to the permanent lifestyle value");
            var reloaded = new Character();
            reloaded.LoadSnapshot(character.CaptureSnapshot());
            check(reloaded.Lifestyles.Single(item => item.InternalId == first.InternalId).Months == 100 &&
                reloaded.Lifestyles.Single(item => item.InternalId == second.InternalId).Months == 4,
                "Lifestyle changes round-trip through character serialization");

            streetGearTabs.SelectedTab = Field<Forms.TabPage>("tabArmor");
            check(!host.Visible, "Leaving Lifestyle hides the native editor");
        }
        finally
        {
            dirty.SetValue(editor, false);
            editor.Close();
        }
        await CheckCareerAsync(owner, fixture, check);
    }

    private static async Task CheckCareerAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = true;
        character.Nuyen = 10000;
        var lifestyle = new Lifestyle(character) { Name = "Synthetic Career lifestyle", Cost = 1000, Months = 2 };
        character.Lifestyles.Clear();
        character.Lifestyles.Add(lifestyle);
        using var editor = new frmCareer(character) { Loading = true, MdiParent = owner };
        editor.Show();
        const BindingFlags flags = BindingFlags.Instance | BindingFlags.NonPublic;
        T Field<T>(string name) => (T)typeof(frmCareer).GetField(name, flags)!.GetValue(editor)!;
        try
        {
            Field<Forms.TabControl>("tabCharacterTabs").SelectedTab = Field<Forms.TabPage>("tabStreetGear");
            Field<Forms.TabControl>("tabStreetGearTabs").SelectedTab = Field<Forms.TabPage>("tabLifestyle");
            var tree = Field<Forms.TreeView>("treLifestyles");
            tree.SelectedNode = tree.Nodes[0].Nodes[0];
            await Task.Delay(50);
            var host = owner.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "NativeCareerLifestyles");
            var layout = (Grid)((Grid)host.NativeControl!).Children[0];
            var details = layout.Children.OfType<ScrollViewer>().Single();
            var months = ((StackPanel)details.Content!).Children.OfType<StackPanel>().Single(item => item.Name == "NativeCareerLifestyleMonths");
            var commands = (WrapPanel)months.Children[1];
            tree.SelectedNode = tree.Nodes[0];
            check(!months.IsEffectivelyVisible, "Career hides lifestyle details when a category is selected");
            tree.SelectedNode = tree.Nodes[0].Nodes[0];
            var add = (Button)commands.Children[0];
            var remove = (Button)commands.Children[1];
            details.MaxWidth = 150;
            double textSize = add.FontSize * 1.5;
            Avalonia.Controls.Documents.TextElement.SetFontSize(details, textSize);
            ((TextBlock)add.Content!).Text = "Translated command to purchase another lifestyle month";
            await Task.Delay(60);
            check(add.FontSize == textSize && add.IsEffectivelyVisible && add.IsEffectivelyEnabled &&
                months.Children[0].Bounds.Bottom <= commands.Bounds.Top &&
                add.Bounds.Right <= commands.Bounds.Width + 1 && add.Bounds.Bottom <= remove.Bounds.Top,
                "Career Lifestyle month commands wrap beneath the readout at 150% text");
            int balance = character.Nuyen;
            int expenses = character.ExpenseEntries.Count;
            add.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(lifestyle.Months == 3 && character.Nuyen == balance - lifestyle.TotalMonthlyCost &&
                character.ExpenseEntries.Count == expenses + 1,
                "Career Lifestyle native purchase keeps the existing month and expense rules");
            remove.RaiseEvent(new Avalonia.Interactivity.RoutedEventArgs(Button.ClickEvent));
            check(lifestyle.Months == 2 && character.ExpenseEntries.Count == expenses + 1,
                "Career Lifestyle native decrease updates the selected model without an additional expense");
        }
        finally
        {
            typeof(frmCareer).GetField("_blnIsDirty", flags)!.SetValue(editor, false);
            editor.Close();
        }
    }
}
