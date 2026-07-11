using System;
using System.Collections.Concurrent;
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
public sealed class ModResolutionService(IModLookupStrategy modLookupStrategy) : IModResolutionService
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

        var updatedModsDict = new ConcurrentDictionary<string, Mod>(
            modsList.ToDictionary(m => m.Local.Guid)
        );

        await Parallel.ForEachAsync(
            modsWithNames,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 5,
                CancellationToken = cancellationToken
            },
            async (mod, ct) =>
            {
                var updatedMod = await ResolveAndFetchUrlAsync(mod, sptVersion, ct);
                updatedModsDict[mod.Local.Guid] = updatedMod;
            });

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

        var updatedPairsDict = new ConcurrentDictionary<string, ModPair>(
            pairsList.ToDictionary(p => p.SelectedMod.Local.Guid)
        );
        var allUpdatedMods = new ConcurrentBag<Mod>();

        await Parallel.ForEachAsync(
            validPairs,
            new ParallelOptions
            {
                MaxDegreeOfParallelism = 5,
                CancellationToken = cancellationToken
            },
            async (pair, ct) =>
            {
                var (updatedPair, updatedMods) = await ResolveAndFetchUrlForPairAsync(pair, sptVersion, ct);
                updatedPairsDict[pair.SelectedMod.Local.Guid] = updatedPair;
                foreach (var mod in updatedMods)
                {
                    allUpdatedMods.Add(mod);
                }
            });

        return (updatedPairsDict.Values.ToList(), allUpdatedMods.ToList());
    }

    private async Task<Mod> ResolveAndFetchUrlAsync(
        Mod mod,
        Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        if (mod.Api.ApiSourceCodeUrl is not null || mod.Api.ApiUrl is not null)
        {
            return mod;
        }

        var lookupResult = await modLookupStrategy.LookupModAsync(mod, sptVersion, null, cancellationToken);

        if (lookupResult is null)
        {
            return mod;
        }

        return mod.WithApiMatch(lookupResult.Value.Match);
    }

    private async Task<(ModPair UpdatedPair, List<Mod> UpdatedMods)> ResolveAndFetchUrlForPairAsync(
        ModPair pair,
        Version sptVersion,
        CancellationToken cancellationToken
    )
    {
        var selectedMod = pair.SelectedMod;

        if (selectedMod.Api.ApiSourceCodeUrl is not null || selectedMod.Api.ApiUrl is not null)
        {
            return (pair, []);
        }

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

        var lookupResult = await modLookupStrategy.LookupModAsync(selectedMod, sptVersion, guidsToTry, cancellationToken);

        if (lookupResult is null)
        {
            return (pair, []);
        }

        var match = lookupResult.Value.Match;

        var finalUpdatedMods = new List<Mod>();
        var finalUpdatedServerMod = pair.ServerMod;
        var finalUpdatedClientMod = pair.ClientMod;

        if (finalUpdatedServerMod != null)
        {
            finalUpdatedServerMod = finalUpdatedServerMod.WithApiMatch(match);
            finalUpdatedMods.Add(finalUpdatedServerMod);
        }

        if (finalUpdatedClientMod != null)
        {
            finalUpdatedClientMod = finalUpdatedClientMod.WithApiMatch(match);
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
}
