using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

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

        // Pass 1: Exact GUID pairs first using Join and Zip for duplicates.
        var guidPairs = clientMods
            .Where(c => !string.IsNullOrWhiteSpace(c.Local.Guid))
            .GroupBy(c => c.Local.Guid, StringComparer.OrdinalIgnoreCase)
            .Join(
                serverMods
                    .Where(s => !string.IsNullOrWhiteSpace(s.Local.Guid))
                    .GroupBy(s => s.Local.Guid, StringComparer.OrdinalIgnoreCase),
                cg => cg.Key,
                sg => sg.Key,
                (cg, sg) => cg.Zip(sg, (c, s) => new { Client = c, Server = s }),
                StringComparer.OrdinalIgnoreCase
            )
            .SelectMany(pairs => pairs)
            .ToList();

        var matchedClients = guidPairs.Select(p => p.Client).ToHashSet();
        var matchedServers = guidPairs.Select(p => p.Server).ToHashSet();

        var remainingClients = clientMods.Where(c => !matchedClients.Contains(c)).ToList();
        var remainingServers = serverMods.Where(s => !matchedServers.Contains(s)).ToList();

        // Pass 2: Pair the rest by any normalized name match using ToLookup.
        var serverByName = remainingServers
            .SelectMany(s => GetMatchableNames(s).Select(name => (Name: name, Mod: s)))
            .ToLookup(x => x.Name, x => x.Mod, StringComparer.Ordinal);

        var namePairs = new List<(Mod Client, Mod Server)>();
        var usedServers = new HashSet<Mod>();

        foreach (var client in remainingClients)
        {
            var matchedServer = GetMatchableNames(client)
                .SelectMany(name => serverByName[name])
                .FirstOrDefault(s => !usedServers.Contains(s));

            if (matchedServer != null)
            {
                namePairs.Add((client, matchedServer));
                usedServers.Add(matchedServer);
            }
        }

        matchedClients.UnionWith(namePairs.Select(p => p.Client));
        matchedServers.UnionWith(namePairs.Select(p => p.Server));

        var allPairs = guidPairs.Select(p => (p.Client, p.Server)).Concat(namePairs).ToList();

        var reconciledPairs = allPairs
            .Select(p =>
            {
                var updatedServer = p.Server with
                {
                    Local = p.Server.Local with { PairedComponentPath = p.Client.Local.FilePath },
                };
                var updatedClient = p.Client with
                {
                    Local = p.Client.Local with { PairedComponentPath = p.Server.Local.FilePath },
                };

                var (selectedMod, notes) = SelectBestMod(updatedServer, updatedClient);

                return new ModPair
                {
                    ServerMod = updatedServer,
                    ClientMod = updatedClient,
                    SelectedMod = selectedMod,
                    Notes = notes,
                };
            })
            .ToList();

        var unmatchedClientMods = clientMods.Where(c => !matchedClients.Contains(c)).ToList();
        var unmatchedServerMods = serverMods.Where(s => !matchedServers.Contains(s)).ToList();

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

    private static IEnumerable<string> GetMatchableNames(Mod mod)
    {
        var normName = ModNameNormalizer.Normalize(mod.Local.LocalName, removeComponentSuffixes: true);
        if (!string.IsNullOrEmpty(normName))
        {
            yield return normName;
        }

        var guidName = ModNameNormalizer.ExtractNameFromGuid(mod.Local.Guid);
        if (!string.IsNullOrEmpty(guidName))
        {
            var normGuidName = ModNameNormalizer.Normalize(guidName, removeComponentSuffixes: true);
            if (!string.IsNullOrEmpty(normGuidName) && normGuidName != normName)
            {
                yield return normGuidName;
            }
        }
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

        if (hasClientVer && !hasServerVer)
        {
            notes.Add($"Server mod has invalid version: '{serverMod.Local.LocalVersion}'");
            return (clientMod, notes);
        }

        if (hasServerVer && !hasClientVer)
        {
            notes.Add($"Client mod has invalid version: '{clientMod.Local.LocalVersion}'");
            return (serverMod, notes);
        }

        if (hasServerVer && hasClientVer)
        {
            if (serverVersion != clientVersion)
            {
                notes.Add(
                    $"Version mismatch: server '{serverMod.Local.LocalVersion}' vs client '{clientMod.Local.LocalVersion}'"
                );
            }

            if (clientVersion > serverVersion)
            {
                return (clientMod, notes);
            }

            if (serverVersion > clientVersion)
            {
                return (serverMod, notes);
            }
        }

        // Versions are equal or both invalid - prefer server mod
        return (serverMod, notes);
    }
}
