using System;

using System.Threading;
using System.Threading.Tasks;

using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to clean the application data directory.
/// </summary>
public sealed class CleanCommand : AsyncCommand<GlobalSettings>
{
    private readonly IMaintenanceService _maintenanceService;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanCommand"/> class.
    /// </summary>
    public CleanCommand(IMaintenanceService maintenanceService)
    {
        _maintenanceService = maintenanceService;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(
        CommandContext context,
        GlobalSettings settings,
        CancellationToken cancellationToken
    )
    {
        return ExecuteInternalAsync(context, settings, cancellationToken);
    }

    internal async Task<int> ExecuteInternalAsync(
        CommandContext context,
        GlobalSettings settings,
        CancellationToken cancellationToken
    )
    {
        var cleaned = await _maintenanceService.CleanAppDataAsync(cancellationToken);
        if (cleaned)
        {
            AnsiConsole.MarkupLine("[green]Successfully cleared app data directory.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]App data directory does not exist, nothing to clean.[/]");
        }

        return 0;
    }
}
