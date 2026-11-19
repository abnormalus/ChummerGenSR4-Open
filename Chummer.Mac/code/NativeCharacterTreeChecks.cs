#nullable enable
using System;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia.Controls;
using Avalonia.Threading;
using Forms = Majorsilence.Forms;

namespace Chummer;

internal static class NativeCharacterTreeChecks
{
    internal static async Task RunAsync(frmMain main, Character fixture, Action<bool, string> check)
    {
        foreach (bool career in new[] { false, true })
        {
            var character = new Character();
            character.LoadSnapshot(fixture.CaptureSnapshot());
            character.Created = career;
            using Forms.Form editor = career ? new frmCareer(character) : new frmCreate(character);
            editor.MdiParent = main;
            editor.Show();
            character.MAGEnabled = true;
            var metamagic = new Metamagic(character) { Name = "Synthetic metamagic", Source = "SR4", Page = "1" };
            character.Metamagics.Add(metamagic);
            byte[] before = character.CaptureSnapshot();
            T Field<T>(string name) => (T)editor.GetType().GetField(name,
                BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(editor)!;

            var outer = Field<Forms.TabControl>("tabCharacterTabs");
            var gear = Field<Forms.TabControl>("tabStreetGearTabs");
            foreach (var (name, tabName) in new[]
            {
                ("Cyberware", "tabCyberware"), ("Spells", "tabMagician"),
                ("MartialArts", "tabMartialArts"), ("Foci", "tabGear"),
                ("Metamagic", "tabInitiation"), ("CritterPowers", "tabCritter"),
                ("ComplexForms", "tabTechnomancer")
            })
            {
                var tab = Field<Forms.TabPage>(tabName);
                if (name == "Foci")
                {
                    outer.SelectedTab = Field<Forms.TabPage>("tabStreetGear");
                    gear.SelectedTab = tab;
                }
                else
                {
                    if (!outer.TabPages.Contains(tab))
                        outer.TabPages.Add(tab);
                    outer.SelectedTab = tab;
                }

                string label = (career ? "Career" : "Creation") + name;
                var host = main.Controls.OfType<Forms.NativeControlHost>().SingleOrDefault(control => control.Name == "Native" + label);
                check(host != null, label + " installs a native host");
                var source = Field<Forms.TreeView>("tre" + name);
                var tree = (TreeView)host!.NativeControl!;
                var probe = new Forms.TreeNode { Text = "Synthetic tree root", Tag = name == "Metamagic" ? metamagic.InternalId : "" };
                source.Nodes.Add(probe);
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                var row = tree.Items.OfType<TreeViewItem>().SingleOrDefault(item => ReferenceEquals(item.Tag, probe));
                check(row != null, label + " projects an inserted item");
                check(source.Parent == null && host.Visible && tree.IsEffectivelyVisible &&
                    TopLevel.GetTopLevel(tree) != null && tree.Bounds.Height > 0 &&
                    tree.Items.Count == source.Nodes.Count,
                    label + " renders its native rows on the owning tab");
                tree.SelectedItem = row;
                check(source.SelectedNode == probe, label + " routes selection to the editor");
                check(source.CheckBoxes == (row!.Header is CheckBox), label + " preserves the row presentation kind");
                probe.Remove();
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(!tree.Items.Contains(row) && !ReferenceEquals(tree.SelectedItem, row),
                    label + " clears stale selection after removal");
                outer.SelectedTab = Field<Forms.TabPage>("tabCommon");
                await Dispatcher.UIThread.InvokeAsync(() => { }, DispatcherPriority.ApplicationIdle);
                check(!host.Visible, label + " hides when leaving the tab");
            }
            check(before.AsSpan().SequenceEqual(character.CaptureSnapshot()),
                "Tree-only interaction leaves the editor character unchanged");
            editor.Close();
        }
    }
}
