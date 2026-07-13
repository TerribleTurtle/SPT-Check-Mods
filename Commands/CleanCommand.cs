using System;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Configuration;
using Microsoft.Extensions.Options;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to clean the application data directory.
/// </summary>
public sealed class CleanCommand : AsyncCommand<GlobalSettings>
{
    private readonly AppPaths _appPaths;

    /// <summary>
    /// Initializes a new instance of the <see cref="CleanCommand"/> class.
    /// </summary>
    /// <param name="appPaths">The application paths.</param>
    public CleanCommand(IOptions<AppPaths> appPaths)
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
        if (Directory.Exists(_appPaths.AppDataDirectory))
        {
            Directory.Delete(_appPaths.AppDataDirectory, true);
            AnsiConsole.MarkupLine("[green]Successfully cleared app data directory.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine("[yellow]App data directory does not exist, nothing to clean.[/]");
        }

        return Task.FromResult(0);
    }
}
