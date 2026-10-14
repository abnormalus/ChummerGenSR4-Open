using System;
using System.Collections.Generic;
using Majorsilence.Forms.Drawing;
using System.IO;
using System.Linq;
using System.Xml.Linq;
using Majorsilence.Forms;

namespace Chummer
{
    internal sealed class AppearanceColor
    {
        internal string Name, Family;
        internal bool IsFamily => Name == Family;
        public override string ToString() => Name;
    }

    internal static class AppearanceColors
    {
        // Family order follows the supplied palette; shades sort within each family.
        private static readonly AppearanceColor[] palette = LoadPalette("appearance-colors.xml");
        private static readonly AppearanceColor[] skinPalette = LoadPalette("skin-colors.xml");
        private static AppearanceColor[] LoadPalette(string file) => XDocument.Load(Path.Combine(AppContext.BaseDirectory,
            "data", file)).Root.Elements("family").SelectMany(family =>
            new[] { new AppearanceColor { Name = (string)family.Attribute("name"), Family = (string)family.Attribute("name") } }
            .Concat(family.Elements("color").OrderBy(color => (string)color.Attribute("name"), StringComparer.OrdinalIgnoreCase)
                .Select(color => new AppearanceColor { Name = (string)color.Attribute("name"), Family = (string)family.Attribute("name") }))).ToArray();

        internal static IEnumerable<object> Filter(string query, bool skin = false)
        {
            foreach (var group in (skin ? skinPalette : palette).GroupBy(color => color.Family))
            {
                var matches = group.Where(color => color.Name.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0 ||
                    color.Family.IndexOf(query, StringComparison.OrdinalIgnoreCase) >= 0).ToArray();
                if (matches.Length == 0) continue;
                yield return group.First();
                foreach (var color in matches.Where(color => !color.IsFamily)) yield return color;
            }
        }

        internal static SearchableChoices Attach(ComboBox combo, bool skin = false)
        {
            combo.DropDownStyle = ComboBoxStyle.DropDown;
            combo.Sorted = false;
            combo.DrawMode = DrawMode.OwnerDrawFixed;
            combo.DropDownWidth = 260;
            combo.MaxDropDownItems = 12;
            combo.DrawItem += (_, e) =>
            {
                e.DrawBackground();
                if (e.Index < 0 || e.Index >= combo.Items.Count) return;
                var color = (AppearanceColor)combo.Items[e.Index];
                using var font = new Font(e.Font.FamilyName, (float)(e.Font.SizeInPoints * 96 / 72 * combo.Scaling),
                    color.IsFamily ? FontStyle.Bold : FontStyle.Regular, GraphicsUnit.Pixel);
                using var brush = new SolidBrush(e.ForeColor);
                using var format = new StringFormat { LineAlignment = StringAlignment.Center };
                var bounds = e.Bounds;
                int padding = (int)((color.IsFamily ? 3 : 20) * combo.Scaling);
                bounds.X += padding;
                bounds.Width -= padding;
                e.Graphics.DrawString(color.Name, font, brush, bounds, format);
                e.DrawFocusRectangle();
            };
            var behavior = SearchableChoices.AttachFiltered(combo, query => Filter(query, skin));
            behavior.Refresh();
            return behavior;
        }
    }
}
