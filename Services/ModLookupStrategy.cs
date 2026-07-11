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
using Version = SemanticVersioning.Version;

namespace CheckMods.Services;

/// <inheritdoc />
[Injectable(InjectionType.Transient)]
public sealed class ModLookupStrategy(IForgeApiService forgeApiService) : IModLookupStrategy
{
    /// <inheritdoc />
    public async Task<(ModSearchResult Match, int ConfidenceScore)?> LookupModAsync(
        Mod mod,
        Version sptVersion,
        IReadOnlyList<string>? additionalGuidsToTry = null,
        CancellationToken cancellationToken = default
    )
    {
        var guidsToTry = new List<string>();

        if (!string.IsNullOrWhiteSpace(mod.Local.Guid))
        {
            guidsToTry.Add(mod.Local.Guid);
        }

        guidsToTry.AddRange(mod.Local.AlternateGuids);

        if (additionalGuidsToTry is not null)
        {
            guidsToTry.AddRange(additionalGuidsToTry.Where(g => !string.IsNullOrWhiteSpace(g)));
        }

        guidsToTry = guidsToTry.Distinct(StringComparer.OrdinalIgnoreCase).ToList();

        ModSearchResult? incompatibleMatch = null;

        // Try GUID lookups first
        foreach (var guid in guidsToTry)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var guidResult = await forgeApiService.GetModByGuidAsync(guid, sptVersion, cancellationToken);

            if (guidResult.TryPickT0(out var match, out _))
            {
                return (match, 100);
            }

            if (guidResult.TryPickT2(out var noCompat, out _))
            {
                incompatibleMatch ??= noCompat.Mod;
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

            var searchResults = searchResult.Match(results => results, _ => []);

            if (searchResults.Count == 0)
            {
                continue;
            }

            var bestMatch = FindBestMatch(mod, searchResults);
            if (bestMatch is not null)
            {
                return bestMatch;
            }
        }

        // Nothing compatible turned up. If a GUID matched an incompatible mod, return it.
        if (incompatibleMatch is not null)
        {
            return (incompatibleMatch, 100);
        }

        return null;
    }

    /// <summary>
    /// Builds a list of search terms to try, in order of preference.
    /// </summary>
    private static List<string> BuildSearchTerms(Mod mod)
    {
        List<string> terms = [];
        var seen = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

        AddIfNew(terms, seen, mod.Local.LocalName);

        var spacedName = mod.Local.LocalName?.Replace("-", " ").Replace("_", " ");
        if (!string.Equals(spacedName, mod.Local.LocalName, StringComparison.OrdinalIgnoreCase))
        {
            AddIfNew(terms, seen, spacedName);
        }

        var nameWithoutSuffix = RemoveComponentSuffix(mod.Local.LocalName);
        if (!string.Equals(nameWithoutSuffix, mod.Local.LocalName, StringComparison.OrdinalIgnoreCase))
        {
            AddIfNew(terms, seen, nameWithoutSuffix);
        }

        if (!string.IsNullOrWhiteSpace(mod.Local.Guid))
        {
            var guidName = ModNameNormalizer.ExtractNameFromGuid(mod.Local.Guid);
            AddIfNew(terms, seen, guidName);

            var guidNameWithoutSuffix = RemoveComponentSuffix(guidName);
            if (!string.Equals(guidNameWithoutSuffix, guidName, StringComparison.OrdinalIgnoreCase))
            {
                AddIfNew(terms, seen, guidNameWithoutSuffix);
            }
        }

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

    /// <summary>
    /// Finds the best matching API result for a given mod using multiple comparison strategies.
    /// </summary>
    private static (ModSearchResult Result, int Score)? FindBestMatch(Mod mod, List<ModSearchResult> searchResults)
    {
        foreach (var result in searchResults)
        {
            if (ModNameNormalizer.IsExactMatch(mod.Local.LocalName, result.Name))
            {
                return (result, 100);
            }
        }

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

        foreach (var result in searchResults)
        {
            if (!string.IsNullOrWhiteSpace(result.Slug))
            {
                if (ModNameNormalizer.IsExactMatch(mod.Local.LocalName, result.Slug, removeComponentSuffixes: true))
                {
                    return (result, 100);
                }

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
