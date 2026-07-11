using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

public sealed class IgnoreListCommand : AsyncCommand<GlobalSettings>
{
    private readonly IIgnoredUpdateStore _store;

    public IgnoreListCommand(IIgnoredUpdateStore store)
    {
        _store = store;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, GlobalSettings settings, CancellationToken cancellationToken)
    {
        var ignores = await _store.LoadAsync(cancellationToken);

        AnsiConsole.WriteLine();

        if (ignores.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No ignored updates found.[/]");
            return 0;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Ignored Updates[/]")
            .AddColumn("API Mod ID")
            .AddColumn("Name")
            .AddColumn("Local Version")
            .AddColumn("Ignored Version")
            .AddColumn("Source");

        foreach (var ignore in ignores.OrderBy(i => i.ApiModId))
        {
            table.AddRow(
                ignore.ApiModId.ToString(),
                (ignore.Name ?? "Unknown").EscapeMarkup(),
                ignore.LocalVersion.EscapeMarkup(),
                ignore.IgnoredLatestVersion.EscapeMarkup(),
                ignore.Source.ToString()
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine($"Total: [bold]{ignores.Count}[/] ignored update(s).");

        return 0;
    }
}
