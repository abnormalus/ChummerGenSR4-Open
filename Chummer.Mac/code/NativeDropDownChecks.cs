#nullable enable
using System;
using System.Drawing;
using System.Reflection;
using System.Threading.Tasks;
using Majorsilence.Forms;

namespace Chummer;

internal static class NativeDropDownChecks
{
    internal static async Task RunAsync(Action<bool, string> check)
    {
        using var owner = new Form { Text = "Dropdown placement checks", Width = 360, Height = 180, StartPosition = FormStartPosition.Manual };
        var combo = new ComboBox { Left = 12, Top = 100, Width = 240, Height = 28, DropDownStyle = ComboBoxStyle.DropDownList, MaxDropDownItems = 8 };
        for (int i = 0; i < 50; i++) combo.Items.Add("Choice " + i);
        combo.SelectedIndex = 0; owner.Controls.Add(combo); owner.Show(); await Task.Delay(50);
        var area = Screen.FromControl(owner)!.WorkingArea;
        double scale = owner.DesktopScaling;
        owner.Location = new Point(area.Right - (int)(owner.Width * scale) - 10, area.Bottom - (int)(owner.Height * scale) - 10);
        await Task.Delay(100);
        try
        {
            combo.DroppedDown = true; await Task.Delay(50);
            var popup = (PopupWindow)typeof(ComboBox).GetField("popup", BindingFlags.NonPublic | BindingFlags.Instance)!.GetValue(combo)!;
            Rectangle Bounds() => new(popup.Location, new Size((int)Math.Ceiling(popup.Size.Width * scale), (int)Math.Ceiling(popup.Size.Height * scale)));
            var anchor = combo.PointToScreen(Point.Empty);
            check(area.Contains(Bounds()) && Bounds().Bottom <= anchor.Y,
                "Native dropdown flips upward clear of the Dock or taskbar");
            for (int i = 0; i < 49; i++)
                typeof(ComboBox).GetMethod("OnKeyDown", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(combo, new object[] { new KeyEventArgs(Keys.Down) });
            check(combo.SelectedIndex == 49, "Native constrained dropdown retains keyboard access to its final entry");
            typeof(ComboBox).GetMethod("OnKeyUp", BindingFlags.NonPublic | BindingFlags.Instance)!.Invoke(combo, new object[] { new KeyEventArgs(Keys.Escape) });
            check(!combo.DroppedDown, "Native upward dropdown dismisses through Escape");
            combo.DropDownHeight = area.Height * 3; combo.DropDownWidth = area.Width * 3;
            combo.DroppedDown = true; await Task.Delay(50);
            check(area.Contains(Bounds()) && popup.Size.Height < combo.DropDownHeight,
                "Native oversized dropdown is constrained to the working area");
        }
        finally { combo.DroppedDown = false; owner.Close(); }
    }
}
