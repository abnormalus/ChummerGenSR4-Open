using System;
using System.Linq;
using System.Reflection;
using Majorsilence.Forms;

namespace Chummer;

internal static class MenuOrderChecks
{
    internal static void Run(Action<bool, string> check)
    {
        using var child = new Form();
        using var target = new MenuStrip();
        using var source = new MenuStrip();
        // Use the real compatibility chrome items: they occupy collection positions
        // but are omitted from the Mac system menu projection.
        var chromeType = typeof(Form).Assembly.GetType("Majorsilence.Forms.MdiControlItem")!;
        var kindType = chromeType.GetNestedType("Kind", BindingFlags.NonPublic)!;
        ToolStripMenuItem Chrome(string kind) => (ToolStripMenuItem)Activator.CreateInstance(chromeType,
            BindingFlags.Instance | BindingFlags.NonPublic, null,
            new object[] { child, Enum.Parse(kindType, kind) }, null)!;
        var system = Chrome("System");
        var close = Chrome("Close");
        var file = new ToolStripMenuItem { Text = "File" };
        target.Items.AddRange(new ToolStripItem[] { system, file,
            new ToolStripMenuItem { Text = "Tools" }, new ToolStripMenuItem { Text = "Window" },
            new ToolStripMenuItem { Text = "Help" }, close });
        var edit = new ToolStripMenuItem { Text = "Edit", MergeAction = MergeAction.Insert, MergeIndex = 1 };
        var special = new ToolStripMenuItem { Text = "Special", MergeAction = MergeAction.Insert, MergeIndex = 3 };
        source.Items.AddRange(new ToolStripItem[] { edit, special });
        for (int iteration = 0; iteration < 3; iteration++)
        {
            ToolStripManager.Merge(source, target);
            check(target.Items.Where(item => !chromeType.IsInstanceOfType(item)).Select(item => item.Text)
                .SequenceEqual(new[] { "File", "Edit", "Tools", "Special", "Window", "Help" }),
                "Character menu merge keeps File before Edit with maximized window controls " + iteration);
            check(ReferenceEquals(target.Items[0], system) && ReferenceEquals(target.Items[target.Items.Count - 1], close),
                "Character menu merge retains leading and trailing window controls " + iteration);
            ToolStripManager.RevertMerge(target);
            check(source.Items.Count == 2 && ReferenceEquals(source.Items[0], edit) && ReferenceEquals(source.Items[1], special)
                && ReferenceEquals(target.Items[1], file), "Menu revert preserves root identities and original order " + iteration);
            target.Items.Remove(system);
            target.Items.Remove(close);
            ToolStripManager.Merge(source, target);
            check(target.Items.Select(item => item.Text).SequenceEqual(new[] { "File", "Edit", "Tools", "Special", "Window", "Help" }),
                "Restored document menus keep the same application order without window controls " + iteration);
            ToolStripManager.RevertMerge(target);
            target.Items.Insert(0, system);
            target.Items.Add(close);
        }
    }
}
