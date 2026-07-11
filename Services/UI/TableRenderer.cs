using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CheckMods.Configuration;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services.UI;

/// <summary>
/// Spectre.Console implementation of <see cref="ITableRenderer"/>.
/// </summary>
[Injectable(InjectionType.Singleton)]
public sealed class TableRenderer : ITableRenderer
{
    private readonly ITextRenderer _textRenderer;

    public TableRenderer(ITextRenderer textRenderer)
    {
        _textRenderer = textRenderer;
    }

    /// <inheritdoc />
    public void VersionCompatibilityResults(List<Mod> mods, SemanticVersioning.Version sptVersion)
    {
        var incompatibleMods = mods.Where(m => m.IsLocalSptIncompatible).ToList();

        if (incompatibleMods.Count == 0)
        {
            AnsiConsole.MarkupLine("[green]All mod versions are compatible![/]");
            AnsiConsole.WriteLine();
            _textRenderer.Rule();
            return;
        }

        AnsiConsole.MarkupLine($"[yellow]Found {incompatibleMods.Count} incompatible mod(s).[/]");
        AnsiConsole.WriteLine();
        var tree = new Tree("[yellow]Incompatible mods[/]");

        foreach (var mod in incompatibleMods)
        {
            var nameDisplay = FormatModLink(mod.DisplayName, mod.ApiUrl);

            var modNode = tree.AddNode(nameDisplay);
            modNode.AddNode($"[yellow]{mod.IncompatibilityReason?.EscapeMarkup()}[/]");

            if (string.IsNullOrWhiteSpace(mod.CompatibleVersionString))
            {
                modNode.AddNode($"[red]No compatible version available for SPT {sptVersion}[/]");
                continue;
            }

            modNode.AddNode($"[grey]Latest compatible version:[/] [green]{mod.CompatibleVersionString.EscapeMarkup()}[/]");

            if (mod.ApiModId.HasValue && !string.IsNullOrWhiteSpace(mod.ApiSlug))
            {
                var forgeDownloadUrl = ForgeUrls.Download(mod.ApiModId.Value, mod.ApiSlug, mod.CompatibleVersionString);
                modNode.AddNode($"[grey]Download:[/] [link]{forgeDownloadUrl.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
        _textRenderer.Rule();
    }

    /// <inheritdoc />
    public void LoadingWarnings(List<Mod> modsWithWarnings)
    {
        if (modsWithWarnings.Count == 0)
        {
            return;
        }

        AnsiConsole.WriteLine();

        var tree = new Tree("[yellow]Mod loading warnings[/]");

        foreach (var mod in modsWithWarnings)
        {
            var modType = mod.IsServerMod ? "Server" : "Client";
            var modName = !string.IsNullOrWhiteSpace(mod.LocalName) ? mod.LocalName : Path.GetFileName(mod.FilePath);

            var nameDisplay = FormatModLink(modName, mod.ApiUrl);

            var modNode = tree.AddNode($"[grey]{modType}:[/] {nameDisplay}");
            foreach (var warning in mod.LoadWarnings)
            {
                modNode.AddNode($"[yellow]{warning.EscapeMarkup()}[/]");
            }

            if (!string.IsNullOrWhiteSpace(mod.ApiSourceCodeUrl))
            {
                modNode.AddNode($"[grey]Please report:[/] [link]{mod.ApiSourceCodeUrl.EscapeMarkup()}[/]");
            }
            else if (!string.IsNullOrWhiteSpace(mod.ApiUrl))
            {
                modNode.AddNode($"[grey]Please report:[/] [link]{mod.ApiUrl.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(tree);
    }

    /// <inheritdoc />
    public void ReconciliationResults(ModReconciliationResult result)
    {
        var serverCount = result.ReconciledPairs.Count + result.UnmatchedServerMods.Count;
        var clientCount = result.ReconciledPairs.Count + result.UnmatchedClientMods.Count;
        AnsiConsole.MarkupLine($"[grey]Comparing {serverCount} server mods with {clientCount} client mods...[/]");

        if (result.ReconciledPairs.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No matching server/client mod pairs found.[/]");
        }
        else
        {
            AnsiConsole.MarkupLine($"[green]Matched {result.ReconciledPairs.Count} server/client mod pairs.[/]");

            var pairsWithNotes = result.ReconciledPairs.Where(p => p.Notes.Count > 0).ToList();
            if (pairsWithNotes.Count > 0)
            {
                AnsiConsole.WriteLine();

                var tree = new Tree("[yellow]Reconciliation warnings[/]");

                foreach (var pair in pairsWithNotes)
                {
                    var modName = pair.SelectedMod.LocalName;

                    var nameDisplay = FormatModLink(modName, pair.SelectedMod.ApiUrl);

                    var modNode = tree.AddNode(nameDisplay);
                    foreach (var note in pair.Notes)
                    {
                        modNode.AddNode($"[yellow]{note.EscapeMarkup()}[/]");
                    }

                    var reportUrl = !string.IsNullOrWhiteSpace(pair.SelectedMod.ApiSourceCodeUrl)
                        ? pair.SelectedMod.ApiSourceCodeUrl
                        : pair.SelectedMod.ApiUrl;

                    var guidMismatch = !string.Equals(pair.ServerMod.Guid, pair.ClientMod.Guid, StringComparison.OrdinalIgnoreCase);

                    if (guidMismatch)
                    {
                        modNode.AddNode("[grey]Matched by name, but the GUIDs differ. This is likely a mod packaged with mismatched GUIDs.[/]");

                        if (!string.IsNullOrWhiteSpace(reportUrl))
                        {
                            modNode.AddNode($"[grey]Report the issue here:[/] [link]{reportUrl.EscapeMarkup()}[/]");
                        }
                    }
                    else if (!string.IsNullOrWhiteSpace(reportUrl))
                    {
                        modNode.AddNode($"[grey]Please report:[/] [link]{reportUrl.EscapeMarkup()}[/]");
                    }
                }

                AnsiConsole.Write(tree);
                AnsiConsole.WriteLine();
            }
        }

        AnsiConsole.MarkupLine($"[grey]Final mod count: {result.Mods.Count} (matched pairs: {result.ReconciledPairs.Count}, server-only: {result.UnmatchedServerMods.Count}, client-only: {result.UnmatchedClientMods.Count})[/]");
        AnsiConsole.WriteLine();
        _textRenderer.Rule();
    }

    /// <inheritdoc />
    public void MisplacedMods(MisplacedModReport report)
    {
        AnsiConsole.WriteLine();
        _textRenderer.Rule();
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[red bold]Improperly installed mods detected.[/]");
        AnsiConsole.MarkupLine("[grey]It appears that the following mods are installed incorrectly. Review the mod pages for install instructions and ensure they are correctly installed.[/]");
        AnsiConsole.WriteLine();

        var serverInClient = report.WrongFolder.Where(m => m.IsServerMod).ToList();
        var clientInServer = report.WrongFolder.Where(m => !m.IsServerMod).ToList();

        if (serverInClient.Count > 0)
        {
            var tree = new Tree("[yellow]Server mods found in the client folder[/] [grey](BepInEx/plugins)[/][yellow]. Move them into[/] [grey]SPT/user/mods[/]");
            foreach (var mod in serverInClient)
            {
                AddMisplacedModNode(tree, mod);
            }
            AnsiConsole.Write(tree);
            AnsiConsole.WriteLine();
        }

        if (clientInServer.Count > 0)
        {
            var tree = new Tree("[yellow]Client mods found in the server folder[/] [grey](SPT/user/mods)[/][yellow]. Move them into[/] [grey]BepInEx/plugins[/]");
            foreach (var mod in clientInServer)
            {
                AddMisplacedModNode(tree, mod);
            }
            AnsiConsole.Write(tree);
            AnsiConsole.WriteLine();
        }

        foreach (var directory in report.CrossInstalled)
        {
            PrintCrossInstalledDirectory(directory);
        }

        AnsiConsole.MarkupLine("[red]These mods are being skipped for the rest of this check. Move them to the correct location and run this tool again to have them included.[/]");
        AnsiConsole.MarkupLine("[grey]If this incorrect, please create a Github issue and provide logs.[/]");
        AnsiConsole.WriteLine();
        _textRenderer.Rule();
    }

    private void PrintCrossInstalledDirectory(CrossInstalledDirectory directory)
    {
        Tree tree;
        TreeNode directoryNode;

        if (directory.Ambiguous)
        {
            tree = new Tree("[yellow]Unrelated mods share one folder under[/] [grey](BepInEx/plugins)[/][yellow]. One is likely in the wrong place. Review the install instructions for each[/]");
            directoryNode = tree.AddNode($"[grey]{directory.Directory.EscapeMarkup()}[/]");
            foreach (var mod in directory.Mods)
            {
                AddMisplacedModNode(directoryNode, mod);
            }
        }
        else
        {
            tree = new Tree("[yellow]Mods found inside another mod's folder under[/] [grey](BepInEx/plugins)[/][yellow]. Review the mod's installation instructions[/]");
            directoryNode = tree.AddNode($"[grey]{directory.Directory.EscapeMarkup()}[/]");
            foreach (var mod in directory.Misplaced)
            {
                AddMisplacedModNode(directoryNode, mod);
            }
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
    }

    private static void AddMisplacedModNode(IHasTreeNodes parent, MisplacedMod mod)
    {
        var name = !string.IsNullOrWhiteSpace(mod.Name) ? mod.Name : Path.GetFileName(mod.FilePath);
        var guidSuffix = !string.IsNullOrWhiteSpace(mod.Guid) ? $" [grey]({mod.Guid.EscapeMarkup()})[/]" : string.Empty;

        var modNode = parent.AddNode($"[white]{name.EscapeMarkup()}[/]{guidSuffix}");
        modNode.AddNode($"[grey]{mod.FilePath.EscapeMarkup()}[/]");
    }

    /// <inheritdoc />
    public void UnverifiedMods(List<Mod> mods)
    {
        var unverifiedMods = mods.Where(m => m.Status == ModStatus.NoMatch).ToList();

        if (unverifiedMods.Count == 0)
        {
            return;
        }

        AnsiConsole.WriteLine();
        var tree = new Tree("[yellow]Mods not found on Forge[/]");

        foreach (var mod in unverifiedMods)
        {
            var modDisplayName = mod.DisplayName.EscapeMarkup();
            if (!string.IsNullOrWhiteSpace(mod.DisplayAuthor))
            {
                modDisplayName += $" by {mod.DisplayAuthor.EscapeMarkup()}";
            }

            var modNode = tree.AddNode($"[white]{modDisplayName}[/]");

            if (!string.IsNullOrWhiteSpace(mod.Guid))
            {
                modNode.AddNode($"[grey]GUID: {mod.Guid.EscapeMarkup()}[/]");
            }

            if (!string.IsNullOrWhiteSpace(mod.FilePath))
            {
                modNode.AddNode($"[grey]Path: {mod.FilePath.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(tree);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]These were not matched to a Forge listing. That's expected for a mod that isn't published on the Forge, or for a mod which includes multiple plugins where only one uses the GUID linked to the Forge. No action is needed unless you expected one of these to be its own mod on Forge.[/]");
        AnsiConsole.WriteLine();
    }

    /// <inheritdoc />
    public void DependencyResults(DependencyAnalysisResult result)
    {
        if (result.RootMods.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No dependency information available.[/]");
            AnsiConsole.WriteLine();
            _textRenderer.Rule();
            return;
        }

        AnsiConsole.MarkupLine("[green]Dependency analysis complete.[/]");
        AnsiConsole.WriteLine();

        DependencyTree(result);

        if (result.Conflicts.Count > 0)
        {
            DependencyConflicts(result.Conflicts);
        }

        if (result.MissingDependencies.Count > 0)
        {
            MissingDependencies(result.MissingDependencies);
        }

        if (!result.HasIssues)
        {
            AnsiConsole.MarkupLine("[green]All dependencies are satisfied![/]");
        }

        AnsiConsole.WriteLine();
        _textRenderer.Rule();
    }

    private static void DependencyTree(DependencyAnalysisResult result)
    {
        var tree = new Tree("[bold white]Mod Dependencies[/]");

        var sortedMods = result.RootMods.OrderBy(n => n.Mod.DisplayName, StringComparer.OrdinalIgnoreCase).ToList();

        foreach (var node in sortedMods)
        {
            var label = FormatDependencyNodeLabel(node);
            var treeNode = tree.AddNode(label);

            if (node.Children.Count > 0)
            {
                AddDependencyChildrenToTree(treeNode, node.Children);
            }
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
    }

    private static void AddDependencyChildrenToTree(TreeNode parent, List<DependencyNode> children)
    {
        foreach (var child in children.OrderBy(c => c.Mod.DisplayName, StringComparer.OrdinalIgnoreCase))
        {
            var label = FormatDependencyNodeLabel(child);
            var childTreeNode = parent.AddNode(label);

            if (child.Children.Count > 0)
            {
                AddDependencyChildrenToTree(childTreeNode, child.Children);
            }
        }
    }

    private static string FormatDependencyNodeLabel(DependencyNode node)
    {
        var name = node.Mod.DisplayName.EscapeMarkup();
        var version = node.Mod.LocalVersion.EscapeMarkup();

        string statusIndicator;
        string nameColor;

        if (!node.IsInstalled)
        {
            statusIndicator = "[red](missing)[/]";
            nameColor = "red";
        }
        else if (node.DependencyInfo?.Conflict == true)
        {
            statusIndicator = "[yellow](conflict)[/]";
            nameColor = "yellow";
        }
        else
        {
            statusIndicator = "";
            nameColor = "white";
        }

        string? linkUrl = null;
        if (!string.IsNullOrWhiteSpace(node.Mod.ApiUrl))
        {
            linkUrl = node.Mod.ApiUrl;
        }
        else if (node.DependencyInfo != null && node.DependencyInfo.Id > 0 && !string.IsNullOrWhiteSpace(node.DependencyInfo.Slug))
        {
            linkUrl = ForgeUrls.ModPage(node.DependencyInfo.Id, node.DependencyInfo.Slug);
        }

        var label = $"{WithLink($"[{nameColor}]{name}[/]", linkUrl)} [grey]v{version}[/]";

        if (!string.IsNullOrWhiteSpace(statusIndicator))
        {
            label += $" {statusIndicator}";
        }

        return label;
    }

    private static void DependencyConflicts(List<DependencyConflict> conflicts)
    {
        var tree = new Tree("[yellow]Dependency conflicts[/]");

        foreach (var conflict in conflicts)
        {
            var modNode = tree.AddNode($"[white]{conflict.ModName.EscapeMarkup()}[/]");
            modNode.AddNode($"[yellow]{conflict.Description.EscapeMarkup()}[/]");

            if (conflict.DependencyInfo.Id > 0 && !string.IsNullOrWhiteSpace(conflict.DependencyInfo.Slug))
            {
                var url = ForgeUrls.ModPage(conflict.DependencyInfo.Id, conflict.DependencyInfo.Slug);
                modNode.AddNode($"[grey]View on Forge:[/] [link]{url.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
    }

    private static void MissingDependencies(List<MissingDependency> missingDeps)
    {
        var tree = new Tree("[red]Missing dependencies[/]");

        foreach (var dep in missingDeps)
        {
            var url = dep.ModId > 0 && !string.IsNullOrWhiteSpace(dep.Slug) ? ForgeUrls.ModPage(dep.ModId, dep.Slug) : null;
            var nameDisplay = IsLinkUrlSafe(url)
                ? $"[white link={url}]{dep.Name.EscapeMarkup()}[/]"
                : $"[white]{dep.Name.EscapeMarkup()}[/]";

            var depNode = tree.AddNode(nameDisplay);
            depNode.AddNode($"[grey]Recommended version:[/] [green]{dep.RecommendedVersion.EscapeMarkup()}[/]");

            if (!string.IsNullOrWhiteSpace(dep.DownloadLink))
            {
                depNode.AddNode($"[grey]Download:[/] [link]{dep.DownloadLink.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(tree);
        AnsiConsole.WriteLine();
    }

    private static void AddUpdateDependencyChangeNodes(TreeNode modNode, UpdateDependencyDelta delta)
    {
        var changesNode = modNode.AddNode("[grey]Dependency changes:[/]");

        foreach (var dep in delta.Added)
        {
            var url = dep.ModId > 0 && !string.IsNullOrWhiteSpace(dep.Slug) ? ForgeUrls.ModPage(dep.ModId, dep.Slug) : null;
            var nameDisplay = IsLinkUrlSafe(url)
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

    /// <inheritdoc />
    public void VersionTable(List<Mod> mods)
    {
        var verifiedMods = mods.Where(m => m.IsMatched && m.LatestVersion is not null)
            .GroupBy(m => m.ApiModId!.Value)
            .Select(g => g.OrderByDescending(m => SemVer.ParseOrZero(m.LocalVersion)).First())
            .ToList();

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
            var (displayName, displayAuthor) = FormatModDisplayStrings(mod.DisplayName, mod.DisplayAuthor);
            var latestVersionDisplay = FormatVersionDisplay(mod);
            var nameDisplay = FormatModLink(displayName, mod.ApiUrl);

            table.AddRow(
                nameDisplay,
                displayAuthor.EscapeMarkup(),
                mod.LocalVersion.EscapeMarkup(),
                latestVersionDisplay
            );
        }

        AnsiConsole.Write(table);

        AnsiConsole.MarkupLine("[grey]Version colors: [green]Up to date[/] | [red]Update available[/] | [darkorange]Update blocked[/] | [blue]Newer than latest[/] | [grey]Ignored[/][/]");

        var modsWithUpdates = verifiedMods.Where(m => m.UpdateStatus == UpdateStatus.UpdateAvailable && !m.UpdateSuppressed).ToList();
        if (modsWithUpdates.Count > 0)
        {
            AnsiConsole.WriteLine();

            var updatesTree = new Tree("[red]Updates available[/]");

            foreach (var mod in modsWithUpdates)
            {
                var nameDisplay = FormatModLink(mod.DisplayName, mod.ApiUrl);

                var modNode = updatesTree.AddNode(nameDisplay);
                modNode.AddNode($"[grey]{mod.LocalVersion.EscapeMarkup()}[/] [yellow]->[/] [green]{mod.LatestVersion!.EscapeMarkup()}[/]");

                if (!string.IsNullOrWhiteSpace(mod.DownloadLink))
                {
                    modNode.AddNode($"[grey]Download:[/] [link]{mod.DownloadLink.EscapeMarkup()}[/]");
                }

                if (mod.UpdateDependencyChanges?.HasChanges == true)
                {
                    AddUpdateDependencyChangeNodes(modNode, mod.UpdateDependencyChanges);
                }
            }

            AnsiConsole.Write(updatesTree);
        }

        var modsWithBlockedUpdates = verifiedMods.Where(m => m.UpdateStatus == UpdateStatus.UpdateBlocked).ToList();
        if (modsWithBlockedUpdates.Count > 0)
        {
            AnsiConsole.WriteLine();

            var blockedTree = new Tree("[darkorange]Updates blocked[/]");

            foreach (var mod in modsWithBlockedUpdates)
            {
                var nameDisplay = FormatModLink(mod.DisplayName, mod.ApiUrl);

                var modNode = blockedTree.AddNode(nameDisplay);
                modNode.AddNode($"[grey]{mod.LocalVersion.EscapeMarkup()}[/] [yellow]->[/] [darkorange]{mod.LatestVersion!.EscapeMarkup()}[/]");

                if (!string.IsNullOrWhiteSpace(mod.BlockReason))
                {
                    modNode.AddNode($"[grey]Reason:[/] {FormatBlockReason(mod.BlockReason).EscapeMarkup()}");
                }

                if (mod.BlockingMods is { Count: > 0 })
                {
                    foreach (var blocker in mod.BlockingMods)
                    {
                        modNode.AddNode($"[grey]Blocked by:[/] {blocker.Name.EscapeMarkup()} [grey]({blocker.Constraint.EscapeMarkup()})[/]");
                    }
                }
            }

            AnsiConsole.Write(blockedTree);
        }

        AnsiConsole.WriteLine();
        _textRenderer.Rule();
        AnsiConsole.WriteLine();

        AnsiConsole.Write(new FigletText("FIN").LeftJustified().Color(Color.Fuchsia));
        AnsiConsole.MarkupLine("[fuchsia]Scroll up to read details about your mods![/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[grey]Pro tip:    Mod names are clickable.[/]");
        AnsiConsole.MarkupLine("[grey]Expert tip: Read the mod page before installing or updating mods.[/]");
        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine("[white]Find an issue [italic]with this tool[/]? Find Refringe on Discord, or [link=https://github.com/refringe/SPT-Check-Mods/issues/new]submit a bug report[/].[/]");
        AnsiConsole.WriteLine();
    }

    internal static string FormatVersionDisplay(Mod mod)
    {
        var latestVersion = mod.LatestVersion!;

        if (mod.UpdateSuppressed)
        {
            return $"[grey]{latestVersion.EscapeMarkup()} (ignored)[/]";
        }

        return mod.UpdateStatus switch
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

    private static (string displayName, string displayAuthor) FormatModDisplayStrings(string modName, string author)
    {
        var displayName = modName.Length > MatchingConstants.MaxDisplayNameLength
                ? modName[..(MatchingConstants.MaxDisplayNameLength - 3)] + "..."
                : modName;
        var displayAuthor = author.Length > MatchingConstants.MaxDisplayAuthorLength
                ? author[..(MatchingConstants.MaxDisplayAuthorLength - 3)] + "..."
                : author;

        return (displayName, displayAuthor);
    }

    private static string FormatModLink(string name, string? apiUrl)
    {
        var escaped = name.EscapeMarkup();
        if (IsLinkUrlSafe(apiUrl))
        {
            return $"[white link={apiUrl}]{escaped}[/]";
        }
        return $"[white]{escaped}[/]";
    }

    private static string WithLink(string displayMarkup, string? url)
    {
        return IsLinkUrlSafe(url) ? $"[link={url}]{displayMarkup}[/]" : displayMarkup;
    }

    internal static bool IsLinkUrlSafe(string? url)
    {
        return !string.IsNullOrWhiteSpace(url) && !url.Contains('[') && !url.Contains(']');
    }
}
