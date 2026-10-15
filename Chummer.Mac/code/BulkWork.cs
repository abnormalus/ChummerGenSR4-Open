#nullable enable
using System;
using System.Threading;
using System.Diagnostics;
using System.Threading.Tasks;

namespace Chummer;

// Sequential work remains on its caller's UI context. Cancellation is observed at
// explicit safe boundaries; no nested event pump or concurrent live-model mutation.
internal sealed class BulkWork
{
    internal CancellationToken Cancellation { get; }
    private readonly Action<int, int> report;
    private readonly int total;
    private int started;
    private long lastYield;
    internal BulkWork(CancellationToken cancellation, int total, Action<int, int> report)
    { Cancellation = cancellation; this.total = Math.Max(1, total); this.report = report; }
    internal async Task CheckpointAsync()
    {
        Cancellation.ThrowIfCancellationRequested();
        if (lastYield == 0 || Stopwatch.GetElapsedTime(lastYield).TotalMilliseconds >= 12)
        {
            await Task.Delay(16, Cancellation);
            lastYield = Stopwatch.GetTimestamp();
        }
        Cancellation.ThrowIfCancellationRequested();
    }
    internal async Task StepAsync()
    {
        await CheckpointAsync();
        report(Math.Min(++started, total), total);
        Cancellation.ThrowIfCancellationRequested();
    }
}
