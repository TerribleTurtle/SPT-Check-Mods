using System.Collections.Generic;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Channels;

namespace CheckModsExtended.Services.UI;

public class WebProgressTracker : IWebProgressTracker
{
    private string _currentText = string.Empty;
    private double? _currentProgress = 0;
    private readonly object _lock = new object();
    
    private readonly List<Channel<ScanProgress>> _subscribers = new();

    public void ReportStatus(string text)
    {
        ScanProgress state;
        lock (_lock)
        {
            _currentText = text;
            // Deliberately do NOT reset _currentProgress to 0 so we don't cause UI flashes
            state = new ScanProgress(_currentText, _currentProgress);
        }
        Broadcast(state);
    }

    public void ReportProgress(double progress)
    {
        ScanProgress state;
        lock (_lock)
        {
            _currentProgress = progress;
            state = new ScanProgress(_currentText, _currentProgress);
        }
        Broadcast(state);
    }

    private void Broadcast(ScanProgress state)
    {
        lock (_lock)
        {
            foreach (var channel in _subscribers)
            {
                channel.Writer.TryWrite(state);
            }
        }
    }

    public async IAsyncEnumerable<ScanProgress> SubscribeAsync([EnumeratorCancellation] CancellationToken token)
    {
        var channel = Channel.CreateUnbounded<ScanProgress>(new UnboundedChannelOptions
        {
            SingleReader = true,
            SingleWriter = false
        });

        lock (_lock)
        {
            _subscribers.Add(channel);
            // Send the current state immediately upon subscription
            channel.Writer.TryWrite(new ScanProgress(_currentText, _currentProgress));
        }

        try
        {
            await foreach (var item in channel.Reader.ReadAllAsync(token))
            {
                yield return item;
            }
        }
        finally
        {
            lock (_lock)
            {
                _subscribers.Remove(channel);
            }
        }
    }
}

