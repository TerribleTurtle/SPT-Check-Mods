using CheckMods.Configuration;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using SPTarkov.DI.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CheckMods.Services;

/// <summary>
/// Service responsible for matching local mods with their Forge API counterparts.
/// Uses the configured <see cref="IModLookupStrategy"/> for lookup.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class ModMatchingService(IModLookupStrategy modLookupStrategy, ILogger<ModMatchingService> logger)
    : IModMatchingService
{
    /// <summary>
    /// Minimum number of mods that must all fail before an all-failed batch is treated as a systemic fault.
    /// </summary>
    private const int MinimumModsForSystemicFailure = 3;

    /// <inheritdoc />
    public async Task<(Mod Mod, PendingConfirmation? Confirmation)> MatchModAsync(
        Mod mod,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Matching mod: {ModName} (GUID: {Guid})", mod.Local.LocalName, mod.Local.Guid);

        var lookupResult = await modLookupStrategy.LookupModAsync(mod, sptVersion, null, cancellationToken);

        if (lookupResult is null)
        {
            logger.LogDebug("No match found for mod: {ModName}", mod.Local.LocalName);
            return (mod.MarkUnmatched(), null);
        }

        var match = lookupResult.Value.Match;
        var score = lookupResult.Value.ConfidenceScore;

        if (score >= MatchingConstants.NameSearchFuzzyThreshold)
        {
            logger.LogDebug("Mod matched with high confidence: {ModName} -> {ApiName}", mod.Local.LocalName, match.Name);
            return (mod.WithApiMatch(match), null);
        }
        else
        {
            logger.LogDebug("Mod matched with low confidence ({Score}): {ModName} -> {ApiName}", score, mod.Local.LocalName, match.Name);
            var matchedMod = mod.WithApiMatch(match) with { Status = ModStatus.NeedsConfirmation };
            var confirmation = new PendingConfirmation(mod, match, score);
            return (matchedMod, confirmation);
        }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<Mod>> MatchModsAsync(
        IEnumerable<Mod> mods,
        SemanticVersioning.Version sptVersion,
        Action<Mod, int, int>? progressCallback = null,
        CancellationToken cancellationToken = default
    )
    {
        var modList = mods.ToList();
        var totalCount = modList.Count;
        var completedCount = 0;
        var failureCount = 0;
        Exception? firstFailure = null;

        var tasks = modList.Select(async (mod, index) =>
        {
            PendingConfirmation? confirmation = null;
            try
            {
                var matchResult = await MatchModAsync(mod, sptVersion, cancellationToken);
                mod = matchResult.Mod;
                confirmation = matchResult.Confirmation;
                if (confirmation != null)
                {
                    confirmation = confirmation with { ResultIndex = index };
                }
            }
            catch (OperationCanceledException)
            {
                throw;
            }
            catch (Exception ex)
                when (ex is HttpRequestException or System.Text.Json.JsonException or InvalidOperationException)
            {
                // Isolate per-mod failures: mark this mod unmatched and record the failure.
                logger.LogWarning(ex, "Failed to match mod: {ModName}", mod.Local.LocalName);
                mod = mod.MarkUnmatched();
                Interlocked.Increment(ref failureCount);
                Interlocked.CompareExchange(ref firstFailure, ex, null);
            }

            var current = Interlocked.Increment(ref completedCount);
            progressCallback?.Invoke(mod, current, totalCount);

            return (Mod: mod, Confirmation: confirmation);
        });

        var resultsWithConfirmations = await Task.WhenAll(tasks);

        // When every mod fails and enough mods are involved to be meaningful, throw a systemic failure.
        if (totalCount >= MinimumModsForSystemicFailure && failureCount == totalCount)
        {
            throw new InvalidOperationException(
                $"Failed to match any of the {totalCount} mods against the Forge API.",
                firstFailure
            );
        }

        var results = resultsWithConfirmations.Select(r => r.Mod).ToList();
        var pendingConfirmations = resultsWithConfirmations
            .Where(r => r.Confirmation != null)
            .Select(r => r.Confirmation!)
            .OrderBy(c => c.ResultIndex)
            .ToList();

        if (pendingConfirmations.Count > 0)
        {
            await HandlePendingConfirmationsAsync(pendingConfirmations, results);
        }

        return results;
    }

    /// <summary>
    /// Handles user confirmations for low-confidence mod matches.
    /// </summary>
    private static async Task HandlePendingConfirmationsAsync(
        List<PendingConfirmation> pendingConfirmations,
        List<Mod> orderedResults
    )
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

        foreach (var pending in pendingConfirmations)
        {
            var displayMod = pending.OriginalMod.Local;
            var isMatch = await AnsiConsole.ConfirmAsync(
                $"[yellow]Is '[white]{displayMod.LocalName.EscapeMarkup()}[/]' by '[white]{displayMod.LocalAuthor?.EscapeMarkup() ?? "Unknown"}[/]' the same as '[white]{pending.ApiMatch.Name.EscapeMarkup()}[/]' by '[white]{pending.ApiMatch.Owner?.Name.EscapeMarkup() ?? "N/A"}[/]'? ([grey]Confidence: {pending.ConfidenceScore}%[/])[/]"
            );

            var resultIndex = orderedResults.FindIndex(r => r.Local.LocalName == pending.OriginalMod.Local.LocalName);
            if (resultIndex >= 0)
            {
                if (isMatch)
                {
                    orderedResults[resultIndex] = orderedResults[resultIndex].WithApiMatch(pending.ApiMatch);
                }
                else
                {
                    orderedResults[resultIndex] = orderedResults[resultIndex].MarkUnmatched();
                }
            }
        }

        AnsiConsole.WriteLine();
    }
}
