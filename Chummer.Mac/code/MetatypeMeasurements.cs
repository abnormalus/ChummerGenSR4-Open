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
            int descriptionTop = descriptionLabel.Top;
            var fields = page.Controls.Cast<Control>().ToArray();
            foreach (var control in fields)
            {
                if (control.Top >= descriptionTop) control.Top += 64;
                else if (control.Top >= 30) control.Top += 22;
            }
            var height = fields.Single(control => control.Name == "txtHeight");
            var weight = fields.Single(control => control.Name == "txtWeight");
            var age = fields.Single(control => control.Name == "txtAge");
            Label Hint(Control field, string name, int rows)
            {
                var label = new Label { Name = name, AutoSize = false, Height = rows * 16,
                    Font = new Majorsilence.Forms.Drawing.Font(field.Font.FamilyName, 8),
                    Left = field.Left, Top = field.Bottom + 2, Width = field.Width };
                void Position() { label.Left = field.Left; label.Top = field.Bottom + 2; label.Width = field.Width; }
                field.LocationChanged += (_, _) => Position();
                field.SizeChanged += (_, _) => Position();
                page.Controls.Add(label);
                return label;
            }
            var heightHint = Hint(height, "heightGuidance", 2);
            var weightHint = Hint(weight, "weightGuidance", 1);
            var ageHint = Hint(age, "ageGuidance", 1);
            void Update()
            {
                bool known = ranges.TryGetValue(character.Metatype ?? "", out var range);
                heightHint.Text = known ? $"{range.MinHeight}–{range.MaxHeight} cm\n{Feet(range.MinHeight)}–{Feet(range.MaxHeight)}" : "";
                weightHint.Text = known ? $"{range.MinWeight}–{range.MaxWeight} kg" : "";
                string years = character.Metatype == "Ork" ? "15–25" : character.Metatype == "Troll" ? "18–30" : "20–40";
                ageHint.Text = known ? string.Format(LanguageManager.Instance.GetString("Label_TypicalAgeRange"), years) : "";
            }
            metatypeLabel.TextChanged += (_, _) => Update();
            Update();
        }
    }
}
