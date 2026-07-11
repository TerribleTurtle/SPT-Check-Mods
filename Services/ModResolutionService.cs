using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Configuration;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using SPTarkov.DI.Annotations;
using SemanticVersioning;
using Version = SemanticVersioning.Version;

namespace CheckMods.Services;

/// <inheritdoc />
[Injectable(InjectionType.Transient)]
public sealed class ModResolutionService(IForgeApiService forgeApiService) : IModResolutionService
{
    /// <inheritdoc />
    public async Task FetchSourceCodeUrlsForModsAsync(
        List<Mod> mods,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        // Dispatch the lookups concurrently and let the rate limiter throttle.
        await Task.WhenAll(mods.Select(mod => FetchSourceCodeUrlForModAsync(mod, sptVersion, cancellationToken)));
    }

    /// <inheritdoc />
    public async Task FetchSourceCodeUrlsForPairedModsAsync(
        List<ModPair> pairs,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        // Dispatch the lookups concurrently and let the rate limiter throttle.
        await Task.WhenAll(pairs.Select(pair => FetchSourceCodeUrlForPairAsync(pair, sptVersion, cancellationToken)));
    }

    /// <summary>
    /// Fetches source code URL from the API for a single mod.
    /// </summary>
    private async Task FetchSourceCodeUrlForModAsync(
        Mod mod,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        // Skip if already has API info
        if (mod.Api.ApiSourceCodeUrl is not null || mod.Api.ApiUrl is not null)
        {
            return;
        }

        ModSearchResult? apiResult = null;

        // Try to find the mod by GUID first
        if (!string.IsNullOrWhiteSpace(mod.Local.Guid))
        {
            var guidResult = await forgeApiService.GetModByGuidAsync(mod.Local.Guid, sptVersion, cancellationToken);
            if (guidResult.TryPickT0(out var match, out _))
            {
                apiResult = match;
            }
        }

        // If not found by GUID, try searching by name with fuzzy matching
        apiResult ??= await SearchModByNameAsync(mod, sptVersion, cancellationToken);

        if (apiResult is null)
        {
            return;
        }

        mod.UpdateFromApiMatch(apiResult);
    }

    /// <summary>
    /// Fetches and applies Forge API info for a single reconciled pair, trying both the server and client GUIDs
    /// before falling back to a fuzzy name search.
    /// </summary>
    private async Task FetchSourceCodeUrlForPairAsync(
        ModPair pair,
        Version sptVersion,
        CancellationToken cancellationToken
    )
    {
        var selectedMod = pair.SelectedMod;

        // Skip if already has API info
        if (selectedMod.Api.ApiSourceCodeUrl is not null || selectedMod.Api.ApiUrl is not null)
        {
            return;
        }

        ModSearchResult? apiResult = null;

        // Collect all unique GUIDs to try (server GUID, client GUID)
        List<string> guidsToTry = [];
        if (!string.IsNullOrWhiteSpace(pair.ServerMod.Local.Guid))
        {
            guidsToTry.Add(pair.ServerMod.Local.Guid);
        }

        if (
            !string.IsNullOrWhiteSpace(pair.ClientMod.Local.Guid)
            && !guidsToTry.Contains(pair.ClientMod.Local.Guid, StringComparer.OrdinalIgnoreCase)
        )
        {
            guidsToTry.Add(pair.ClientMod.Local.Guid);
        }

        // Try each GUID until we find a match
        foreach (var guid in guidsToTry)
        {
            var guidResult = await forgeApiService.GetModByGuidAsync(guid, sptVersion, cancellationToken);
            if (!guidResult.TryPickT0(out var match, out _))
            {
                continue;
            }

            apiResult = match;
            break;
        }

        // If not found by any GUID, try searching by name with fuzzy matching
        apiResult ??= await SearchModByNameAsync(selectedMod, sptVersion, cancellationToken);

        if (apiResult is null)
        {
            return;
        }

        selectedMod.UpdateFromApiMatch(apiResult);
    }

    /// <summary>
    /// Searches for a mod by name using fuzzy matching.
    /// </summary>
    private async Task<ModSearchResult?> SearchModByNameAsync(
        Mod mod,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        if (string.IsNullOrWhiteSpace(mod.Local.LocalName))
        {
            return null;
        }

        var searchResult = mod.Local.IsServerMod
            ? await forgeApiService.SearchModsAsync(mod.Local.LocalName, sptVersion, cancellationToken)
            : await forgeApiService.SearchClientModsAsync(mod.Local.LocalName, sptVersion, cancellationToken);

        // Extract search results or empty list on error
        var searchResults = searchResult.Match(
            results => results,
            _ => [] // ApiError
        );

        if (searchResults.Count == 0)
        {
            return null;
        }

        var normalizedLocalName = ModNameNormalizer.Normalize(mod.Local.LocalName);

        // Try exact match first
        var apiResult = searchResults.FirstOrDefault(r =>
            ModNameNormalizer.Normalize(r.Name).Equals(normalizedLocalName, StringComparison.OrdinalIgnoreCase)
        );

        if (apiResult is not null)
        {
            return apiResult;
        }

        // If no exact match, try fuzzy match with high threshold
        var bestMatch = searchResults
            .Select(r => (Result: r, Score: ModNameNormalizer.GetFuzzyMatchScore(mod.Local.LocalName, r.Name)))
            .Where(x => x.Score >= MatchingConstants.NameSearchFuzzyThreshold)
            .OrderByDescending(x => x.Score)
            .FirstOrDefault();

        return bestMatch.Result;
    }
}
