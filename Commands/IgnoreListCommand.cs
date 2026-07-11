using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

public sealed class IgnoreListCommand : AsyncCommand<GlobalSettings>
{
    private readonly IIgnoredUpdateStore _store;
    private readonly IModCheckReporter _reporter;

    public IgnoreListCommand(IIgnoredUpdateStore store, IModCheckReporter reporter)
    {
        _store = store;
        _reporter = reporter;
    }

    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        GlobalSettings settings,
        CancellationToken cancellationToken
    )
    {
        var ignores = await _store.LoadAsync(cancellationToken);
        _reporter.IgnoredUpdatesList(ignores);
        return 0;
    }
}
