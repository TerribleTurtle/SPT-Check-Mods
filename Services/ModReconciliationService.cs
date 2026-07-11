using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services;

/// <summary>
/// Service responsible for reconciling server and client mod components. Matches components of the same mod and selects
/// the best version when duplicates exist.
/// </summary>
[Injectable(InjectionType.Transient)]
public sealed class ModReconciliationService(ILogger<ModReconciliationService> logger) : IModReconciliationService
{
    /// <inheritdoc />
    public ModReconciliationResult ReconcileMods(List<Mod> serverMods, List<Mod> clientMods)
    {
        logger.LogDebug(
            "Reconciling {ServerCount} server mods with {ClientCount} client mods",
            serverMods.Count,
            clientMods.Count
        );

        List<ModPair> reconciledPairs = [];
        var matchedServerIndices = new HashSet<int>();
        var matchedClientIndices = new HashSet<int>();

        var serverByGuid = new Dictionary<string, List<int>>(StringComparer.OrdinalIgnoreCase);
        var serverByNormName = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var serverByNormGuidName = new Dictionary<string, List<int>>(StringComparer.Ordinal);
        var serverHasGuidName = new bool[serverMods.Count];

        for (int i = 0; i < serverMods.Count; i++)
        {
            var serverMod = serverMods[i];

            if (!string.IsNullOrWhiteSpace(serverMod.Local.Guid))
            {
                if (!serverByGuid.TryGetValue(serverMod.Local.Guid, out var list))
                {
                    list = [];
                    serverByGuid[serverMod.Local.Guid] = list;
                }
                list.Add(i);
            }

            var normName = ModNameNormalizer.Normalize(serverMod.Local.LocalName, removeComponentSuffixes: true);
            if (!string.IsNullOrEmpty(normName))
            {
                if (!serverByNormName.TryGetValue(normName, out var list))
                {
                    list = [];
                    serverByNormName[normName] = list;
                }
                list.Add(i);
            }

            var guidName = ModNameNormalizer.ExtractNameFromGuid(serverMod.Local.Guid);
            if (!string.IsNullOrEmpty(guidName))
            {
                serverHasGuidName[i] = true;
                var normGuidName = ModNameNormalizer.Normalize(guidName, removeComponentSuffixes: true);
                if (!string.IsNullOrEmpty(normGuidName))
                {
                    if (!serverByNormGuidName.TryGetValue(normGuidName, out var list))
                    {
                        list = [];
                        serverByNormGuidName[normGuidName] = list;
                    }
                    list.Add(i);
                }
            }
        }

        void Match(int serverIdx, int clientIdx)
        {
            var serverMod = serverMods[serverIdx];
            var clientMod = clientMods[clientIdx];
            var (selectedMod, notes) = SelectBestMod(serverMod, clientMod);

            serverMod = serverMod with { Local = serverMod.Local with { PairedComponentPath = clientMod.Local.FilePath } };
            clientMod = clientMod with { Local = clientMod.Local with { PairedComponentPath = serverMod.Local.FilePath } };

            reconciledPairs.Add(
                new ModPair
                {
                    ServerMod = serverMod,
                    ClientMod = clientMod,
                    SelectedMod = selectedMod,
                    Notes = notes,
                }
            );

            matchedServerIndices.Add(serverIdx);
            matchedClientIndices.Add(clientIdx);
        }

        // Pass 1: Exact GUID pairs first.
        for (int clientIdx = 0; clientIdx < clientMods.Count; clientIdx++)
        {
            var clientMod = clientMods[clientIdx];
            if (string.IsNullOrWhiteSpace(clientMod.Local.Guid))
            {
                continue;
            }

            if (serverByGuid.TryGetValue(clientMod.Local.Guid, out var serverIndices))
            {
                foreach (var serverIdx in serverIndices)
                {
                    if (!matchedServerIndices.Contains(serverIdx))
                    {
                        Match(serverIdx, clientIdx);
                        break;
                    }
                }
            }
        }

        // Pass 2: Pair the rest by name.
        var candidateIndices = new HashSet<int>();
        for (int clientIdx = 0; clientIdx < clientMods.Count; clientIdx++)
        {
            if (matchedClientIndices.Contains(clientIdx))
            {
                continue;
            }

            var clientMod = clientMods[clientIdx];
            var normName = ModNameNormalizer.Normalize(clientMod.Local.LocalName, removeComponentSuffixes: true);
            var guidName = ModNameNormalizer.ExtractNameFromGuid(clientMod.Local.Guid);
            var hasGuidName = !string.IsNullOrEmpty(guidName);
            var normGuidName = hasGuidName
                ? ModNameNormalizer.Normalize(guidName, removeComponentSuffixes: true)
                : null;

            candidateIndices.Clear();

            if (!string.IsNullOrEmpty(normName) && serverByNormName.TryGetValue(normName, out var nameMatches))
            {
                candidateIndices.UnionWith(nameMatches);
            }

            if (hasGuidName)
            {
                if (
                    !string.IsNullOrEmpty(normGuidName)
                    && serverByNormGuidName.TryGetValue(normGuidName, out var guidNameMatches)
                )
                {
                    candidateIndices.UnionWith(guidNameMatches);
                }

                if (
                    !string.IsNullOrEmpty(normName)
                    && serverByNormGuidName.TryGetValue(normName, out var guidNameToNameMatches)
                )
                {
                    candidateIndices.UnionWith(guidNameToNameMatches);
                }

                if (
                    !string.IsNullOrEmpty(normGuidName)
                    && serverByNormName.TryGetValue(normGuidName, out var nameToGuidNameMatches)
                )
                {
                    foreach (var idx in nameToGuidNameMatches)
                    {
                        if (serverHasGuidName[idx])
                        {
                            candidateIndices.Add(idx);
                        }
                    }
                }
            }

            int bestIdx = -1;
            foreach (var idx in candidateIndices)
            {
                if (!matchedServerIndices.Contains(idx))
                {
                    if (bestIdx == -1 || idx < bestIdx)
                    {
                        bestIdx = idx;
                    }
                }
            }

            if (bestIdx != -1)
            {
                Match(bestIdx, clientIdx);
            }
        }

        var unmatchedServerMods = serverMods.Where((_, idx) => !matchedServerIndices.Contains(idx)).ToList();
        var unmatchedClientMods = clientMods.Where((_, idx) => !matchedClientIndices.Contains(idx)).ToList();

        // Build full mod list.
        var allMods = reconciledPairs
            .Select(p => p.SelectedMod)
            .Concat(unmatchedServerMods)
            .Concat(unmatchedClientMods)
            .ToList();

        logger.LogDebug(
            "Reconciliation complete. Pairs: {PairCount}, Unmatched server: {UnmatchedServer}, Unmatched client: {UnmatchedClient}",
            reconciledPairs.Count,
            unmatchedServerMods.Count,
            unmatchedClientMods.Count
        );

        return new ModReconciliationResult
        {
            Mods = allMods,
            ReconciledPairs = reconciledPairs,
            UnmatchedServerMods = unmatchedServerMods,
            UnmatchedClientMods = unmatchedClientMods,
        };
    }

