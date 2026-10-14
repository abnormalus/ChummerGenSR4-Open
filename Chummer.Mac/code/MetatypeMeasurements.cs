using System;
using System.Collections.Generic;
using System.Drawing;
using System.Linq;
using Majorsilence.Forms;

namespace Chummer
{
    internal static class MetatypeMeasurements
    {
        private static readonly Dictionary<string, (int MinHeight, int MaxHeight, int MinWeight, int MaxWeight)> ranges =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["Dwarf"] = (100, 145, 35, 85),
                ["Elf"] = (160, 215, 48, 120),
                ["Human"] = (145, 205, 43, 145),
                ["Ork"] = (165, 225, 78, 210),
                ["Troll"] = (210, 290, 180, 480)
            };

        private static string Feet(int cm)
        {
            int inches = (int)Math.Round(cm / 2.54);
            return $"{inches / 12}′{inches % 12}″";
        }

        internal static string Guidance(string metatype)
        {
            if (!ranges.TryGetValue(metatype ?? "", out var range))
                return LanguageManager.Instance.GetString("Label_MeasurementRangesUnavailable");
            return string.Format(LanguageManager.Instance.GetString("Label_MeasurementRanges"),
                range.MinHeight, range.MaxHeight, Feet(range.MinHeight), Feet(range.MaxHeight),
                range.MinWeight, range.MaxWeight);
        }

        internal static void Attach(Character character, TabPage page, Label metatypeLabel, Label descriptionLabel)
        {
            // Reserve a guidance row below the editable measurements. Existing responsive
            // description layout uses the shifted label position to size its text areas.
            int top = descriptionLabel.Top;
            foreach (Control control in page.Controls.Cast<Control>().ToArray())
                if (control.Top >= top) control.Top += 40;
            var guidance = new Label { Name = "measurementGuidance", AutoSize = false,
                Left = descriptionLabel.Left, Top = top, Height = 36,
                Width = Math.Max(200, page.ClientSize.Width - descriptionLabel.Left - 8),
                Anchor = AnchorStyles.Top | AnchorStyles.Left | AnchorStyles.Right };
            void Update() => guidance.Text = Guidance(character.Metatype);
            metatypeLabel.TextChanged += (_, _) => Update();
            page.Controls.Add(guidance);
            Update();
        }
    }
}
