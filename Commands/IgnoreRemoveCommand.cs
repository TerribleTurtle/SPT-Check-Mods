using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to remove a mod from the ignored updates list.
/// </summary>
public sealed class IgnoreRemoveCommand : AsyncCommand<IgnoreRemoveCommand.Settings>
{
    private readonly IIgnoredUpdateStore _store;
    private readonly IModCheckReporter _reporter;

    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<ApiModId>")]
        [Description("The Forge API ID of the mod to remove from the ignore list.")]
        public int ApiModId { get; set; }
    }

    public IgnoreRemoveCommand(IIgnoredUpdateStore store, IModCheckReporter reporter)
    {
        _store = store;
        _reporter = reporter;
    }

    /// <summary>
    /// Executes the ignore remove command asynchronously.
    /// </summary>
    /// <param name="context">The command context.</param>
    /// <param name="settings">The command settings.</param>
    /// <param name="cancellationToken">The cancellation token.</param>
    /// <returns>A task that represents the asynchronous execution operation. The task result contains the exit code.</returns>
    protected override async Task<int> ExecuteAsync(
        CommandContext context,
        Settings settings,
        CancellationToken cancellationToken
    )
    {
        var ignores = (await _store.LoadAsync(cancellationToken)).ToList();

        var removedCount = ignores.RemoveAll(i => i.ApiModId == settings.ApiModId);

        if (removedCount == 0)
        {
            _reporter.IgnoreRemoveNotFound(settings.ApiModId);
            return 0;
        }

        await _store.SaveAsync(ignores, cancellationToken);

        _reporter.IgnoreRemoveSuccess(removedCount, settings.ApiModId);
        return 0;
    }
}
