using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using SPTarkov.DI.Annotations;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace CheckModsExtended.Services;

/// <summary>
/// Service responsible for matching local mods with their Forge API counterparts.
/// Uses the configured <see cref="IModLookupStrategy"/> for lookup.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class ModMatchingService(
    IModLookupStrategy modLookupStrategy,
    IOptions<ModMatchingOptions> options,
    IModCheckReporter reporter,
    ILogger<ModMatchingService> logger)
    : IModMatchingService
{
    private readonly ModMatchingOptions _options = options.Value;

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
        IProgress<int>? progress = null,
        CancellationToken cancellationToken = default
    )
    {
        var modList = mods.ToList();
        var totalCount = modList.Count;
        var completedCount = 0;
        var failureCount = 0;
        Exception? firstFailure = null;

        var resultsWithConfirmations = new (Mod Mod, PendingConfirmation? Confirmation)[totalCount];

        await Parallel.ForEachAsync(
            modList.Select((mod, index) => (mod, index)),
            new ParallelOptions
            {
                CancellationToken = cancellationToken
            },
            async (item, ct) =>
            {
                var mod = item.mod;
                var index = item.index;
                PendingConfirmation? confirmation = null;

                try
                {
                    var matchResult = await MatchModAsync(mod, sptVersion, ct);
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

                resultsWithConfirmations[index] = (Mod: mod, Confirmation: confirmation);

                var current = Interlocked.Increment(ref completedCount);
                progress?.Report(current);
            });

        // When every mod fails and enough mods are involved to be meaningful, throw a systemic failure.
        if (totalCount >= _options.MinimumModsForSystemicFailure && failureCount == totalCount)
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
    private async Task HandlePendingConfirmationsAsync(
        List<PendingConfirmation> pendingConfirmations,
        List<Mod> orderedResults
    )
    {
        reporter.PendingConfirmationsSummary(pendingConfirmations);

        foreach (var pending in pendingConfirmations)
        {
            var isMatch = await reporter.PromptForConfirmationAsync(pending);

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
        
        reporter.Blank();
    }
}

