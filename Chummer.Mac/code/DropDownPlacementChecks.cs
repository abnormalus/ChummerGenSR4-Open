#nullable enable
using System;
using System.Drawing;
using Majorsilence.Forms;

namespace Chummer;

internal static class DropDownPlacementChecks
{
    internal static void Run(Action<bool, string> check)
    {
        foreach (int scale in new[] { 1, 2 })
        {
            var work = new Rectangle(-1200 * scale, 30 * scale, 1200 * scale, 730 * scale);
            Rectangle Place(int y, int height, int width = 250) => DropDownPlacement.Calculate(
                new Rectangle(-300 * scale, y * scale, 200 * scale, 30 * scale), new Size(width * scale, height * scale), work);
            check(Place(100, 200).Top == 130 * scale, "dropdown opens below when it fits at scale " + scale);
            var bottom = Place(700, 200);
            check(bottom.Bottom == 700 * scale && work.Contains(bottom), "dropdown flips above reserved Dock area at scale " + scale);
            var tall = Place(400, 2000, 2000);
            check(work.Contains(tall) && tall.Height < 2000 * scale && tall.Width == work.Width,
                "oversized dropdown stays scrollable inside negative-origin work area at scale " + scale);
        }
    }
}
