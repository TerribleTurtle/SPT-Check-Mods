using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

public sealed class IgnoreListCommand : AsyncCommand<ListCommandSettings>
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
        ListCommandSettings settings,
        CancellationToken cancellationToken
    )
    {
        var ignores = await _store.LoadAsync(cancellationToken);

        var options = new ListFilterOptions
        {
            Type = settings.Type,
            Status = settings.Status,
            Sort = settings.Sort,
            Limit = settings.Limit,
            Search = settings.Search,
        };

        _reporter.IgnoredUpdatesList(ignores, options);
        return 0;
    }
}
