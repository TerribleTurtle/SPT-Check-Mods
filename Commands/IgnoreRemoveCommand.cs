using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

public sealed class IgnoreRemoveCommand : AsyncCommand<IgnoreRemoveCommand.Settings>
{
    private readonly IIgnoredUpdateStore _store;

    public sealed class Settings : GlobalSettings
    {
        [CommandArgument(0, "<ApiModId>")]
        [Description("The Forge API ID of the mod to remove from the ignore list.")]
        public int ApiModId { get; set; }
    }

    public IgnoreRemoveCommand(IIgnoredUpdateStore store)
    {
        _store = store;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
    {
        var ignores = (await _store.LoadAsync(cancellationToken)).ToList();
        
        var removedCount = ignores.RemoveAll(i => i.ApiModId == settings.ApiModId);

        if (removedCount == 0)
        {
            AnsiConsole.MarkupLine($"[yellow]No ignored updates found for API Mod ID {settings.ApiModId}.[/]");
            return 0;
        }

        await _store.SaveAsync(ignores, cancellationToken);
        
        AnsiConsole.MarkupLine($"[green]Successfully removed {removedCount} ignored update(s) for API Mod ID {settings.ApiModId}.[/]");
        return 0;
    }
}
