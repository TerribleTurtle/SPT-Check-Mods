using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckMods.Configuration;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using SemanticVersioning;
using SPTarkov.DI.Annotations;
using Version = SemanticVersioning.Version;

namespace CheckMods.Services;

/// <inheritdoc />
[Injectable(InjectionType.Transient)]
public sealed class ModResolutionService(IForgeApiService forgeApiService) : IModResolutionService
{
    /// <inheritdoc />
    public async Task<IReadOnlyList<Mod>> FetchSourceCodeUrlsForModsAsync(
        IEnumerable<Mod> mods,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var modsList = mods.ToList();
        var modsWithNames = modsList.Where(m => !string.IsNullOrWhiteSpace(m.Local.LocalName)).ToList();

        if (modsWithNames.Count == 0)
        {
            return modsList;
        }

        // Dispatch the lookups concurrently and let the rate limiter throttle.
        var updatedModsDict = modsList.ToDictionary(m => m.Local.Guid);

        var tasks = modsWithNames.Select(async mod =>
        {
            var updatedMod = await ResolveAndFetchUrlAsync(mod, sptVersion, cancellationToken);
            return (OriginalGuid: mod.Local.Guid, UpdatedMod: updatedMod);
        });

        var results = await Task.WhenAll(tasks);
        foreach (var (guid, updatedMod) in results)
        {
            updatedModsDict[guid] = updatedMod;
        }

        return updatedModsDict.Values.ToList();
    }

    /// <inheritdoc />
    public async Task<(IReadOnlyList<ModPair> UpdatedPairs, IReadOnlyList<Mod> UpdatedMods)> FetchSourceCodeUrlsForPairedModsAsync(
        IEnumerable<ModPair> pairs,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var pairsList = pairs.ToList();
        var validPairs = pairsList
            .Where(p =>
                !string.IsNullOrWhiteSpace(p.ServerMod?.Local.LocalName)
                || !string.IsNullOrWhiteSpace(p.ClientMod?.Local.LocalName)
            )
            .ToList();

        if (validPairs.Count == 0)
        {
            return (pairsList, []);
        }

        // Dispatch the lookups concurrently and let the rate limiter throttle.
        var updatedPairsDict = pairsList.ToDictionary(p => p.SelectedMod.Local.Guid);
        var allUpdatedMods = new List<Mod>();

        var tasks = validPairs.Select(async pair =>
        {
            var (updatedPair, updatedMods) = await ResolveAndFetchUrlForPairAsync(pair, sptVersion, cancellationToken);
            return (OriginalGuid: pair.SelectedMod.Local.Guid, UpdatedPair: updatedPair, UpdatedMods: updatedMods);
        });

        var results = await Task.WhenAll(tasks);
        foreach (var (guid, updatedPair, updatedMods) in results)
        {
            updatedPairsDict[guid] = updatedPair;
            allUpdatedMods.AddRange(updatedMods);
        }

        return (updatedPairsDict.Values.ToList(), allUpdatedMods);
    }

    /// <summary>
    /// Fetches source code URL from the API for a single mod.
    /// </summary>
    private async Task<Mod> ResolveAndFetchUrlAsync(
        Mod mod,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        // Skip if already has API info
        if (mod.Api.ApiSourceCodeUrl is not null || mod.Api.ApiUrl is not null)
        {
            return mod;
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
            return mod;
        }

        return mod.WithApiMatch(apiResult);
    }

    /// <summary>
    /// Fetches and applies Forge API info for a single reconciled pair, trying both the server and client GUIDs
    /// before falling back to a fuzzy name search.
    /// </summary>
    private async Task<(ModPair UpdatedPair, List<Mod> UpdatedMods)> ResolveAndFetchUrlForPairAsync(
        ModPair pair,
        Version sptVersion,
        CancellationToken cancellationToken
    )
    {
        var selectedMod = pair.SelectedMod;

        // Skip if already has API info
        if (selectedMod.Api.ApiSourceCodeUrl is not null || selectedMod.Api.ApiUrl is not null)
        {
            return (pair, []);
        }

        ModSearchResult? apiResult = null;

        // Collect all unique GUIDs to try (server GUID, client GUID)
        List<string> guidsToTry = [];
        if (!string.IsNullOrWhiteSpace(pair.ServerMod?.Local.Guid))
        {
            guidsToTry.Add(pair.ServerMod.Local.Guid);
        }

        if (
            !string.IsNullOrWhiteSpace(pair.ClientMod?.Local.Guid)
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

            var updatedMods = new List<Mod>();
            var updatedServerMod = pair.ServerMod;
            var updatedClientMod = pair.ClientMod;

            if (updatedServerMod != null)
            {
                updatedServerMod = updatedServerMod.WithApiMatch(match);
                updatedMods.Add(updatedServerMod);
            }
            
            if (updatedClientMod != null)
            {
                updatedClientMod = updatedClientMod.WithApiMatch(match);
                updatedMods.Add(updatedClientMod);
            }

            var updatedPair = new ModPair
            {
                ServerMod = updatedServerMod,
                ClientMod = updatedClientMod,
                SelectedMod = pair.SelectedMod == pair.ServerMod ? updatedServerMod! : updatedClientMod!,
                Notes = pair.Notes
            };
            return (updatedPair, updatedMods);
        }

        // If not found by any GUID, try searching by name with fuzzy matching
        apiResult ??= await SearchModByNameAsync(selectedMod, sptVersion, cancellationToken);

        if (apiResult is null)
        {
            return (pair, []);
        }

        var finalUpdatedMods = new List<Mod>();
        var finalUpdatedServerMod = pair.ServerMod;
        var finalUpdatedClientMod = pair.ClientMod;

        if (finalUpdatedServerMod != null)
        {
            finalUpdatedServerMod = finalUpdatedServerMod.WithApiMatch(apiResult);
            finalUpdatedMods.Add(finalUpdatedServerMod);
        }
        
        if (finalUpdatedClientMod != null)
        {
            finalUpdatedClientMod = finalUpdatedClientMod.WithApiMatch(apiResult);
            finalUpdatedMods.Add(finalUpdatedClientMod);
        }

        var finalUpdatedPair = new ModPair
        {
            ServerMod = finalUpdatedServerMod,
            ClientMod = finalUpdatedClientMod,
            SelectedMod = pair.SelectedMod == pair.ServerMod ? finalUpdatedServerMod! : finalUpdatedClientMod!,
            Notes = pair.Notes
        };

        return (finalUpdatedPair, finalUpdatedMods);
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
