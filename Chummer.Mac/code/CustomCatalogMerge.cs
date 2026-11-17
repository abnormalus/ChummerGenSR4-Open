#nullable enable
using System.Collections.Generic;
using System.Xml;

namespace Chummer;

// Operates only on caller-owned XML. File lookup, caching, localization and UI
// remain outside this merge stage. Both documents are mutable scratch data.
internal static class CustomCatalogMerge
{
    internal static void Apply(XmlDocument target, XmlDocument custom)
    {
        foreach (XmlNode group in custom.SelectNodes("/chummer/*")!)
        {
            // Match against definitions already in the target, preserving the
            // legacy file/group order and canonical-name comparison semantics.
            var duplicates = new List<XmlNode>();
            foreach (XmlNode child in group.ChildNodes)
            {
                if (child["name"] != null && target.SelectNodes("/chummer/" + child.ParentNode!.Name + "/" +
                    child.Name + "[name = \"" + child["name"]!.InnerText + "\"]")!.Count > 0)
                    duplicates.Add(child);
            }
            // Remove after enumeration. Repeated names within this same group
            // retain the legacy behavior; only existing target entries win.
            foreach (XmlNode duplicate in duplicates) group.RemoveChild(duplicate);
            target.DocumentElement!.AppendChild(target.ImportNode(group, true));
        }
    }
}
