#nullable enable
using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using Avalonia;
using Avalonia.Media.Imaging;
using Avalonia.Styling;
using Forms = Majorsilence.Forms;
namespace Chummer;
internal static class NativeCalendarStartChecks
{
    internal static async Task RunAsync(Forms.Form owner, Character original, string output, Action<bool, string> check)
    {
        using (var prompt = new frmSelectCalendarStart(new CalendarWeek(2072, 52)))
        {
            var pending = prompt.ShowDialogAsync(owner); var window = (CalendarStartWindow)NativeDialogHost.ActiveWindow!;
            check(window.YearInput.Value == 2072 && window.MonthInput.Value == 12 && window.WeekInput.Value == 5,
                "Native calendar loads the existing year final fifth week");
            window.MonthInput.Value = 1;
            check(window.WeekInput.Maximum == 4 && window.WeekInput.Value == 4, "Native calendar clamps fifth week when changing to a four-week month");
            window.YearInput.Text = "10000"; await Task.Delay(30);
            check(window.YearInput.Value == 9000, "Native calendar preserves the historical year limit");
            window.YearInput.Value = 2072; window.MonthInput.Value = 12; window.WeekInput.Value = 5;
            var nativeOwner = Forms.AvaloniaHostInterop.ToAvaloniaWindow(owner); var previousTheme = nativeOwner.RequestedThemeVariant;
            try
            {
                foreach (var theme in new[] { ThemeVariant.Dark, ThemeVariant.Light })
                {
                    nativeOwner.RequestedThemeVariant = theme; await Task.Delay(100);
                    check(window.ActualThemeVariant == theme && window.YearInput.Bounds.Width >= 110,
                        "Native calendar follows " + (theme == ThemeVariant.Dark ? "dark" : "light") + " owner theme");
                    using var bitmap = new RenderTargetBitmap(new PixelSize((int)window.ClientSize.Width * 2, (int)window.ClientSize.Height * 2), new Vector(192, 192));
                    bitmap.Render(window); bitmap.Save(Path.Combine(output, "native-calendar-start-" + (theme == ThemeVariant.Dark ? "dark" : "light") + ".png"), PngBitmapEncoderOptions.Default);
                }
            }
            finally { nativeOwner.RequestedThemeVariant = previousTheme; }
            window.Accept();
            check(await pending == Forms.DialogResult.OK && prompt.SelectedYear == 2072 && prompt.SelectedWeek == 52,
                "Native calendar returns accepted legacy ordinal week");
        }
        var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot()); character.Created = true; character.Calendar.Clear();
        using var editor = new frmCareer(character) { Loading = true, MdiParent = owner }; editor.Show();
        const BindingFlags flags = BindingFlags.NonPublic | BindingFlags.Instance;
        void Invoke(string name) => editor.GetType().GetMethod(name, flags)!.Invoke(editor, new object[] { editor, EventArgs.Empty });
        void Choose(string command, bool accept, int year = 2072, int month = 12, int week = 5)
        {
            Avalonia.Threading.Dispatcher.UIThread.Post(() => {
                var window = (CalendarStartWindow)NativeDialogHost.ActiveWindow!;
                window.YearInput.Value = year; window.MonthInput.Value = month; window.WeekInput.Value = week;
                if (accept) window.Accept(); else window.Close();
            });
            Invoke(command);
        }
        try
        {
            Choose("cmdAddWeek_Click", false);
            check(character.Calendar.Count == 0, "Native calendar initial cancellation creates no entries");
            Choose("cmdAddWeek_Click", true);
            check(character.Calendar.Count == 1 && character.Calendar[0].Year == 2072 && character.Calendar[0].Week == 52,
                "Native calendar initial acceptance creates the selected final week");
            var first = character.Calendar[0]; first.Notes = "Synthetic calendar note"; string identity = first.InternalId;
            Invoke("cmdAddWeek_Click");
            check(character.Calendar.Count == 2 && character.Calendar[1].Year == 2073 && character.Calendar[1].Week == 1,
                "Native calendar next week preserves the existing year rollover");
            Choose("cmdChangeStartWeek_Click", false, 2073, 1, 1);
            check(first.Year == 2072 && first.Week == 52 && character.Calendar[1].Year == 2073 && character.Calendar[1].Week == 1 && first.InternalId == identity,
                "Native calendar shift cancellation preserves all dates and identity");
            Choose("cmdChangeStartWeek_Click", true, 2073, 1, 1);
            check(first.Year == 2073 && first.Week == 1 && character.Calendar[1].Year == 2073 && character.Calendar[1].Week == 2 && first.InternalId == identity && first.Notes == "Synthetic calendar note",
                "Native calendar accepted shift preserves spacing identity and notes across year boundary");
        }
        finally { editor.GetType().GetField("_blnIsDirty", flags)!.SetValue(editor, false); editor.Close(); }
    }
}
