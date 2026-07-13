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
    private readonly IIgnoreService _ignoreService;
    private readonly IModCheckReporter _reporter;

    /// <summary>
    /// Settings for the ignore add command.
    /// </summary>
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

    /// <summary>
    /// Initializes a new instance of the <see cref="IgnoreAddCommand"/> class.
    /// </summary>
    /// <param name="ignoreService">The ignore service.</param>
    /// <param name="reporter">The reporter.</param>
    public IgnoreAddCommand(IIgnoreService ignoreService, IModCheckReporter reporter)
    {
        _ignoreService = ignoreService;
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
        var added = await _ignoreService.AddIgnoreAsync(settings.ApiModId, settings.LocalVersion, settings.LatestVersion, cancellationToken);

        if (!added)
        {
            _reporter.IgnoreAddAlreadyIgnored(settings.ApiModId, settings.LocalVersion, settings.LatestVersion);
            return 0;
        }

        _reporter.IgnoreAddSuccess(settings.ApiModId, settings.LocalVersion, settings.LatestVersion);
        return 0;
    }
}
