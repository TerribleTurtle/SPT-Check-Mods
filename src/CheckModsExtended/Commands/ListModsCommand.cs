using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to list locally installed mods without checking for updates.
/// </summary>
public sealed class ListModsCommand : AsyncCommand<ListModsCommand.Settings>
{
    private readonly IInitializationService _initializationService;
    private readonly IModScannerService _scannerService;
    private readonly IModCheckReporter _reporter;

    /// <summary>
    /// Settings for the list command.
    /// </summary>
    public sealed class Settings : ListCommandSettings
    {
        [CommandArgument(0, "[SptPath]")]
        [Description("The path to your SPT installation directory. Defaults to the current directory.")]
        public string? SptPath { get; set; }
    }

    /// <summary>
    /// Initializes a new instance of the <see cref="ListModsCommand"/> class.
    /// </summary>
    /// <param name="initializationService">The initialization service.</param>
    /// <param name="scannerService">The mod scanner service.</param>
    /// <param name="reporter">The mod check reporter.</param>
    public ListModsCommand(
        IInitializationService initializationService,
        IModScannerService scannerService,
        IModCheckReporter reporter
    )
    {
        _initializationService = initializationService;
        _scannerService = scannerService;
        _reporter = reporter;
    }

    /// <summary>
    /// Executes the list mods command asynchronously.
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
        var args = string.IsNullOrWhiteSpace(settings.SptPath)
            ? System.Array.Empty<string>()
            : new[] { settings.SptPath };

        var sptPath = _initializationService.GetValidatedSptPath(args);
        if (sptPath is null)
        {
            return 1;
        }

        var (serverMods, clientMods) = await _scannerService.ScanAllModsAsync(sptPath, cancellationToken);

        var options = new ListFilterOptions
        {
            Type = settings.Type,
            Status = settings.Status,
            Sort = settings.Sort,
            Limit = settings.Limit,
            Search = settings.Search,
        };

        _reporter.InstalledModsList(serverMods, clientMods, options);

        return 0;
    }
}
