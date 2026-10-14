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
        public override string ToString() => Name + (Review ? " (review required)" : "");
    }

    internal static class DetailChoices
    {
        private static readonly Lazy<XDocument> Data = new Lazy<XDocument>(() =>
            XDocument.Load(Path.Combine(AppContext.BaseDirectory, "data", "quality-detail-choices.xml")));
        private static string ChoiceGroup(string name) => name == "SINner (Standard)" || name == "SINner (Criminal)" ? "Records on File" : name;
        internal static bool RequiresReview(string qualityName, string detail) =>
            Data.Value.Root.Elements("choices").Where(group => (string)group.Attribute("quality") == ChoiceGroup(qualityName))
                .Elements("choice").Any(item => string.Equals((string)item.Attribute("name"), detail,
                    StringComparison.OrdinalIgnoreCase) && ((bool?)item.Attribute("review") ?? false));
        // Add another quality's <choices quality="…"> group to this data file to reuse the picker.
        // null means use the ordinary free-text prompt; an empty array means all sources are disabled.
        internal static DetailChoice[] ForQuality(Character character, string qualityName)
        {
            if (character == null || string.IsNullOrEmpty(qualityName)) return null;
            bool allergy = qualityName.StartsWith("Allergy (", StringComparison.Ordinal);
            var group = Data.Value.Root.Elements("choices").FirstOrDefault(item =>
                (string)item.Attribute("quality") == (allergy ? "Allergy" : ChoiceGroup(qualityName)));
            if (group == null) return null;
            return group.Elements("choice")
                .Where(item => !allergy || qualityName.StartsWith("Allergy (" + (string)item.Attribute("rarity") + ",", StringComparison.Ordinal))
                .Where(item => string.IsNullOrEmpty((string)item.Attribute("source")) ||
                    character.Options.BookEnabled((string)item.Attribute("source")))
                .Select(item => new DetailChoice { Name = (string)item.Attribute("name"),
                    Book = (string)item.Attribute("book"), Review = (bool?)item.Attribute("review") ?? false })
                .OrderBy(item => item.Name).ToArray();
        }
    }
}
