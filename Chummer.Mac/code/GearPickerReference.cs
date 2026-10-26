#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Security.Cryptography;
using System.Text;
using System.Text.Json;

namespace Chummer;

// Public catalog/product data captured by the last passing legacy-form suite.
// Frozen expectations keep the retired UI's behavior independently testable.
internal static class GearPickerReference
{
    internal sealed record CatalogContract(string[] Books, Dictionary<string, string> Baseline);
    private static T Read<T>(string name)
    {
        using var stream = Assembly.GetExecutingAssembly().GetManifestResourceStream(name)!;
        return JsonSerializer.Deserialize<T>(stream, new JsonSerializerOptions { PropertyNameCaseInsensitive = true })!;
    }
    internal static CatalogContract Catalog() => Read<CatalogContract>("Chummer.GearLegacyCatalog.json");
    internal static Dictionary<string, GearPreviewResult> Previews() => Read<Dictionary<string, GearPreviewResult>>("Chummer.GearLegacyPreview.json");
    internal static string Digest(IEnumerable<string> values) => Convert.ToHexString(SHA256.HashData(
        Encoding.UTF8.GetBytes(string.Join("\n", values.OrderBy(value => value, StringComparer.Ordinal)))));
}
