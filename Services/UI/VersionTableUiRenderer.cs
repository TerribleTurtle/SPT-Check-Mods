using System.Collections.Generic;
using System.Linq;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services.UI;

/// <inheritdoc />
[Injectable(InjectionType.Singleton)]
public sealed class VersionTableUiRenderer(ITextRenderer textRenderer) : IVersionTableUiRenderer
{
    /// <inheritdoc />
    public void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion)
    {
        var incompatibleMods = mods.Where(m => m.Update.IsLocalSptIncompatible).ToList();

        if (incompatibleMods.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All mod versions are compatible![/]");
            AnsiConsole.WriteLine();
            textRenderer.Rule();
            return;
        }

        AnsiConsole.MarkupLine($"[yellow]Found {incompatibleMods.Count} incompatible mod(s).[/]");
        AnsiConsole.WriteLine();
        var tree = new Tree("[yellow]Incompatible mods[/]");

        foreach (var mod in incompatibleMods)
        {
            var nameDisplay = UiFormattingUtility.FormatModLink(mod.DisplayName, mod.Api.ApiUrl);

            var modNode = tree.AddNode(nameDisplay);
            modNode.AddNode($"[yellow]{mod.Update.IncompatibilityReason?.EscapeMarkup()}[/]");

            if (string.IsNullOrWhiteSpace(mod.Update.CompatibleVersionString))
            {
                modNode.AddNode($"[red]No compatible version available for SPT {sptVersion}[/]");
                continue;
            }

            modNode.AddNode($"[grey]Latest compatible version:[/] [green]{mod.Update.CompatibleVersionString.EscapeMarkup()}[/]");

            if (mod.Api.ApiModId.HasValue && !string.IsNullOrWhiteSpace(mod.Api.ApiSlug))
            {
                var forgeDownloadUrl = ForgeUrls.Download(mod.Api.ApiModId.Value, mod.Api.ApiSlug, mod.Update.CompatibleVersionString);
                modNode.AddNode($"[grey]Download:[/] [link]{forgeDownloadUrl.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
        textRenderer.Rule();
    }

    /// <inheritdoc />
    public void VersionTable(List<Mod> mods)
    {
        var verifiedMods = GetDeduplicatedVerifiedMods(mods);

        if (verifiedMods.Count == 0)
        {
            return;
        }

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[bold blue]Checking for mod updates...[/]");
        AnsiConsole.MarkupLine("[white]This tool depends on mod authors to use and update valid version numbers. If you notice a version number in the Current Version column that is incorrect, please contact the author of the mod to have it updated. Additionally, these updates can be ignored by selecting the \"Manage ignored updates\" option at the end of the check.[/]");
        AnsiConsole.WriteLine();

        var table = new Table()
            .Title("[blue]Mod Version Summary[/]")
            .BorderColor(Color.Grey)
            .AddColumn("[white]Name[/]")
            .AddColumn("[white]Author[/]")
            .AddColumn("[white]Current Version[/]")
            .AddColumn("[white]Latest Version[/]");

        foreach (var mod in verifiedMods)
        {
            var (displayName, displayAuthor) = UiFormattingUtility.FormatModDisplayStrings(mod.DisplayName, mod.DisplayAuthor);
            var latestVersionDisplay = FormatVersionDisplay(mod);
            var nameDisplay = UiFormattingUtility.FormatModLink(displayName, mod.Api.ApiUrl);

            table.AddRow(
                nameDisplay,
                displayAuthor.EscapeMarkup(),
                mod.Local.LocalVersion.EscapeMarkup(),
                latestVersionDisplay
            );
        }

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine("[grey]Version colors: [green]Up to date[/] | [red]Update available[/] | [darkorange]Update blocked[/] | [blue]Newer than latest[/] | [grey]Ignored[/][/]");

        var modsWithUpdates = verifiedMods.Where(m => m.Update.UpdateStatus == UpdateStatus.UpdateAvailable && !m.Update.UpdateSuppressed).ToList();
        if (modsWithUpdates.Count > 0)
        {
            AnsiConsole.WriteLine();

            var updatesTree = new Tree("[red]Updates available[/]");

            foreach (var mod in modsWithUpdates)
            {
                var nameDisplay = UiFormattingUtility.FormatModLink(mod.DisplayName, mod.Api.ApiUrl);

                var modNode = updatesTree.AddNode(nameDisplay);
                modNode.AddNode($"[grey]{mod.Local.LocalVersion.EscapeMarkup()}[/] [yellow]->[/] [green]{mod.Update.LatestVersion!.EscapeMarkup()}[/]");

                if (!string.IsNullOrWhiteSpace(mod.Update.DownloadLink))
                {
                    modNode.AddNode($"[grey]Download:[/] [link]{mod.Update.DownloadLink.EscapeMarkup()}[/]");
                }

                if (mod.Update.UpdateDependencyChanges?.HasChanges == true)
                {
                    AddUpdateDependencyChangeNodes(modNode, mod.Update.UpdateDependencyChanges);
                }
            }

            AnsiConsole.Write(updatesTree);
        }

        var modsWithBlockedUpdates = verifiedMods.Where(m => m.Update.UpdateStatus == UpdateStatus.UpdateBlocked).ToList();
        if (modsWithBlockedUpdates.Count > 0)
        {
            AnsiConsole.WriteLine();

            var blockedTree = new Tree("[darkorange]Updates blocked[/]");

            foreach (var mod in modsWithBlockedUpdates)
            {
                var nameDisplay = UiFormattingUtility.FormatModLink(mod.DisplayName, mod.Api.ApiUrl);

                var modNode = blockedTree.AddNode(nameDisplay);
                modNode.AddNode($"[grey]{mod.Local.LocalVersion.EscapeMarkup()}[/] [yellow]->[/] [darkorange]{mod.Update.LatestVersion!.EscapeMarkup()}[/]");

                if (!string.IsNullOrWhiteSpace(mod.Update.BlockReason))
                {
                    modNode.AddNode($"[grey]Reason:[/] {FormatBlockReason(mod.Update.BlockReason).EscapeMarkup()}");
                }

                if (mod.Update.BlockingMods is { Count: > 0 })
                {
                    foreach (var blocker in mod.Update.BlockingMods)
                    {
                        modNode.AddNode($"[grey]Blocked by:[/] {blocker.Name.EscapeMarkup()} [grey]({blocker.Constraint.EscapeMarkup()})[/]");
                    }
                }

                if (mod.Update.UpdateDependencyChanges?.HasChanges == true)
                {
                    AddUpdateDependencyChangeNodes(modNode, mod.Update.UpdateDependencyChanges);
                }
            }

            AnsiConsole.Write(blockedTree);
        }

        AnsiConsole.WriteLine();
        textRenderer.Rule();
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new FigletText("FIN").LeftJustified().Color(Color.Fuchsia));
        AnsiConsole.MarkupLine("[fuchsia]Scroll up to read details about your mods![/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Pro tip:    Mod names are clickable.[/]");
        AnsiConsole.MarkupLine("[grey]Expert tip: Read the mod page before installing or updating mods.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[white]Find an issue [italic]with this tool[/]? Submit a bug report on the [link=https://github.com/TerribleTurtle/SPT-Check-Mods/issues/new]TerribleTurtle fork[/].[/]");
        AnsiConsole.WriteLine();
    }

    internal static string FormatVersionDisplay(Mod mod)
    {
        var latestVersion = mod.Update.LatestVersion!;

        if (mod.Update.UpdateSuppressed)
        {
            return $"[grey]{latestVersion.EscapeMarkup()} (ignored)[/]";
        }

        return mod.Update.UpdateStatus switch
        {
            UpdateStatus.UpToDate => $"[green]{latestVersion.EscapeMarkup()}[/]",
            UpdateStatus.UpdateAvailable => $"[red]{latestVersion.EscapeMarkup()}[/]",
            UpdateStatus.UpdateBlocked => $"[darkorange]{latestVersion.EscapeMarkup()}[/]",
            UpdateStatus.NewerInstalled => $"[blue]{latestVersion.EscapeMarkup()}[/]",
            _ => latestVersion.EscapeMarkup(),
        };
    }

    private static string FormatBlockReason(string reason)
    {
        return reason switch
        {
            "dependency_constraint_violation" => "A dependency has a version constraint that prevents this update",
            "chain_dependency_conflict" => "A dependency chain conflict prevents this update",
            _ => reason.Replace('_', ' '),
        };
    }

    private static void AddUpdateDependencyChangeNodes(TreeNode modNode, UpdateDependencyDelta delta)
    {
        var changesNode = modNode.AddNode("[grey]Dependency changes:[/]");

        foreach (var dep in delta.Added)
        {
            var url = dep.ModId > 0 && !string.IsNullOrWhiteSpace(dep.Slug) ? ForgeUrls.ModPage(dep.ModId, dep.Slug) : null;
            var nameDisplay = UiFormattingUtility.IsLinkUrlSafe(url)
                ? $"[white link={url}]{dep.Name.EscapeMarkup()}[/]"
                : $"[white]{dep.Name.EscapeMarkup()}[/]";

            var annotation = dep.InstallState switch
            {
                DependencyInstallState.NotInstalled => $"[red]new - download v{dep.RecommendedVersion.EscapeMarkup()}[/]",
                DependencyInstallState.InstalledOutdated => $"[yellow]installed v{(dep.InstalledVersion ?? "?").EscapeMarkup()}, update needs v{dep.RecommendedVersion.EscapeMarkup()}[/]",
                _ => $"[grey]already satisfied (v{(dep.InstalledVersion ?? dep.RecommendedVersion).EscapeMarkup()})[/]",
            };

            var depNode = changesNode.AddNode($"[green]+[/] {nameDisplay} {annotation}");

            if (dep.Conflict)
            {
                depNode.AddNode("[red]Version constraint conflict reported by Forge.[/]");
            }

            if (dep.InstallState == DependencyInstallState.NotInstalled && !string.IsNullOrWhiteSpace(dep.DownloadLink))
            {
                depNode.AddNode($"[grey]Download:[/] [link]{dep.DownloadLink.EscapeMarkup()}[/]");
            }
        }

        foreach (var dep in delta.Removed)
        {
            var wasVersion = dep.InstalledVersion ?? dep.RecommendedVersion;
            changesNode.AddNode($"[grey]-[/] [grey]{dep.Name.EscapeMarkup()} no longer required (was v{wasVersion.EscapeMarkup()})[/]");
        }
    }

    private static List<Mod> GetDeduplicatedVerifiedMods(List<Mod> mods)
    {
        return mods.Where(m => m.IsMatched && m.Update.LatestVersion is not null)
            .GroupBy(m => m.Api.ApiModId!.Value)
            .Select(g => g.OrderByDescending(m => SemVer.TryParse(m.Local.LocalVersion, nameof(VersionTableUiRenderer))
                .Match(v => v, _ => new SemanticVersioning.Version(0, 0, 0))).First())
            .ToList();
    }
}
