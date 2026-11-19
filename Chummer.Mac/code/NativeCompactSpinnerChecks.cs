#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Interactivity;
using Avalonia.Threading;
using Avalonia.VisualTree;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCompactSpinnerChecks
{
    internal static async Task RunAsync(frmMain owner, Character fixture, Action<bool, string> check)
    {
        const BindingFlags fields = BindingFlags.Instance | BindingFlags.NonPublic;

        using (var options = new frmOptions())
        {
            options.Show();
            await Idle();
            var sources = typeof(frmOptions).GetFields(fields).Where(field => field.FieldType == typeof(Forms.NumericUpDown))
                .Select(field => (Forms.NumericUpDown)field.GetValue(options)!).ToArray();
            var hosts = Descendants(options.Controls.Cast<Forms.Control>()).OfType<Forms.NativeControlHost>()
                .Where(host => host.NativeControl is Border { Child: NumericUpDown }).ToDictionary(host => host.Name);
            check(sources.Length == 63 && hosts.Count == sources.Length, "All 63 Preferences numeric fields have native hosts");
            foreach (var source in sources)
            {
                var host = hosts["native" + source.Name];
                var input = Input(host);
                Reveal(host.Parent);
                await Idle();
                check(source.Parent == null && input.Minimum == source.Minimum && input.Maximum == source.Maximum &&
                    input.Increment == source.Increment && input.Value == source.Value && input.IsEnabled == source.Enabled,
                    source.Name + " preserves Preferences range, increment, value and enabled state");
                if (host.Parent is Forms.ScrollableControl { AutoScroll: true } scroll && host.Bottom > scroll.ClientSize.Height)
                {
                    scroll.AutoScrollPosition = new System.Drawing.Point(0, -scroll.AutoScrollPosition.Y + host.Bottom - scroll.ClientSize.Height + 12);
                    await Idle();
                }
                await Idle();
                CheckLayout(input, source.Name + $" (tab={host.Parent?.Name}, host={host.Visible}/{host.Bounds}, valueVisible={input.IsVisible})");
            }

            var houseRulesHost = hosts["nativenudForbiddenCostMultiplier"];
            var houseRules = (Forms.TabPage)houseRulesHost.Parent!;
            Reveal(houseRules);
            var originalExtent = houseRules.AutoScrollMinSize;
            houseRules.AutoScrollMinSize = new System.Drawing.Size(originalExtent.Width, originalExtent.Height + houseRules.ClientSize.Height);
            houseRules.AutoScrollPosition = new System.Drawing.Point(0, houseRulesHost.Bottom + 20);
            await Idle();
            check(houseRulesHost.Bottom < 0 && !Input(houseRulesHost).IsEffectivelyVisible,
                "A Preferences spinner scrolled above House Rules is clipped out of the native overlay");
            houseRules.AutoScrollPosition = System.Drawing.Point.Empty;
            houseRules.AutoScrollMinSize = originalExtent;
            await Idle();
            check(Input(houseRulesHost).IsEffectivelyVisible,
                "The Preferences spinner returns when House Rules scrolls back into view");

            var autosave = sources.Single(source => source.Name == "nudCharacterAutosaveMinutes");
            var autosaveHost = hosts["native" + autosave.Name];
            Reveal(autosaveHost.Parent);
            var enable = (Forms.CheckBox)typeof(frmOptions).GetField("chkCharacterAutosave", fields)!.GetValue(options)!;
            enable.Checked = true;
            autosave.Value = autosave.Minimum;
            await Idle();
            var autosaveInput = Input(autosaveHost);
            autosaveInput.GetVisualDescendants().OfType<Button>().Single(button => button.Name == "PART_IncreaseButton")
                .RaiseEvent(new RoutedEventArgs(Button.ClickEvent));
            check(autosave.Value == autosave.Minimum + autosave.Increment,
                "Preferences native autosave spinner button updates the value used by Apply");
            enable.Checked = false;
            check(!autosaveInput.IsEnabled, "Disabling Preferences autosave disables its native interval");
            options.Close();
        }

        var character = new Character();
        character.LoadSnapshot(fixture.CaptureSnapshot());
        character.Created = false;
        using var creation = new frmCreate(character) { MdiParent = owner };
        creation.Show();
        var tabs = (Forms.TabControl)typeof(frmCreate).GetField("tabCharacterTabs", fields)!.GetValue(creation)!;
        foreach (var (name, tabName) in new[]
        {
            ("nudCyberwareRating", "tabCyberware"), ("nudArmorRating", "tabArmor"),
            ("nudMartialArtsRating", "tabMartialArts"), ("nudComplexFormRating", "tabTechnomancer"),
            ("nudMysticAdeptMAGMagician", "tabCommon"), ("nudSignal", "tabCommon"), ("nudResponse", "tabCommon")
        })
        {
            var source = (Forms.NumericUpDown)typeof(frmCreate).GetField(name, fields)!.GetValue(creation)!;
            var page = (Forms.TabPage)typeof(frmCreate).GetField(tabName, fields)!.GetValue(creation)!;
            if (tabName == "tabTechnomancer" && !tabs.TabPages.Contains(page))
                tabs.TabPages.Add(page);
            Reveal(page);
            var host = owner.Controls.OfType<Forms.NativeControlHost>().Single(item => item.Name == "native" + name);
            var input = Input(host);
            check(source.Parent == null && input.Minimum == source.Minimum && input.Maximum == source.Maximum &&
                input.Value == source.Value && input.IsEnabled == source.Enabled && input.IsVisible == source.Visible,
                name + " preserves the Creation controller range, value, and availability");
            bool visible = source.Visible;
            source.Visible = true;
            await Idle();
            CheckLayout(input, name);
            source.Visible = visible;
        }
        var grades = new GradeList();
        grades.LoadList(Improvement.ImprovementSource.Cyberware);
        var ware = new Cyberware(character);
        var wareNode = new Forms.TreeNode();
        ware.Create(XmlManager.Instance.Load("cyberware.xml")
                .SelectSingleNode("/chummer/cyberwares/cyberware[name='Olfactory Booster']")!,
            character, grades.GetGrade("Standard"), Improvement.ImprovementSource.Cyberware, 1, wareNode,
            new List<Weapon>(), new List<Forms.TreeNode>(), false, false);
        character.Cyberware.Add(ware);
        var cyberwareTree = Field<Forms.TreeView>("treCyberware");
        cyberwareTree.Nodes[0].Nodes.Add(wareNode);
        Reveal(Field<Forms.TabPage>("tabCyberware"));
        cyberwareTree.SelectedNode = wareNode;
        var wareInput = Input(owner.Controls.OfType<Forms.NativeControlHost>()
            .Single(host => host.Name == "nativenudCyberwareRating"));
        await Idle();
        wareInput.Value = 2;
        check(ware.Rating == 2 && wareNode.Text == ware.DisplayName &&
            Field<Forms.Label>("lblCyberwareEssence").Text == ware.CalculatedESS.ToString(),
            "Editing native installed Cyberware rating updates the model, row, and essence");

        var armorCatalog = XmlManager.Instance.Load("armor.xml");
        var armor = new Armor(character);
        var armorNode = new Forms.TreeNode();
        armor.Create(armorCatalog.SelectSingleNode("/chummer/armors/armor[name='Armor Vest']")!, armorNode, null, false, false);
        var modification = new ArmorMod(character);
        var modificationNode = new Forms.TreeNode();
        modification.Create(armorCatalog.SelectSingleNode("/chummer/mods/mod[name='Fire Resistance']")!,
            modificationNode, 1, new List<Weapon>(), new List<Forms.TreeNode>());
        modification.Parent = armor;
        armor.ArmorMods.Add(modification);
        armorNode.Nodes.Add(modificationNode);
        character.Armor.Add(armor);
        var armorTree = Field<Forms.TreeView>("treArmor");
        armorTree.Nodes[0].Nodes.Add(armorNode);
        Reveal(Field<Forms.TabPage>("tabArmor"));
        armorTree.SelectedNode = modificationNode;
        var armorInput = Input(owner.Controls.OfType<Forms.NativeControlHost>()
            .Single(host => host.Name == "nativenudArmorRating"));
        await Idle();
        armorInput.Value = 3;
        check(modification.Rating == 3 && modificationNode.Text == modification.DisplayName &&
            Field<Forms.Label>("lblArmorCost").Text.Contains("300", StringComparison.Ordinal),
            "Editing native installed Armor rating updates the model, row, and cost");
        var reopened = new Character();
        reopened.LoadSnapshot(character.CaptureSnapshot());
        check(reopened.Cyberware.Single(item => item.InternalId == ware.InternalId).Rating == 2 &&
            reopened.Armor.Single(item => item.InternalId == armor.InternalId).ArmorMods.Single().Rating == 3,
            "Edited Cyberware and Armor ratings survive the existing character format");
        typeof(frmCreate).GetField("_blnIsDirty", fields)!.SetValue(creation, false);
        creation.Close();

        T Field<T>(string name) => (T)typeof(frmCreate).GetField(name, fields)!.GetValue(creation)!;

        void CheckLayout(NumericUpDown input, string name)
        {
            var textBox = input.GetVisualDescendants().OfType<TextBox>().FirstOrDefault();
            var buttons = input.GetVisualDescendants().OfType<Button>().Where(button =>
                button.Name is "PART_IncreaseButton" or "PART_DecreaseButton").ToArray();
            check(TopLevel.GetTopLevel(input) != null && input.IsEffectivelyVisible && textBox is { Bounds.Width: >= 10, Bounds.Height: >= 16 } &&
                buttons.Count(button => button.IsEffectivelyVisible && button.Bounds.Width >= 9) == 2,
                name + $" lays out a readable input and both spinner buttons (visible={input.IsEffectivelyVisible}, input={input.Bounds.Size}, text={textBox?.Bounds.Size}, buttons={string.Join("/", buttons.Select(button => $"{button.GetType().Name}.{button.TemplatedParent?.GetType().Name}:{button.Bounds.Size}/{button.Width}/{button.MinWidth}/{button.MaxWidth}"))})");
        }

        static NumericUpDown Input(Forms.NativeControlHost host) => (NumericUpDown)((Border)host.NativeControl!).Child!;
        static async Task Idle() => await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
        static void Reveal(Forms.Control? control)
        {
            if (control == null) return;
            Reveal(control.Parent);
            if (control is Forms.TabPage page && page.Parent is Forms.TabControl tabs)
                tabs.SelectedTab = page;
        }
        static IEnumerable<Forms.Control> Descendants(IEnumerable<Forms.Control> roots) =>
            roots.SelectMany(control => Descendants(control.Controls.Cast<Forms.Control>()).Prepend(control));
    }
}
