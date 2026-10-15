#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using SkiaSharp;
using Majorsilence.Forms;
using Majorsilence.Forms.Headless;

namespace Chummer;

internal static class RenderingChecks
{
    // Compare a normal cached frame with a fresh frame of exactly the same state.
    // Save both images only on failure so a platform discrepancy can be inspected.
    private static void CompareFrames(Form window, string output, string transition, Action<bool, string> check)
    {
        byte[] cached = HeadlessRenderer.CapturePng(window, 1120, 760);
        byte[] complete;
        bool previousMode = RenderingDiagnostics.ForceFullRedraw;
        try
        {
            RenderingDiagnostics.ForceFullRedraw = true;
            complete = HeadlessRenderer.CapturePng(window, 1120, 760);
        }
        finally { RenderingDiagnostics.ForceFullRedraw = previousMode; }
        bool matches = cached.SequenceEqual(complete);
        if (!matches)
        {
            File.WriteAllBytes(Path.Combine(output, transition + "-cached.png"), cached);
            File.WriteAllBytes(Path.Combine(output, transition + "-complete.png"), complete);
        }
        check(matches, "cached rendering matches a fresh frame after " + transition);
    }

    internal static void Run(string output, Action<bool, string> check)
    {
        bool previousMode = RenderingDiagnostics.ForceFullRedraw;
        double previousScale = Application.UiScale;
        try
        {
            RenderingDiagnostics.ForceFullRedraw = false;
            foreach (double scale in new[] { 1.0, 2.0 })
            {
                Application.UiScale = scale;
                using var window = new Form { Width = 560, Height = 380 };
                var details = new Panel { Left = 240, Top = 5, Width = 300, Height = 340 };
                var value = new Label { Left = 10, Top = 10, AutoSize = true, Text = "Capacity: 1250 / 7500¥" };
                details.Controls.Add(value);
                var choices = new ListBox { Left = 10, Top = 70, Width = 230, Height = 180 };
                for (int index = 0; index < 25; index++) choices.Items.Add("Quality choice " + index);
                details.Controls.Add(choices);
                var tree = new TreeView { Left = 5, Top = 5, Width = 220, Height = 340 };
                var equipment = new TreeNode("Selected Gear");
                for (int index = 0; index < 35; index++)
                    equipment.Nodes.Add(new TreeNode("Equipment " + index + " (a long editable detail)"));
                tree.Nodes.Add(equipment); equipment.Expand();
                window.Controls.Add(details); window.Controls.Add(tree);
                int paints = 0;
                value.Paint += (_, _) => paints++;
                window.Show();
                HeadlessRenderer.CapturePng(window, 1120, 760);

                paints = 0;
                window.Invalidate(false);
                HeadlessRenderer.CapturePng(window, 1120, 760);
                check(paints == 0, "normal window invalidation reuses unchanged child pixels at scale " + scale);
                window.Invalidate(true);
                HeadlessRenderer.CapturePng(window, 1120, 760);
                check(paints > 0, "window child invalidation redraws nested cached detail controls at scale " + scale);

                string suffix = "-scale-" + scale;
                choices.SelectedIndex = 3;
                CompareFrames(window, output, "list-selection" + suffix, check);
                choices.Items[3] = "Short";
                CompareFrames(window, output, "edited-list-choice" + suffix, check);
                value.Text = "8";
                CompareFrames(window, output, "shorter-detail" + suffix, check);
                value.Left = 70;
                CompareFrames(window, output, "moved-detail" + suffix, check);
                value.Visible = false;
                CompareFrames(window, output, "hidden-detail" + suffix, check);
                value.Visible = true;
                CompareFrames(window, output, "restored-detail" + suffix, check);
                tree.SelectedNode = equipment.Nodes[2];
                CompareFrames(window, output, "inventory-selection" + suffix, check);
                equipment.Nodes[2].Text = "Medkit";
                CompareFrames(window, output, "edited-inventory-detail" + suffix, check);
                tree.TopNode = equipment.Nodes[20];
                CompareFrames(window, output, "inventory-scroll" + suffix, check);
                tree.Width = 180; details.Width = 260;
                CompareFrames(window, output, "resized-inventory" + suffix, check);
                equipment.Collapse();
                CompareFrames(window, output, "collapsed-inventory" + suffix, check);
                equipment.Expand();
                CompareFrames(window, output, "expanded-inventory" + suffix, check);
                byte[] resizedFrame = HeadlessRenderer.CapturePng(window, 1120, 760);
                File.WriteAllBytes(Path.Combine(output, "inventory-rendering" + suffix + ".png"), resizedFrame);
                using (var image = SKBitmap.Decode(resizedFrame))
                {
                    SKColor background = image.GetPixel((int)(530 * scale), (int)(350 * scale));
                    bool oldFootprintCleared = true;
                    for (int x = (int)(195 * scale); x < (int)(215 * scale); x++)
                        for (int y = (int)(50 * scale); y < (int)(200 * scale); y++)
                            oldFootprintCleared &= image.GetPixel(x, y) == background;
                    check(oldFootprintCleared, "resized inventory clears its previous footprint at scale " + scale);
                }

                // Exercise the scroll-blit shortcut, then request a true full refresh
                // before it can consume its pending "reuse the patched buffer" flag.
                foreach (TreeNode node in equipment.Nodes) node.Text = "Gear " + node.Index;
                HeadlessRenderer.CapturePng(window, 1120, 760);
                typeof(Control).GetMethod("RaiseScrollGesture", BindingFlags.NonPublic | BindingFlags.Instance)!
                    .Invoke(tree, new object[] { new ScrollGestureEventArgs(20, 40, new System.Drawing.Point(0, -3)) });
                window.Invalidate(true);
                CompareFrames(window, output, "full-refresh-after-scroll-blit" + suffix, check);

                paints = 0;
                RenderingDiagnostics.ForceFullRedraw = true;
                HeadlessRenderer.CapturePng(window, 1120, 760);
                check(paints > 0, "full redraw diagnostic bypasses nested control caches at scale " + scale);
                RenderingDiagnostics.ForceFullRedraw = false;
                paints = 0;
                HeadlessRenderer.CapturePng(window, 1120, 760);
                check(paints == 0, "disabling full redraw restores cached rendering at scale " + scale);
                window.Close();

                using var tabWindow = new Form { Width = 560, Height = 380 };
                var tabs = new TabControl { Dock = DockStyle.Fill };
                var inventoryPage = new TabPage { Text = "Inventory" };
                var otherPage = new TabPage { Text = "Other" };
                inventoryPage.Controls.Add(new Label { Left = 20, Top = 20, AutoSize = true, Text = "Inventory details" });
                otherPage.Controls.Add(new Label { Left = 50, Top = 50, AutoSize = true, Text = "Other details" });
                tabs.TabPages.Add(inventoryPage); tabs.TabPages.Add(otherPage);
                tabWindow.Controls.Add(tabs); tabWindow.Show();
                HeadlessRenderer.CapturePng(tabWindow, 1120, 760);
                tabs.SelectedIndex = 1;
                CompareFrames(tabWindow, output, "switched-tab" + suffix, check);
                tabs.SelectedIndex = 0;
                CompareFrames(tabWindow, output, "restored-tab" + suffix, check);
                tabWindow.Close();

                using var paintedWindow = new Form { Width = 560, Height = 380,
                    BackColor = System.Drawing.Color.FromArgb(23, 47, 71) };
                using var markerPaint = new SKPaint { Color = SKColors.Tomato };
                double markerScale = 1;
                paintedWindow.Paint += (_, args) =>
                {
                    markerScale = args.Scaling;
                    args.Canvas.DrawRect(500, 300, 20, 20, markerPaint);
                };
                paintedWindow.Controls.Add(new Label { Left = 10, Top = 10, AutoSize = true, Text = "Ambient background" });
                paintedWindow.Show();
                using (var painted = SKBitmap.Decode(HeadlessRenderer.CapturePng(paintedWindow, 1120, 760)))
                    check(painted.GetPixel((int)(510 * markerScale), (int)(310 * markerScale)) == SKColors.Tomato &&
                        painted.GetPixel(540, 360) == new SKColor(23, 47, 71),
                        "clearing the client composite preserves window painting and ambient background at scale " + scale);
                paintedWindow.Close();
            }
        }
        finally
        {
            RenderingDiagnostics.ForceFullRedraw = previousMode;
            Application.UiScale = previousScale;
        }
    }
}
