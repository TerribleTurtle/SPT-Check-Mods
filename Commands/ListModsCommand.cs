using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

/// <summary>
/// Command to list locally installed mods without checking for updates.
/// </summary>
public sealed class ListModsCommand : AsyncCommand<ListModsCommand.Settings>
{
    private readonly IInitializationService _initializationService;
    private readonly IModScannerService _scannerService;

    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "[SptPath]")]
        [Description("The path to your SPT installation directory. Defaults to the current directory.")]
        public string? SptPath { get; set; }
    }

    public ListModsCommand(
        IInitializationService initializationService,
        IModScannerService scannerService)
    {
        _initializationService = initializationService;
        _scannerService = scannerService;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
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

        AnsiConsole.WriteLine();

        if (clientMods.Count == 0 && serverMods.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No mods found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Installed Mods[/]")
            .AddColumn("Name")
            .AddColumn("Author")
            .AddColumn("Version")
            .AddColumn("Type");

        foreach (var mod in serverMods)
        {
            table.AddRow(
                mod.DisplayName.EscapeMarkup(),
                mod.DisplayAuthor.EscapeMarkup(),
                mod.Local.LocalVersion.EscapeMarkup(),
                "[green]Server[/]"
            );
        }

        foreach (var mod in clientMods)
        {
            table.AddRow(
                mod.DisplayName.EscapeMarkup(),
                (mod.DisplayAuthor ?? "Unknown").EscapeMarkup(),
                mod.Local.LocalVersion.EscapeMarkup(),
                "[blue]Client[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Total: [bold]{clientMods.Count + serverMods.Count}[/] mods ([green]{serverMods.Count} server[/], [blue]{clientMods.Count} client[/])");

        return 0;
    }
}
