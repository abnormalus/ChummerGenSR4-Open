#nullable enable
using System;
using Majorsilence.Forms;

namespace Chummer;

internal static class CharacterTabChecks
{
    internal static void Run(Action<bool, string> check)
    {
        using var main = new frmMain(); main.Show();
        var tabs = main.DocumentTabs;
        using var first = new Form { MdiParent = main, Text = "First" };
        using var second = new Form { MdiParent = main, Text = "Second" };
        using var third = new Form { MdiParent = main, Text = "Third" };
        first.Show(); second.Show(); third.Show();
        check(tabs.Count == 3, "character tabs track three open editors");
        first.Select();
        check(main.ActiveMdiChild == first,
            "programmatic character activation retains its existing document entry");
        for (int iteration = 0; iteration < 5; iteration++)
        {
            second.Select();
            check(main.ActiveMdiChild == second, "document activation succeeds on repeated switches " + iteration);
            first.Select();
        }
        check(tabs.Count == 3, "repeated activation reuses existing headers");
        third.Select();
        FormClosingEventHandler cancel = (_, args) => args.Cancel = true;
        third.FormClosing += cancel; third.Close();
        check(tabs.Count == 3 && !third.IsDisposed, "cancelled character close retains its tab");
        third.FormClosing -= cancel;
        second.Close();
        check(tabs.Count == 2, "closing an inactive character removes its tab and editor reference");
        third.Close();
        check(tabs.Count == 1 && !tabs.Visible && main.ActiveMdiChild == first,
            "closing the active character activates the survivor and hides the single tab");
        first.Close();
        check(tabs.Count == 0 && !tabs.Visible && main.ActiveMdiChild == null,
            "closing the last character leaves no dead tab headers");
        main.Close();
    }
}
