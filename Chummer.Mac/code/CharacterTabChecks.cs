#nullable enable
using System;
using System.Reflection;
using Majorsilence.Forms;

namespace Chummer;

internal static class CharacterTabChecks
{
    internal static void Run(Action<bool, string> check)
    {
        using var main = new frmMain(); main.Show();
        var tabs = (TabControl)typeof(frmMain).GetField("tabForms", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(main)!;
        using var first = new Form { MdiParent = main, Text = "First" };
        using var second = new Form { MdiParent = main, Text = "Second" };
        using var third = new Form { MdiParent = main, Text = "Third" };
        first.Show(); second.Show(); third.Show();
        check(tabs.TabCount == 3, "character tabs track three open editors");
        first.Select();
        check(main.ActiveMdiChild == first && tabs.SelectedTab == first.Tag,
            "programmatic character activation selects its existing tab");
        for (int iteration = 0; iteration < 5; iteration++)
        {
            tabs.SelectedTab = (TabPage)second.Tag!;
            check(main.ActiveMdiChild == second, "tab selection activates its character on repeated switches " + iteration);
            first.Select();
        }
        check(tabs.TabCount == 3, "repeated activation reuses existing headers");
        third.Select();
        FormClosingEventHandler cancel = (_, args) => args.Cancel = true;
        third.FormClosing += cancel; third.Close();
        check(tabs.TabCount == 3 && !third.IsDisposed, "cancelled character close retains its tab");
        third.FormClosing -= cancel;
        second.Close();
        check(tabs.TabCount == 2 && second.Tag == null, "closing an inactive character removes its tab and editor reference");
        third.Close();
        check(tabs.TabCount == 1 && !tabs.Visible && main.ActiveMdiChild == first,
            "closing the active character activates the survivor and hides the single tab");
        first.Close();
        check(tabs.TabCount == 0 && !tabs.Visible && main.ActiveMdiChild == null,
            "closing the last character leaves no dead tab headers");
        main.Close();
    }
}
