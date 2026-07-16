using System;


using System.Threading;
using System.Threading.Tasks;

using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to create a diagnostic archive of the logs.
/// </summary>
public sealed class DiagCommand : AsyncCommand<GlobalSettings>
{
    private readonly IDiagnosticService _diagnosticService;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagCommand"/> class.
    /// </summary>
    public DiagCommand(IDiagnosticService diagnosticService)
    {
        _diagnosticService = diagnosticService;
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
        try
        {
            var zipPath = await _diagnosticService.ExportLogsAsync(cancellationToken);
            if (zipPath == null)
            {
                AnsiConsole.MarkupLine("[yellow]Logs directory does not exist, nothing to export.[/]");
                return 0;
            }

            AnsiConsole.MarkupLine($"[green]Successfully exported logs to {zipPath}[/]");
            return 0;
        }
        catch (Exception ex) when (ex is System.IO.IOException or System.UnauthorizedAccessException)
        {
            AnsiConsole.MarkupLine($"[red]Failed to export logs: {ex.Message}[/]");
            return 1;
        }
    }
}
