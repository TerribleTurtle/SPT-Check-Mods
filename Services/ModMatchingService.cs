using CheckMods.Configuration;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using Microsoft.Extensions.Logging;
using Spectre.Console;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services;

/// <summary>
/// Service responsible for matching local mods with their Forge API counterparts.
/// Uses GUID lookup as the primary method with multiple fallback strategies.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class ModMatchingService(IForgeApiService forgeApiService, ILogger<ModMatchingService> logger)
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

        // A GUID match whose mod has no SPT-compatible version, held as a fallback.
        ModSearchResult? incompatibleMatch = null;

        // Try GUID lookup first
        if (!string.IsNullOrWhiteSpace(mod.Local.Guid))
        {
            var guidResult = await forgeApiService.GetModByGuidAsync(mod.Local.Guid, sptVersion, cancellationToken);

            if (guidResult.TryPickT0(out var guidMatch, out _))
            {
                logger.LogDebug("Mod matched by GUID: {ModName} -> {ApiName}", mod.Local.LocalName, guidMatch.Name);
                return (mod.WithApiMatch(guidMatch), null);
            }

            if (guidResult.TryPickT2(out var guidNoCompat, out _))
            {
                incompatibleMatch = guidNoCompat.Mod;
            }
        }

        // Try alternate GUIDs
        foreach (var alternateGuid in mod.Local.AlternateGuids)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var altGuidResult = await forgeApiService.GetModByGuidAsync(alternateGuid, sptVersion, cancellationToken);

            if (altGuidResult.TryPickT0(out var altGuidMatch, out _))
            {
                return (mod.WithApiMatch(altGuidMatch), null);
            }

            if (altGuidResult.TryPickT2(out var altNoCompat, out _))
            {
                incompatibleMatch ??= altNoCompat.Mod;
            }
        }

        // Build list of search terms to try in order of preference
        var searchTerms = BuildSearchTerms(mod);

        foreach (var searchTerm in searchTerms)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var searchResult = mod.Local.IsServerMod
                ? await forgeApiService.SearchModsAsync(searchTerm, sptVersion, cancellationToken)
                : await forgeApiService.SearchClientModsAsync(searchTerm, sptVersion, cancellationToken);

            // Extract the list from the result (empty list if error)
            var searchResults = searchResult.Match(
                results => results,
                _ => [] // ApiError - return empty list
            );

            if (searchResults.Count == 0)
            {
                continue;
            }

            // Try to find a matching result using multiple comparison strategies
            var bestMatch = FindBestMatch(mod, searchResults);
            if (bestMatch is null)
            {
                continue;
            }

            if (bestMatch.Value.Score >= MatchingConstants.NameSearchFuzzyThreshold)
            {
                return (mod.WithApiMatch(bestMatch.Value.Result), null);
            }
            else
            {
                var matchedMod = mod.WithApiMatch(bestMatch.Value.Result);
                matchedMod = matchedMod with { Status = ModStatus.NeedsConfirmation };
                var confirmation = new PendingConfirmation(mod, bestMatch.Value.Result, bestMatch.Value.Score);
                return (matchedMod, confirmation);
            }
        }

        // Nothing compatible turned up. If a GUID matched a mod that has no SPT-compatible version, keep it as a match.
        if (incompatibleMatch is not null)
        {
            logger.LogDebug(
                "Mod matched by GUID but has no SPT-compatible version: {ModName} -> {ApiName}",
                mod.Local.LocalName,
                incompatibleMatch.Name
            );
            return (mod.WithApiMatch(incompatibleMatch), null);
        }

        logger.LogDebug("No match found for mod: {ModName}", mod.Local.LocalName);
        return (mod.MarkUnmatched(), null);
    }

    /// <summary>
    /// Builds a list of search terms to try, in order of preference.
    /// </summary>
    private static List<string> BuildSearchTerms(Mod mod)
    {
        List<string> terms = [];
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        // 1. Original local name
        AddIfNew(terms, seen, mod.Local.LocalName);

        // 1b. Additive fallback: replace '-' and '_' with ' ' in LocalName
        var spacedName = mod.Local.LocalName?.Replace("-", " ").Replace("_", " ");
        if (!string.Equals(spacedName, mod.Local.LocalName, StringComparison.OrdinalIgnoreCase))
        {
            AddIfNew(terms, seen, spacedName);
        }

        // 2. Name without server/client suffix (e.g., "ModNameServer" -> "ModName")
        var nameWithoutSuffix = RemoveComponentSuffix(mod.Local.LocalName);
        if (!string.Equals(nameWithoutSuffix, mod.Local.LocalName, StringComparison.OrdinalIgnoreCase))
        {
            AddIfNew(terms, seen, nameWithoutSuffix);
        }

        // 3. Name extracted from GUID (e.g., "com.author.modname" -> "modname")
        if (!string.IsNullOrWhiteSpace(mod.Local.Guid))
        {
            var guidName = ModNameNormalizer.ExtractNameFromGuid(mod.Local.Guid);
            AddIfNew(terms, seen, guidName);

            // Also try without suffix
            var guidNameWithoutSuffix = RemoveComponentSuffix(guidName);
            if (!string.Equals(guidNameWithoutSuffix, guidName, StringComparison.OrdinalIgnoreCase))
            {
                AddIfNew(terms, seen, guidNameWithoutSuffix);
            }
        }

        // 4. Author + name combination (if author is known and not generic)
        if (
            !string.IsNullOrWhiteSpace(mod.Local.LocalAuthor)
            && !string.Equals(mod.Local.LocalAuthor, "Unknown", StringComparison.OrdinalIgnoreCase)
        )
        {
            AddIfNew(terms, seen, $"{mod.Local.LocalAuthor} {mod.Local.LocalName}");
        }

        return terms;
    }

    /// <summary>
    /// Removes common component suffixes from a mod name.
    /// </summary>
    private static string? RemoveComponentSuffix(string? name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            return name;
        }

        var matchingSuffix = ModNameNormalizer.SuffixesToRemove.FirstOrDefault(s =>
            name.EndsWith(s, StringComparison.OrdinalIgnoreCase) && name.Length > s.Length
        );

        return matchingSuffix is not null ? name[..^matchingSuffix.Length] : name;
    }

    /// <summary>
    /// Adds a term to the list if it's not already present and not empty.
    /// </summary>
    private static void AddIfNew(List<string> terms, HashSet<string> seen, string? term)
    {
        if (!string.IsNullOrWhiteSpace(term) && seen.Add(term))
        {
            terms.Add(term);
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
                    confirmation.ResultIndex = index;
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

    /// <summary>
    /// Finds the best matching API result for a given mod using multiple comparison strategies.
    /// </summary>
    private static (ModSearchResult Result, int Score)? FindBestMatch(Mod mod, List<ModSearchResult> searchResults)
    {
        // 1. Try exact normalized name match
        foreach (var result in searchResults)
        {
            if (ModNameNormalizer.IsExactMatch(mod.Local.LocalName, result.Name))
            {
                return (result, 100);
            }
        }

        // 2. Try exact match with component suffix removed
        var nameWithoutSuffix = RemoveComponentSuffix(mod.Local.LocalName);
        if (!string.Equals(nameWithoutSuffix, mod.Local.LocalName, StringComparison.OrdinalIgnoreCase))
        {
            foreach (var result in searchResults)
            {
                if (ModNameNormalizer.IsExactMatch(nameWithoutSuffix, result.Name, removeComponentSuffixes: true))
                {
                    return (result, 100);
                }
            }
        }

        // 3. Try matching by slug (normalized)
        foreach (var result in searchResults)
        {
            if (!string.IsNullOrWhiteSpace(result.Slug))
            {
                // Compare normalized slug to normalized local name
                if (ModNameNormalizer.IsExactMatch(mod.Local.LocalName, result.Slug, removeComponentSuffixes: true))
                {
                    return (result, 100);
                }

                // Also compare GUID name part to slug
                if (!string.IsNullOrWhiteSpace(mod.Local.Guid))
                {
                    var guidName = ModNameNormalizer.ExtractNameFromGuid(mod.Local.Guid);
                    if (ModNameNormalizer.IsExactMatch(guidName, result.Slug, removeComponentSuffixes: true))
                    {
                        return (result, 100);
                    }
                }
            }
        }

        // 4. Try matching by author + name combination
        if (
            !string.IsNullOrWhiteSpace(mod.Local.LocalAuthor)
            && !string.Equals(mod.Local.LocalAuthor, "Unknown", StringComparison.OrdinalIgnoreCase)
        )
        {
            foreach (var result in searchResults)
            {
                if (
                    result.Owner is not null
                    && string.Equals(mod.Local.LocalAuthor, result.Owner.Name, StringComparison.OrdinalIgnoreCase)
                    && ModNameNormalizer.IsExactMatch(mod.Local.LocalName, result.Name, removeComponentSuffixes: true)
                )
                {
                    return (result, 100);
                }
            }
        }

        // 5. Try fuzzy matching with minimum threshold
        var bestFuzzyMatch = searchResults
            .Select(r => new
            {
                Result = r,
                NameScore = ModNameNormalizer.GetFuzzyMatchScore(mod.Local.LocalName, r.Name),
                SlugScore = !string.IsNullOrWhiteSpace(r.Slug)
                    ? ModNameNormalizer.GetFuzzyMatchScore(mod.Local.LocalName, r.Slug)
                    : 0,
            })
            .Select(x => new { x.Result, Score = Math.Max(x.NameScore, x.SlugScore) })
            .Where(x => x.Score >= MatchingConstants.MinimumFuzzyMatchScore)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        if (bestFuzzyMatch is not null)
        {
            return (bestFuzzyMatch.Result, bestFuzzyMatch.Score);
        }

        return null;
    }
}
