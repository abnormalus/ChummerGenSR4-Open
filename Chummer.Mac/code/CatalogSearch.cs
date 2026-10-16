#nullable enable
using System;
using System.Xml;

namespace Chummer;

// Match the same localized name shown in catalog lists, without interpreting typed
// quotes or operators. Shared by native and compatibility pickers during migration.
internal static class CatalogSearch
{
    internal static bool MatchesDisplayName(XmlNode definition, string text) =>
        (definition["translate"]?.InnerText ?? definition["name"]?.InnerText ?? "")
            .IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0;
}
