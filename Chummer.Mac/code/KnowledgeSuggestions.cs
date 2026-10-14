using System;
using System.IO;
using System.Linq;
using System.Xml.Linq;

namespace Chummer
{
    internal static class KnowledgeSuggestions
    {
        private static readonly Lazy<XDocument> Data = new Lazy<XDocument>(() =>
            XDocument.Load(Path.Combine(AppContext.BaseDirectory, "data", "knowledge-suggestions.xml")));
        internal static string[] Names => Data.Value.Root.Elements("skill")
            .Select(item => (string)item.Attribute("name")).ToArray();
        internal static string Category(string name) => Data.Value.Root.Elements("skill")
            .FirstOrDefault(item => string.Equals((string)item.Attribute("name"), name,
                StringComparison.OrdinalIgnoreCase))?.Attribute("category")?.Value;
    }
}
