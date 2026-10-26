#nullable enable
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using System.Xml;

namespace Chummer;

internal sealed record CyberwarePreviewCase(string DefinitionXml, CyberwarePreviewRequest Request,
    CyberwarePreviewResult Result, int SensitiveSystemCount, int EssenceDecimals);

// Frozen before delegation: these cases remain independent of the active picker.
internal static class CyberwarePreviewChecks
{
    private static readonly Lazy<Dictionary<string, CyberwarePreviewCase>> FrozenPreviewCases = new(() =>
    {
        using var stream = typeof(CyberwarePreviewChecks).Assembly.GetManifestResourceStream("Chummer.CyberwareLegacyPreview.json")!;
        return JsonSerializer.Deserialize<Dictionary<string, CyberwarePreviewCase>>(stream)!;
    });
    internal static void Run(Character original, Action<bool, string> check)
    {
        check(FrozenPreviewCases.Value.Count == 101, "Cyberware independent preview oracle retains all101captured cases");
        foreach (var pair in FrozenPreviewCases.Value)
        {
            var reference = pair.Value;
            var character = new Character(); character.LoadSnapshot(original.CaptureSnapshot());
            character.Options.EssenceDecimals = reference.EssenceDecimals;
            foreach (var improvement in character.Improvements.Where(item => item.ImproveType == Improvement.ImprovementType.SensitiveSystem).ToArray())
                character.Improvements.Remove(improvement);
            for (int index = 0; index < reference.SensitiveSystemCount; index++)
                character.Improvements.Add(new Improvement { ImproveType = Improvement.ImprovementType.SensitiveSystem, Enabled = true });
            byte[] before = character.CaptureSnapshot();
            var definition = new XmlDocument(); definition.LoadXml(reference.DefinitionXml);
            var catalog = XmlManager.Instance.Load(reference.Request.Bioware ? "bioware.xml" : "cyberware.xml");
            var result = CyberwarePreview.Calculate(character, catalog, definition.DocumentElement!, reference.Request);
            check(result == reference.Result && character.CaptureSnapshot().SequenceEqual(before),
                "Cyberware calculator preserves frozen legacy preview and complete snapshot: " + pair.Key);
        }
    }
}