    /// <summary>
    /// Selects the best mod from a server/client pair based on version and completeness.
    /// </summary>
    private static (Mod SelectedMod, List<string> Notes) SelectBestMod(Mod serverMod, Mod clientMod)
    {
        List<string> notes = [];

        if (!string.Equals(serverMod.Local.Guid, clientMod.Local.Guid, StringComparison.OrdinalIgnoreCase))
        {
            notes.Add($"GUID mismatch: server '{serverMod.Local.Guid}' vs client '{clientMod.Local.Guid}'");
        }

        var serverVersionResult = SemVer.TryParse(serverMod.Local.LocalVersion, "ModReconciliation_Server");
        var clientVersionResult = SemVer.TryParse(clientMod.Local.LocalVersion, "ModReconciliation_Client");

        var hasServerVer = serverVersionResult.TryPickT0(out var serverVersion, out _);
        var hasClientVer = clientVersionResult.TryPickT0(out var clientVersion, out _);

        if (hasServerVer && hasClientVer)
        {
            if (serverVersion != clientVersion)
            {
                notes.Add(
                    $"Version mismatch: server '{serverMod.Local.LocalVersion}' vs client '{clientMod.Local.LocalVersion}'"
                );
            }

            // Select the mod with the higher version
            if (clientVersion > serverVersion)
            {
                return (clientMod, notes);
            }

            if (serverVersion > clientVersion)
            {
                return (serverMod, notes);
            }
        }
        else if (hasClientVer && !hasServerVer)
        {
            notes.Add($"Server mod has invalid version: '{serverMod.Local.LocalVersion}'");
            return (clientMod, notes);
        }
        else if (hasServerVer && !hasClientVer)
        {
            notes.Add($"Client mod has invalid version: '{clientMod.Local.LocalVersion}'");
            return (serverMod, notes);
        }

        // Versions are equal or both invalid - prefer server mod
        return (serverMod, notes);
    }
}
