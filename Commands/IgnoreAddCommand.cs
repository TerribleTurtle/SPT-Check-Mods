using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to manually add a mod to the ignored updates list.
/// </summary>
public sealed class IgnoreAddCommand : AsyncCommand<IgnoreAddCommand.Settings>
{
    private readonly IIgnoredUpdateStore _store;
    private readonly IModCheckReporter _reporter;

    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<ApiModId>")]
        [Description("The Forge API ID of the mod.")]
        public int ApiModId { get; set; }

        [CommandArgument(1, "<LocalVersion>")]
        [Description("Your locally installed version.")]
        public string LocalVersion { get; set; } = string.Empty;

        [CommandArgument(2, "<LatestVersion>")]
        [Description("The remote version to ignore.")]
        public string LatestVersion { get; set; } = string.Empty;
    }

    public IgnoreAddCommand(IIgnoredUpdateStore store, IModCheckReporter reporter)
    {
        _store = store;
        _reporter = reporter;
    }

    /// <summary>
    /// Executes the ignore add command asynchronously.
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

        var newIgnore = new IgnoredUpdate(
            ApiModId: settings.ApiModId,
            LocalVersion: settings.LocalVersion,
            IgnoredLatestVersion: settings.LatestVersion,
            Source: IgnoreSource.User,
            DismissedUtc: DateTimeOffset.UtcNow
        );

        if (ignores.Any(i => string.Equals(i.Key, newIgnore.Key, StringComparison.OrdinalIgnoreCase)))
        {
            _reporter.IgnoreAddAlreadyIgnored(settings.ApiModId, settings.LocalVersion, settings.LatestVersion);
            return 0;
        }

        ignores.Add(newIgnore);
        await _store.SaveAsync(ignores, cancellationToken);

        _reporter.IgnoreAddSuccess(settings.ApiModId, settings.LocalVersion, settings.LatestVersion);
        return 0;
    }
}
