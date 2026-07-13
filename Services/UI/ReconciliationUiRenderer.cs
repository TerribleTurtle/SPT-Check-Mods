using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services.UI;

/// <inheritdoc />
[Injectable(InjectionType.Singleton)]
public sealed class ReconciliationUiRenderer(ITextRenderer textRenderer) : IReconciliationUiRenderer
{
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
                    var modName = pair.SelectedMod.Local.LocalName;

                    var nameDisplay = UiFormattingUtility.FormatModLink(modName, pair.SelectedMod.Api.ApiUrl);

                    var modNode = tree.AddNode(nameDisplay);
                    foreach (var note in pair.Notes)
                    {
                        modNode.AddNode($"[yellow]{note.EscapeMarkup()}[/]");
                    }

                    var reportUrl = !string.IsNullOrWhiteSpace(pair.SelectedMod.Local.Url)
                        ? pair.SelectedMod.Local.Url
                        : (
                            !string.IsNullOrWhiteSpace(pair.SelectedMod.Api.ApiSourceCodeUrl)
                                ? pair.SelectedMod.Api.ApiSourceCodeUrl
                                : pair.SelectedMod.Api.ApiUrl
                        );

                    var guidMismatch =
                        pair.ServerMod != null
                        && pair.ClientMod != null
                        && !string.Equals(
                            pair.ServerMod.Local.Guid,
                            pair.ClientMod.Local.Guid,
                            StringComparison.OrdinalIgnoreCase
                        );

                    if (guidMismatch)
                    {
                        modNode.AddNode(
                            "[grey]Matched by name, but the GUIDs differ. This is likely a mod packaged with mismatched GUIDs.[/]"
                        );

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

        AnsiConsole.MarkupLine(
            $"[grey]Final mod count: {result.Mods.Count} (matched pairs: {result.ReconciledPairs.Count}, server-only: {result.UnmatchedServerMods.Count}, client-only: {result.UnmatchedClientMods.Count})[/]"
        );
        AnsiConsole.WriteLine();
        textRenderer.Rule();
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
            var modType = mod.Local.IsServerMod ? "Server" : "Client";
            var modName = !string.IsNullOrWhiteSpace(mod.Local.LocalName)
                ? mod.Local.LocalName
                : Path.GetFileName(mod.Local.FilePath);

            var nameDisplay = UiFormattingUtility.FormatModLink(modName, mod.Api.ApiUrl);

            var modNode = tree.AddNode($"[grey]{modType}:[/] {nameDisplay}");
            foreach (var warning in mod.LoadWarnings)
            {
                modNode.AddNode($"[yellow]{warning.EscapeMarkup()}[/]");
            }

            if (!string.IsNullOrWhiteSpace(mod.Api.ApiSourceCodeUrl))
            {
                modNode.AddNode($"[grey]Please report:[/] [link]{mod.Api.ApiSourceCodeUrl.EscapeMarkup()}[/]");
            }
            else if (!string.IsNullOrWhiteSpace(mod.Api.ApiUrl))
            {
                modNode.AddNode($"[grey]Please report:[/] [link]{mod.Api.ApiUrl.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(tree);
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

            if (!string.IsNullOrWhiteSpace(mod.Local.Guid))
            {
                modNode.AddNode($"[grey]GUID: {mod.Local.Guid.EscapeMarkup()}[/]");
            }

            if (!string.IsNullOrWhiteSpace(mod.Local.FilePath))
            {
                modNode.AddNode($"[grey]Path: {mod.Local.FilePath.EscapeMarkup()}[/]");
            }
        }

        AnsiConsole.Write(tree);

        AnsiConsole.WriteLine();
        AnsiConsole.MarkupLine(
            "[grey]These were not matched to a Forge listing. That's expected for a mod that isn't published on the Forge, or for a mod which includes multiple plugins where only one uses the GUID linked to the Forge. No action is needed unless you expected one of these to be its own mod on Forge.[/]"
        );
        AnsiConsole.WriteLine();
    }
}
