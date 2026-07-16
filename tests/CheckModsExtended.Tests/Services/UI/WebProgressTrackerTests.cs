using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.UI;
using Xunit;

namespace CheckModsExtended.Tests.Services.UI;

public class WebProgressTrackerTests
{
    [Fact]
    public async Task SubscribeAsync_ReceivesInitialState()
    {
        // Arrange
        var tracker = new WebProgressTracker();
        tracker.ReportStatus("Initial Status");
        tracker.ReportProgress(50.0);

        using var cts = new CancellationTokenSource();

        // Act
        var iterator = tracker.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        var moved = await iterator.MoveNextAsync();

        // Assert
        Assert.True(moved);
        Assert.Equal("Initial Status", iterator.Current.Text);
        Assert.Equal(50.0, iterator.Current.Progress);

        cts.Cancel();
    }

    [Fact]
    public async Task SubscribeAsync_ReceivesUpdates()
    {
        // Arrange
        var tracker = new WebProgressTracker();
        using var cts = new CancellationTokenSource();

        // Act & Assert
        var iterator = tracker.SubscribeAsync(cts.Token).GetAsyncEnumerator(cts.Token);
        
        // Initial state
        await iterator.MoveNextAsync();
        Assert.Equal(string.Empty, iterator.Current.Text);
        Assert.Equal(0, iterator.Current.Progress);

        // Update status
        tracker.ReportStatus("New Status");
        await iterator.MoveNextAsync();
        Assert.Equal("New Status", iterator.Current.Text);
        Assert.Equal(0, iterator.Current.Progress);

        // Update progress
        tracker.ReportProgress(75.0);
        await iterator.MoveNextAsync();
        Assert.Equal("New Status", iterator.Current.Text);
        Assert.Equal(75.0, iterator.Current.Progress);

        cts.Cancel();
    }
}

