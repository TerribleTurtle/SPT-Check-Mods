using System.Collections.Generic;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services.UI;

/// <summary>
/// Spectre.Console implementation of <see cref="ITableRenderer"/>.
/// Functions as a facade that delegates to specialized UI renderers.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class TableRenderer(
    IVersionTableUiRenderer versionTableRenderer,
    IReconciliationUiRenderer reconciliationRenderer,
    IMisplacedModUiRenderer misplacedModRenderer,
    IDependencyUiRenderer dependencyRenderer
) : ITableRenderer
{
    /// <inheritdoc />
    public void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion)
    {
        versionTableRenderer.VersionCompatibilityResults(mods, sptVersion);
    }

    /// <inheritdoc />
    public void LoadingWarnings(List<Mod> modsWithWarnings)
    {
        reconciliationRenderer.LoadingWarnings(modsWithWarnings);
    }

    /// <inheritdoc />
    public void ReconciliationResults(ModReconciliationResult result)
    {
        reconciliationRenderer.ReconciliationResults(result);
    }

    /// <inheritdoc />
    public void MisplacedMods(MisplacedModReport report)
    {
        misplacedModRenderer.MisplacedMods(report);
    }

    /// <inheritdoc />
    public void UnverifiedMods(List<Mod> mods)
    {
        reconciliationRenderer.UnverifiedMods(mods);
    }

    /// <inheritdoc />
    public void DependencyResults(DependencyAnalysisResult result)
    {
        dependencyRenderer.DependencyResults(result);
    }

    /// <inheritdoc />
    public void VersionTable(List<Mod> mods)
    {
        versionTableRenderer.VersionTable(mods);
    }

    /// <inheritdoc />
    public void IgnoredUpdatesList(IReadOnlyList<IgnoredUpdate> ignores)
    {
        AnsiConsole.WriteLine();

        if (ignores.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No ignored updates found.[/]");
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
    }

    /// <inheritdoc />
    public void InstalledModsList(IReadOnlyList<Mod> serverMods, IReadOnlyList<Mod> clientMods)
    {
        AnsiConsole.WriteLine();

        if (clientMods.Count == 0 && serverMods.Count == 0)
        {
            AnsiConsole.MarkupLine("[yellow]No mods found.[/]");
            return;
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
        AnsiConsole.MarkupLine(
            $"Total: [bold]{clientMods.Count + serverMods.Count}[/] mods ([green]{serverMods.Count} server[/], [blue]{clientMods.Count} client[/])"
        );
    }

    /// <inheritdoc />
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
