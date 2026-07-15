using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Microsoft.Extensions.Options;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class MaybeFetchRemoteIgnoresStepTests
{
    private sealed class FakeOptionsSnapshot<T>(T value) : IOptionsSnapshot<T> where T : class
    {
        public T Value => value;
        public T Get(string? name) => value;
    }

    [Fact]
    public async Task ExecuteAsync_WhenNotConfigured_ReturnsEarly()
    {
        var client = new FakeRemoteIgnoreFileClient { IsConfigured = false };
        var store = new FakeIgnoredUpdateStore();
        var reporter = new FakeModCheckReporter();
        var logger = new FakeLogger<MaybeFetchRemoteIgnoresStep>();
        var options = new FakeOptionsSnapshot<IgnoredUpdateOptions>(new IgnoredUpdateOptions());
        var settingsService = new FakeSettingsService();
        var step = new MaybeFetchRemoteIgnoresStep(client, store, reporter, logger, options, settingsService);

        var context = new UpdateWorkflowContext { Args = [] };

        await step.ExecuteAsync(context, CancellationToken.None);
        Assert.NotNull(context);
    }
}
