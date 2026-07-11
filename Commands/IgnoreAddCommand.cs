using System;
using System.ComponentModel;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using Spectre.Console.Cli;

namespace CheckModsExtended.Commands;

public sealed class IgnoreAddCommand : AsyncCommand<IgnoreAddCommand.Settings>
{
    private readonly IIgnoredUpdateStore _store;

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

    public IgnoreAddCommand(IIgnoredUpdateStore store)
    {
        _store = store;
    }

    protected override async Task<int> ExecuteAsync(CommandContext context, Settings settings, CancellationToken cancellationToken)
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
            AnsiConsole.MarkupLine($"[yellow]Update is already ignored (ID: {settings.ApiModId}, {settings.LocalVersion} -> {settings.LatestVersion}).[/]");
            return 0;
        }

        ignores.Add(newIgnore);
        await _store.SaveAsync(ignores, cancellationToken);
        
        AnsiConsole.MarkupLine($"[green]Successfully ignored update for API Mod ID {settings.ApiModId} ({settings.LocalVersion} -> {settings.LatestVersion}).[/]");
        return 0;
    }
}
