using System.Collections.Generic;
using System.Threading;

namespace CheckModsExtended.Services.UI;

public record ScanProgress(string Text, double? Progress);

public interface IWebProgressTracker
{
    void ReportStatus(string text);
    void ReportProgress(double progress);
    IAsyncEnumerable<ScanProgress> SubscribeAsync(CancellationToken token);
}
