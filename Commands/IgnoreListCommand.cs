using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to list all currently ignored updates.
/// </summary>
public sealed class IgnoreListCommand : AsyncCommand<ListCommandSettings>
{
    private readonly IIgnoredUpdateStore _store;
    private readonly IModCheckReporter _reporter;

    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreListCommand"/> class.
    /// </summary>
    /// <param name="store">The ignored update store.</param>
    /// <param name="reporter">The reporter.</param>
    public IgnoreListCommand(IIgnoredUpdateStore store, IModCheckReporter reporter)
    {
        _store = store;
        _reporter = reporter;
    }

    /// <summary>
    /// Executes the ignore list command asynchronously.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings for listing, filtering, and sorting.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous execution operation. The task result contains the exit code.</returns>
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
