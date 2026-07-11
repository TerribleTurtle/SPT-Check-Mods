using System;
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
public sealed class DependencyUiRenderer(ITextRenderer textRenderer) : IDependencyUiRenderer
{
    /// <inheritdoc />
    public void DependencyResults(DependencyAnalysisResult result)
    {
        if (result.RootMods.Count == 0)
        {
            AnsiConsole.MarkupLine("[grey]No dependency information available.[/]");
            AnsiConsole.WriteLine();
            textRenderer.Rule();
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
        textRenderer.Rule();
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
        var version = node.Mod.Local.LocalVersion.EscapeMarkup();

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
        if (!string.IsNullOrWhiteSpace(node.Mod.Api.ApiUrl))
        {
            linkUrl = node.Mod.Api.ApiUrl;
        }
        else if (node.DependencyInfo != null && node.DependencyInfo.Id > 0 && !string.IsNullOrWhiteSpace(node.DependencyInfo.Slug))
        {
            linkUrl = ForgeUrls.ModPage(node.DependencyInfo.Id, node.DependencyInfo.Slug);
        }

        var label = $"{UiFormattingUtility.WithLink($"[{nameColor}]{name}[/]", linkUrl)} [grey]v{version}[/]";

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
            var nameDisplay = UiFormattingUtility.IsLinkUrlSafe(url)
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
}
