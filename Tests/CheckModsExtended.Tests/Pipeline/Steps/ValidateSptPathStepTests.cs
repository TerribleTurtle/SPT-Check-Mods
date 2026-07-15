using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models.Pipeline;
using CheckModsExtended.Services.Pipeline.Steps;
using CheckModsExtended.Tests.Fakes;
using Xunit;

namespace CheckModsExtended.Tests.Pipeline.Steps;

public class ValidateSptPathStepTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPathIsNull_CancelsContext()
    {
        var initService = new FakeInitializationService();
        initService.ValidatedSptPathToReturn = null;
        var logger = new FakeLogger<ValidateSptPathStep>();
        var step = new ValidateSptPathStep(initService, logger);

        var context = new UpdateWorkflowContext
        {
            Args = []
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.True(context.IsCancelled);
    }

    [Fact]
    public async Task ExecuteAsync_WhenPathIsValid_SetsPath()
    {
        var initService = new FakeInitializationService();
        initService.ValidatedSptPathToReturn = "C:\\SPT";
        var logger = new FakeLogger<ValidateSptPathStep>();
        var step = new ValidateSptPathStep(initService, logger);

        var context = new UpdateWorkflowContext
        {
            Args = []
        };

        await step.ExecuteAsync(context, CancellationToken.None);

        Assert.False(context.IsCancelled);
        Assert.Equal("C:\\SPT", context.SptPath);
    }
}
