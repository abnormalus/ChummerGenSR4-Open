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

    // Category labels are supplied by the caller so localized catalog structure
    // remains independent of this literal matching policy.
    internal static bool MatchesNameOrCategory(XmlNode definition, string text, string? categoryLabel) =>
        MatchesDisplayName(definition, text) ||
        (definition["category"]?.InnerText ?? "").IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0 ||
        (categoryLabel ?? "").IndexOf(text, StringComparison.CurrentCultureIgnoreCase) >= 0;

    internal static string GearQuery(string text) => System.Text.RegularExpressions.Regex.Replace(text,
        @"\b(?:comlinks?|commlinks)\b", "commlink", System.Text.RegularExpressions.RegexOptions.IgnoreCase |
        System.Text.RegularExpressions.RegexOptions.CultureInvariant);
}
