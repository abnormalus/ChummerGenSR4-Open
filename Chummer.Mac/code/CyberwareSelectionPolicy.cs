#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Xml;

namespace Chummer;

internal sealed record CyberwareSelectionContext(bool AllowModularPlugins = false, bool PluginSelection = false,
    bool SubsystemsOnly = false, string Subsystems = "");

// Shared catalog boundary for compatibility and standard pickers. Typed search never
// becomes XPath, and every presentation path retains the same source/context limits.
internal static class CyberwareSelectionPolicy
{
    internal static XmlNode? FindDefinition(XmlDocument catalog, bool bioware, string name, string category)
    {
        if (string.IsNullOrEmpty(name) || string.IsNullOrEmpty(category)) return null;
        string kind = bioware ? "bioware" : "cyberware";
        return catalog.SelectNodes("/chummer/" + kind + "s/" + kind)!.Cast<XmlNode>()
            .FirstOrDefault(definition => definition["name"]?.InnerText == name && definition["category"]?.InnerText == category);
    }
    internal static bool AllowsEntry(XmlNode definition, CyberwareSelectionContext context) =>
        definition["source"] is XmlNode source && source.InnerText.Length > 0 &&
        (!context.SubsystemsOnly || (definition["capacity"]?.InnerText.Contains('[') ?? false));
    internal static IReadOnlyList<CatalogCategory> Categories(XmlDocument catalog, CyberwareSelectionContext context)
    {
        var subsystems = (context.Subsystems ?? "").Split(',').Select(value => value.Trim()).ToHashSet(StringComparer.Ordinal);
        return catalog.SelectNodes("/chummer/categories/category")!.Cast<XmlNode>()
            .Where(category => category.Attributes?["show"] == null || context.AllowModularPlugins)
            .Where(category => !context.PluginSelection || !context.SubsystemsOnly || subsystems.Contains(category.InnerText))
            .Select(category => new CatalogCategory(category.InnerText, category.Attributes?["translate"]?.InnerText ?? category.InnerText)).ToArray();
    }

    internal static IEnumerable<XmlNode> Entries(Character character, XmlDocument catalog, bool bioware,
        CyberwareSelectionContext context, string? category = null, string search = "")
    {
        var allowed = Categories(catalog, context).Select(value => value.Code).ToHashSet(StringComparer.Ordinal);
        string node = bioware ? "bioware" : "cyberware";
        return catalog.SelectNodes("/chummer/" + node + "s/" + node)!.Cast<XmlNode>()
            .Where(item => item["source"] is XmlNode source && source.InnerText.Length > 0 && character.Options.Books.Contains(source.InnerText))
            .Where(item => allowed.Contains(item["category"]?.InnerText ?? ""))
            .Where(item => AllowsEntry(item, context))
            .Where(item => search.Length > 0 ? CatalogSearch.MatchesDisplayName(item, search)
                : category == null || item["category"]?.InnerText == category);
    }
}
