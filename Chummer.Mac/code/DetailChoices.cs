using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Chummer
{
    internal sealed class DetailChoice
    {
        internal string Name, Book;
        internal bool Review;
        public override string ToString() => Name + (string.IsNullOrEmpty(Book) ? "" : " — " + Book)
            + (Review ? " (review required)" : "");
    }

    internal static class DetailChoices
    {
        // Add another quality's <choices quality="…"> group to this data file to reuse the picker.
        // null means use the ordinary free-text prompt; an empty array means all sources are disabled.
        internal static DetailChoice[] ForQuality(Character character, string qualityName)
        {
            if (character == null || string.IsNullOrEmpty(qualityName)) return null;
            var group = XDocument.Load(Path.Combine(AppContext.BaseDirectory, "data", "quality-detail-choices.xml"))
                .Root.Elements("choices").FirstOrDefault(item => (string)item.Attribute("quality") == qualityName);
            if (group == null) return null;
            return group.Elements("choice")
                .Where(item => string.IsNullOrEmpty((string)item.Attribute("source")) ||
                    character.Options.BookEnabled((string)item.Attribute("source")))
                .Select(item => new DetailChoice { Name = (string)item.Attribute("name"),
                    Book = (string)item.Attribute("book"), Review = (bool?)item.Attribute("review") ?? false })
                .OrderBy(item => item.Name).ToArray();
        }
    }
}
