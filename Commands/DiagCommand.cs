using System;
using System.IO;
using System.IO.Compression;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to create a diagnostic archive of the logs.
/// </summary>
public sealed class DiagCommand : AsyncCommand<GlobalSettings>
{
    private readonly AppPaths _appPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="DiagCommand"/> class.
    /// </summary>
    /// <param name="appPaths">The application paths.</param>
    public DiagCommand(IOptions<AppPaths> appPaths)
    {
        _appPaths = appPaths.Value;
    }

    /// <inheritdoc/>
    protected override Task<int> ExecuteAsync(
        CommandContext context,
        GlobalSettings settings,
        CancellationToken cancellationToken
    )
    {
        var logsDir = Path.Combine(_appPaths.AppDataDirectory, "logs");
        if (!Directory.Exists(logsDir))
        {
            AnsiConsole.MarkupLine("[yellow]Logs directory does not exist, nothing to export.[/]");
            return Task.FromResult(0);
        }

        var zipPath = Path.Combine(
            Directory.GetCurrentDirectory(),
            $"spt-check-mods-logs-{DateTime.Now:yyyyMMdd-HHmmss}.zip"
        );
        ZipFile.CreateFromDirectory(logsDir, zipPath);
        AnsiConsole.MarkupLine($"[green]Successfully exported logs to {zipPath}[/]");
        return Task.FromResult(0);
    }
}
