using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Services.Pipeline.Steps;

public class CacheResultsStepTests
{
    [Fact]
    public async Task ExecuteAsync_SavesScanCacheRecord()
    {
        // Arrange
        var cacheService = new FakeScanCacheService();
        var step = new CacheResultsStep(cacheService);

        var context = new UpdateWorkflowContext
        {
            Args = [],
            Mods = new List<Mod>(),
            SptVersion = null,
            SptPath = "C:\\SPT"
        };

        // Act
        await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.NotNull(cacheService.SavedRecord);
        Assert.Empty(cacheService.SavedRecord.Response.Mods);
        Assert.Equal("C:\\SPT", cacheService.SavedRecord.SptPath);
    }

    [Fact]
    public async Task ExecuteAsync_WhenContextCancelled_DoesNotSave()
    {
        // Arrange
        var cacheService = new FakeScanCacheService();
        var step = new CacheResultsStep(cacheService);

        var context = new UpdateWorkflowContext
        {
            Args = [],
            IsCancelled = true
        };

        // Act
        await step.ExecuteAsync(context, CancellationToken.None);

        // Assert
        Assert.Null(cacheService.SavedRecord);
    }
}
