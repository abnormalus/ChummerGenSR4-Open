#nullable enable
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using System.Xml;

namespace Chummer;

internal static class BatchOutputChecks
{
    internal static void Run(Character sample, string output, Action<bool, string> check)
    {
        // Headless tests have no native UI loop; test the sequential service on a worker.
        // Production captures the native UI synchronization context instead.
        Task.Run(async () => {
            var progress = new List<int>();
            var document = await BatchCharacterOutput.BuildAsync(new[] { "one", "two" }, CancellationToken.None,
                (done, total) => { if (total != 2) throw new Exception("Incorrect batch size."); progress.Add(done); }, _ => sample);
            check(document.SelectNodes("/characters/character")!.Count == 2 && progress.SequenceEqual(new[] { 0, 1, 2 }),
                "batch output preserves character order and reports completed units");
            foreach (int cancelAt in new[] { 0, 1, 2 })
            {
                using var cancellation = new CancellationTokenSource();
                int loads = 0;
                bool cancelled = false;
                try
                {
                    await BatchCharacterOutput.BuildAsync(new[] { "one", "two" }, cancellation.Token,
                        (done, _) => { if (done == cancelAt) cancellation.Cancel(); }, _ => { loads++; return sample; });
                }
                catch (OperationCanceledException) { cancelled = true; }
                check(cancelled && loads == cancelAt,
                    "batch cancellation at unit " + cancelAt + " discards output without loading further characters");
            }
            bool failed = false;
            int loaded = 0;
            try
            {
                await BatchCharacterOutput.BuildAsync(new[] { "one", "broken", "three" }, CancellationToken.None,
                    (_, _) => {}, _ => ++loaded == 2 ? throw new InvalidDataException("Test fixture") : sample);
            }
            catch (InvalidDataException) { failed = true; }
            check(failed && loaded == 2, "batch failure stops before subsequent characters and returns no partial document");
        }).GetAwaiter().GetResult();

        string path = Path.Combine(output, "batch-read-only.chum");
        string originalName = sample.FileName;
        try { sample.FileName = path; sample.Save(); }
        finally { sample.FileName = originalName; }
        var legacy = new XmlDocument(); legacy.Load(path);
        var qualities = legacy.SelectSingleNode("/character/qualities")!;
        qualities.RemoveAll();
        var oldQuality = legacy.CreateElement("quality");
        oldQuality.InnerText = "Low-Light Vision [5]";
        qualities.AppendChild(oldQuality);
        legacy.SelectSingleNode("/character/movement")!.InnerText = "";
        legacy.Save(path);
        byte[] original = File.ReadAllBytes(path);
        File.Delete(path + ".bak");
        var readOnly = new Character { FileName = path };
        bool loadedReadOnly = readOnly.Load(false);
        _ = readOnly.Movement;
        check(loadedReadOnly && File.ReadAllBytes(path).SequenceEqual(original) && !File.Exists(path + ".bak"),
            "read-only quality conversion and lazy movement calculation never overwrite the source file");
    }
}
