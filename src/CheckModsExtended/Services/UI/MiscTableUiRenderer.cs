using System;
using System.Collections.Generic;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services.UI;

[Injectable(InjectionType.Singleton)]
public sealed class MiscTableUiRenderer : IMiscTableUiRenderer
{
    public void IgnoredUpdatesList(IReadOnlyList<IgnoredUpdate> ignores, ListFilterOptions? options = null)
    {
        AnsiConsole.WriteLine();

        if (ignores.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No ignored updates found.[/]");
            AnsiConsole.MarkupLine(
                "[grey]You can ignore mod updates by running:[/] check-mods ignore add <ApiModId> <LocalVersion>"
            );
            AnsiConsole.WriteLine();
            return;
        }

        var filtered = ignores.AsEnumerable();

        if (!string.IsNullOrWhiteSpace(options?.Search))
        {
            var search = options.Search;
            filtered = filtered.Where(i =>
                (i.Name?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
                || i.ApiModId.ToString().Contains(search, StringComparison.OrdinalIgnoreCase)
            );
        }

        if (!string.IsNullOrWhiteSpace(options?.Status))
        {
            var status = options.Status;
            filtered = filtered.Where(i => i.Source.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(options?.Sort))
        {
            filtered = options.Sort.ToLowerInvariant() switch
            {
                "name" => filtered.OrderBy(i => i.Name ?? string.Empty),
                "id" or "apimodid" => filtered.OrderBy(i => i.ApiModId),
                "localversion" or "version" => filtered.OrderBy(i => i.LocalVersion),
                "ignoredversion" => filtered.OrderBy(i => i.IgnoredLatestVersion),
                "source" => filtered.OrderBy(i => i.Source.ToString()),
                _ => filtered.OrderBy(i => i.ApiModId),
            };
        }
        else
        {
            filtered = filtered.OrderBy(i => i.ApiModId);
        }

        if (options?.Limit > 0)
        {
            filtered = filtered.Take(options.Limit.Value);
        }

        var results = filtered.ToList();

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No ignored updates match the given filters.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Ignored Updates[/]")
            .AddColumn("API Mod ID")
            .AddColumn("Name")
            .AddColumn("Local Version")
            .AddColumn("Ignored Version")
            .AddColumn("Source");

        foreach (var ignore in results)
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
        AnsiConsole.MarkupLine($"Total: [bold]{results.Count}[/] ignored update(s).");
    }

    public void InstalledModsList(
        IReadOnlyList<Mod> serverMods,
        IReadOnlyList<Mod> clientMods,
        ListFilterOptions? options = null
    )
    {
        AnsiConsole.WriteLine();

        if (clientMods.Count == 0 && serverMods.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No mods found.[/]");
            AnsiConsole.MarkupLine("[grey]Server mods should be located in:[/] SPT/user/mods");
            AnsiConsole.MarkupLine("[grey]Client mods should be located in:[/] BepInEx/plugins");
            AnsiConsole.WriteLine();
            return;
        }

        var allMods = serverMods
            .Select(m => (Mod: m, Type: "Server"))
            .Concat(clientMods.Select(m => (Mod: m, Type: "Client")));

        if (!string.IsNullOrWhiteSpace(options?.Type))
        {
            var t = options.Type;
            allMods = allMods.Where(x => x.Type.Equals(t, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(options?.Status))
        {
            var status = options.Status;
            allMods = allMods.Where(x => x.Mod.Status.ToString().Equals(status, StringComparison.OrdinalIgnoreCase));
        }

        if (!string.IsNullOrWhiteSpace(options?.Search))
        {
            var search = options.Search;
            allMods = allMods.Where(x =>
                x.Mod.DisplayName.Contains(search, StringComparison.OrdinalIgnoreCase)
                || (x.Mod.DisplayAuthor?.Contains(search, StringComparison.OrdinalIgnoreCase) ?? false)
            );
        }

        if (!string.IsNullOrWhiteSpace(options?.Sort))
        {
            allMods = options.Sort.ToLowerInvariant() switch
            {
                "name" => allMods.OrderBy(x => x.Mod.DisplayName),
                "author" => allMods.OrderBy(x => x.Mod.DisplayAuthor ?? string.Empty),
                "version" => allMods.OrderBy(x => x.Mod.Local.LocalVersion),
                "type" => allMods.OrderBy(x => x.Type).ThenBy(x => x.Mod.DisplayName),
                _ => allMods.OrderBy(x => x.Type).ThenBy(x => x.Mod.DisplayName),
            };
        }
        else
        {
            allMods = allMods.OrderBy(x => x.Type).ThenBy(x => x.Mod.DisplayName);
        }

        if (options?.Limit > 0)
        {
            allMods = allMods.Take(options.Limit.Value);
        }

        var results = allMods.ToList();

        if (results.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No mods match the given filters.[/]");
            return;
        }

        var table = new Table()
            .Border(TableBorder.Rounded)
            .Title("[bold blue]Installed Mods[/]")
            .AddColumn("Name")
            .AddColumn("Author")
            .AddColumn("Version")
            .AddColumn("Type");

        foreach (var item in results)
        {
            var typeColor = item.Type == "Server" ? "green" : "blue";

            table.AddRow(
                item.Mod.DisplayName.EscapeMarkup(),
                (item.Mod.DisplayAuthor ?? "Unknown").EscapeMarkup(),
                item.Mod.Local.LocalVersion.EscapeMarkup(),
                $"[{typeColor}]{item.Type}[/]"
            );
        }

        AnsiConsole.Write(table);
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            $"Total: [bold]{results.Count}[/] mods ([green]{results.Count(x => x.Type == "Server")} server[/], [blue]{results.Count(x => x.Type == "Client")} client[/])"
        );
    }

    public void PendingConfirmationsSummary(IReadOnlyList<PendingConfirmation> pendingConfirmations)
    {
        AnsiConsole.MarkupLine($"\n[yellow]Found {pendingConfirmations.Count} match(es) that need confirmation...[/]");

        var table = new Table();
        table.AddColumn("Local Server Mod");
        table.AddColumn("Author");
        table.AddColumn("API Match");
        table.AddColumn("API Author");
        table.AddColumn("Confidence");

        foreach (var pending in pendingConfirmations)
        {
            table.AddRow(
                pending.OriginalMod.Local.LocalName.EscapeMarkup(),
                pending.OriginalMod.Local.LocalAuthor?.EscapeMarkup() ?? "Unknown",
                pending.ApiMatch.Name.EscapeMarkup(),
                pending.ApiMatch.Owner?.Name.EscapeMarkup() ?? "N/A",
                $"{pending.ConfidenceScore}%"
            );
        }
        AnsiConsole.Write(table);
    }
}
