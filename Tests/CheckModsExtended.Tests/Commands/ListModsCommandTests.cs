using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Tests.Fakes;
using Spectre.Console.Cli;
using Xunit;

namespace CheckModsExtended.Tests.Commands;

public class ListModsCommandTests
{
    [Fact]
    public async Task ExecuteAsync_WhenPathValid_ExecutesCorrectly()
    {
        var initService = new FakeInitializationService { ValidatedSptPathToReturn = "C:\\SPT" };
        var scannerService = new FakeModScannerService();
        var reporter = new FakeModCheckReporter();
        var command = new ListModsCommand(initService, scannerService, reporter);
        
        var settings = new ListModsCommand.Settings();
        var method = typeof(ListModsCommand).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var result = (Task<int>)method!.Invoke(command, new object[] { null!, settings, CancellationToken.None })!;
        var exitCode = await result;

        Assert.Equal(0, exitCode);
    }
    
    [Fact]
    public async Task ExecuteAsync_WhenPathInvalid_ReturnsError()
    {
        var initService = new FakeInitializationService { ValidatedSptPathToReturn = null };
        var scannerService = new FakeModScannerService();
        var reporter = new FakeModCheckReporter();
        var command = new ListModsCommand(initService, scannerService, reporter);
        
        var settings = new ListModsCommand.Settings { SptPath = "invalid" };
        var method = typeof(ListModsCommand).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var result = (Task<int>)method!.Invoke(command, new object[] { null!, settings, CancellationToken.None })!;
        var exitCode = await result;

        Assert.Equal(1, exitCode);
    }
}
