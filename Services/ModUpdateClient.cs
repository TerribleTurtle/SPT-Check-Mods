using System.Net.Http.Json;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;

namespace CheckModsExtended.Services;

/// <summary>
/// Service for interacting with mod updates and dependencies endpoints on the Forge API.
/// </summary>
public sealed class ModUpdateClient(
    IForgeApiClient apiClient,
    IOptions<ForgeApiOptions> options,
    ILogger<ModUpdateClient> logger
) : IModUpdateClient
{
    private readonly ForgeApiOptions _options = options.Value;
    private const int MaxModsPerUpdateRequest = 50;

    /// <inheritdoc />
    public async Task<OneOf<ModUpdatesData, NotFound, ApiError>> GetModUpdatesAsync(
        IEnumerable<(int ModId, string CurrentVersion)> modUpdates,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var modList = modUpdates.ToList();
        if (modList.Count == 0)
        {
            return new NotFound();
        }

        if (modList.Count <= MaxModsPerUpdateRequest)
        {
            return await GetModUpdatesChunkAsync(modList, sptVersion, cancellationToken);
        }

        var chunks = modList.Chunk(MaxModsPerUpdateRequest).ToArray();
        var chunkResults = new OneOf<ModUpdatesData, NotFound, ApiError>[chunks.Length];

        await Parallel.ForEachAsync(
            chunks.Select((chunk, index) => (chunk, index)),
            new ParallelOptions { CancellationToken = cancellationToken },
            async (item, ct) =>
            {
                chunkResults[item.index] = await GetModUpdatesChunkAsync(item.chunk, sptVersion, ct);
            }
        );

        var safeToUpdate = new List<SafeToUpdateMod>();
        var blocked = new List<BlockedUpdateMod>();
        var upToDate = new List<UpToDateMod>();
        var incompatible = new List<IncompatibleMod>();
        var anyData = false;

        foreach (var chunkResult in chunkResults)
        {
            if (chunkResult.TryPickT2(out var error, out _))
            {
                logger.LogDebug(
                    "A mod-updates chunk failed ({Error}); failing the batch update request",
                    error.Message
                );
                return error;
            }

            if (!chunkResult.TryPickT0(out var data, out _))
            {
                continue;
            }

            anyData = true;
            if (data.SafeToUpdate is not null)
            {
                safeToUpdate.AddRange(data.SafeToUpdate);
            }
            if (data.Blocked is not null)
            {
                blocked.AddRange(data.Blocked);
            }
            if (data.UpToDate is not null)
            {
                upToDate.AddRange(data.UpToDate);
            }
            if (data.Incompatible is not null)
            {
                incompatible.AddRange(data.Incompatible);
            }
        }

        if (!anyData)
        {
            return new NotFound();
        }

        return new ModUpdatesData(safeToUpdate, blocked, upToDate, incompatible);
    }

    private async Task<OneOf<ModUpdatesData, NotFound, ApiError>> GetModUpdatesChunkAsync(
        IReadOnlyList<(int ModId, string CurrentVersion)> chunk,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken
    )
    {
        var modsParam = string.Join(",", chunk.Select(m => $"{m.ModId}:{Uri.EscapeDataString(m.CurrentVersion)}"));

        var query = new QueryBuilder().AddRaw("mods", modsParam).Add("spt_version", sptVersion.ToString()).ToString();

        var url = $"{_options.BaseUrl}mods/updates{query}";

        var res = await apiClient.GetFromJsonAsync(
            url,
            CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ModUpdatesApiResponse,
            cancellationToken
        );

        if (res.TryPickT2(out var error, out _))
        {
            return error;
        }
        if (res.TryPickT1(out var notFound, out _))
        {
            return notFound;
        }
        var apiResponse = res.AsT0;

        if (apiResponse?.Success != true || apiResponse.Data is null)
        {
            return new NotFound();
        }

        return apiResponse.Data;
    }

    /// <inheritdoc />
    public async Task<OneOf<List<ModDependency>, NotFound, ApiError>> GetModDependenciesAsync(
        IEnumerable<(string Identifier, string Version)> modVersions,
        CancellationToken cancellationToken = default
    )
    {
        var modList = modVersions.ToList();
        if (modList.Count == 0)
        {
            return new NotFound();
        }

        var modsParam = string.Join(
            ",",
            modList.Select(m => $"{Uri.EscapeDataString(m.Identifier)}:{Uri.EscapeDataString(m.Version)}")
        );

        var query = new QueryBuilder().AddRaw("mods", modsParam).ToString();
        var url = $"{_options.BaseUrl}mods/dependencies{query}";

        var res = await apiClient.GetFromJsonAsync(
            url,
            CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ModDependenciesApiResponse,
            cancellationToken
        );

        if (res.TryPickT2(out var error, out _))
        {
            return error;
        }
        if (res.TryPickT1(out var notFound, out _))
        {
            return notFound;
        }
        var apiResponse = res.AsT0;

        if (apiResponse?.Success != true || apiResponse.Data is null)
        {
            return new NotFound();
        }

        return apiResponse.Data;
    }
}
