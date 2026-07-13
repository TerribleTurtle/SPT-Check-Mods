using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Commands;
using CheckModsExtended.Tests.Fakes;
using Spectre.Console.Cli;
using Xunit;

namespace CheckModsExtended.Tests.Commands;

public class IgnoreListCommandTests
{
    [Fact]
    public async Task ExecuteAsync_ExecutesCorrectly()
    {
        var store = new FakeIgnoredUpdateStore();
        var reporter = new FakeModCheckReporter();
        var command = new IgnoreListCommand(store, reporter);
        var settings = new ListCommandSettings();
        var method = typeof(IgnoreListCommand).GetMethod("ExecuteAsync", System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.NonPublic);
        var result = (Task<int>)method!.Invoke(command, new object[] { null!, settings, CancellationToken.None })!;
        var exitCode = await result;
        Assert.Equal(0, exitCode);
    }
}
