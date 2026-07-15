using System.Collections.Generic;
using System.Threading;
using CheckModsExtended.Services.UI;

namespace CheckModsExtended.Tests.Fakes;

public class FakeWebProgressTracker : IWebProgressTracker
{
    public string? LastReportedStatus { get; private set; }
    public double? LastReportedProgress { get; private set; }
    public int ReportStatusCallCount { get; private set; }
    public int ReportProgressCallCount { get; private set; }

    public void ReportStatus(string text)
    {
        LastReportedStatus = text;
        ReportStatusCallCount++;
    }

    public void ReportProgress(double progress)
    {
        LastReportedProgress = progress;
        ReportProgressCallCount++;
    }

    public async IAsyncEnumerable<ScanProgress> SubscribeAsync([System.Runtime.CompilerServices.EnumeratorCancellation] CancellationToken token)
    {
        await System.Threading.Tasks.Task.Yield();
        yield break;
    }
}
