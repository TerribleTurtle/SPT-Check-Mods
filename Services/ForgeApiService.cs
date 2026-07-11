using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text.Json;
using System.Text.RegularExpressions;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;

namespace CheckModsExtended.Services;

/// <summary>
/// Service for interacting with the Forge API with rate limiting. Handles mod searching, version validation, and data
/// retrieval.
/// </summary>
/// <remarks>
/// Registered manually in ServiceCollectionExtensions via AddHttpClient, not via [Injectable].
/// </remarks>
public sealed partial class ForgeApiService(
    IForgeApiClient apiClient,
    IOptions<ForgeApiOptions> options,
    ILogger<ForgeApiService> logger
) : IForgeApiService
{
    private readonly ForgeApiOptions _options = options.Value;

    /// <summary>
    /// Maximum number of mods sent in a single batch updates request.
    /// </summary>
    private const int MaxModsPerUpdateRequest = 50;

    private static readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    /// <summary>
    /// A regular expression to convert camelCase strings to space-separated words.
    /// </summary>
    [GeneratedRegex(@"(?<!^)(?<![\p{Lu}])(?=[\p{Lu}])|(?<=[\p{Ll}])(?=[\p{Lu}])")]
    private static partial Regex ConvertCamelCaseRegex();

    /// <summary>
    /// Converts camelCase strings to space-separated words. Handles special cases like
    /// all-uppercase strings (MOAR, SPT, API).
    /// </summary>
    /// <param name="input">The camelCase string to convert.</param>
    private static string ConvertCamelCaseToSpaces(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }

        // Handle special cases where the entire string is uppercase (like "MOAR")
        if (input.All(c => !char.IsLetter(c) || char.IsUpper(c)))
        {
            return input;
        }

        return ConvertCamelCaseRegex().Replace(input, " ").Trim();
    }

    /// <inheritdoc />
    public async Task<OneOf<bool, InvalidSptVersion, ApiError>> ValidateSptVersionAsync(
        string sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Validating SPT version: {SptVersion}", sptVersion);

        var escapedVersion = Uri.EscapeDataString(sptVersion);
            var url = $"{_options.BaseUrl}spt/versions?filter[spt_version]={escapedVersion}";

            var result = await apiClient.GetFromJsonAsync(
                url,
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.SptVersionApiResponse,
                cancellationToken
            );

            if (result.TryPickT2(out var error, out _))
            {
                logger.LogError("SPT version validation failed: {Message}", error.Message);
                return error;
            }
            if (result.TryPickT1(out var notFound, out _))
            {
                logger.LogWarning("SPT version {SptVersion} not found in Forge API", sptVersion);
                return new InvalidSptVersion();
            }
            var apiResponse = result.AsT0;

            var isValid =
                apiResponse is { Success: true, Data: not null } && apiResponse.Data.Any(v => v.Version == sptVersion);

            if (!isValid)
            {
                logger.LogWarning("SPT version {SptVersion} not found in Forge API", sptVersion);
                return new InvalidSptVersion();
            }

            logger.LogDebug("SPT version {SptVersion} validated successfully", sptVersion);
            return true;
    }

    /// <inheritdoc />
    public async Task<OneOf<List<SptVersionResult>, ApiError>> GetAllSptVersionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Fetching all SPT versions from Forge API");

        var query = new QueryBuilder().Add("sort", "-version").Add("per_page", "15").ToString();
        var url = $"{_options.BaseUrl}spt/versions{query}";

            var result = await apiClient.GetFromJsonAsync(
                url,
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.SptVersionApiResponse,
                cancellationToken
            );

            if (result.TryPickT2(out var error, out _))
            {
                logger.LogError("Failed to fetch SPT versions: {Message}", error.Message);
                return error;
            }
            if (result.TryPickT1(out var notFound, out _))
            {
                return new List<SptVersionResult>();
            }
            var apiResponse = result.AsT0;

            return apiResponse is { Success: true, Data: not null } ? apiResponse.Data : [];
    }

    /// <inheritdoc />
    public async Task<OneOf<List<ModSearchResult>, ApiError>> SearchModsAsync(
        string modName,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Searching for server mod: {ModName}", modName);

        var searchQuery = ConvertCamelCaseToSpaces(modName);
        return await SearchModsInternalAsync(searchQuery, sptVersion, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OneOf<List<ModSearchResult>, ApiError>> SearchClientModsAsync(
        string modName,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Searching for client mod: {ModName}", modName);

        var searchQuery = ConvertCamelCaseToSpaces(modName);
        return await SearchModsInternalAsync(searchQuery, sptVersion, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<OneOf<ModSearchResult, NotFound, InvalidInput, ApiError>> GetModByIdAsync(
        int modId,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Getting mod by ID: {ModId}", modId);

        if (modId <= 0)
        {
            logger.LogWarning("Invalid mod ID: {ModId}", modId);
            return new InvalidInput("modId", "Mod ID must be greater than 0");
        }

        var query = new QueryBuilder().Add("include", "versions,source_code_links").ToString();
        var url = $"{_options.BaseUrl}mod/{modId}{query}";

            var result = await apiClient.GetFromJsonAsync(
                url,
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ModApiResponse,
                cancellationToken
            );

            if (result.TryPickT2(out var error, out _)) { return error; }
            if (result.TryPickT1(out var notFound, out _)) { return notFound; }
            var apiResponse = result.AsT0;

            if (apiResponse?.Success != true || apiResponse.Data is null)
            {
                return new NotFound();
            }

            return apiResponse.Data;
    }

    /// <inheritdoc />
    public async Task<OneOf<ModSearchResult, NotFound, NoCompatibleVersion, ApiError>> GetModByGuidAsync(
        string modGuid,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("Getting mod by GUID: {ModGuid}", modGuid);

        if (string.IsNullOrWhiteSpace(modGuid))
        {
            logger.LogDebug("Empty GUID provided");
            return new NotFound();
        }

        // filter[guid] is case-insensitive, so the GUID is sent verbatim.
            var url =
                $"{_options.BaseUrl}mods?filter[guid]={Uri.EscapeDataString(modGuid)}&include=versions,source_code_links";

            var res = await apiClient.GetFromJsonAsync(
                url,
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ModSearchApiResponse,
                cancellationToken
            );

            if (res.TryPickT2(out var error, out _)) { return error; }
            if (res.TryPickT1(out var notFound, out _)) { return notFound; }
            var apiResponse = res.AsT0;

            if (apiResponse is not { Success: true, Data.Count: > 0 })
            {
                return new NotFound();
            }

            var result = apiResponse.Data[0];

            // Check if any version is compatible with the requested SPT version
            if (result.Versions is not { Count: > 0 })
            {
                return result;
            }

            var hasCompatibleVersion = result.Versions.Any(v =>
                SemVer.SatisfiesRange(v.SptVersionConstraint, sptVersion)
            );

            // No published version targets the requested SPT version; return the matched mod as incompatible.
            if (!hasCompatibleVersion)
            {
                return new NoCompatibleVersion(result);
            }

            return result;
    }

    /// <summary>
    /// Internal method for searching mods with shared logic between server and client mod searches.
    /// </summary>
    /// <param name="searchQuery">The processed search query.</param>
    /// <param name="sptVersion">The SPT version to filter by.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>List of matching mod search results or an error.</returns>
    private async Task<OneOf<List<ModSearchResult>, ApiError>> SearchModsInternalAsync(
        string searchQuery,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var url =
                $"{_options.BaseUrl}mods?query={Uri.EscapeDataString(searchQuery)}&filter[spt_version]={sptVersion}&include=versions,source_code_links&per_page=50";

            var res = await apiClient.GetFromJsonAsync(
                url,
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ModSearchApiResponse,
                cancellationToken
            );

            if (res.TryPickT2(out var error, out _)) { return error; }
            // Ignore notFound (T1) and return empty list if not found
            var apiResponse = res.TryPickT1(out var _, out var _) ? null : res.AsT0;

            return apiResponse is { Success: true, Data: not null } ? apiResponse.Data : [];
    }

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

        // When the whole batch fits in one request, call it directly and return its result.
        if (modList.Count <= MaxModsPerUpdateRequest)
        {
            return await GetModUpdatesChunkAsync(modList, sptVersion, cancellationToken);
        }

        // Split larger batches into chunks and dispatch them concurrently.
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

        // Combine results across chunks.
        var safeToUpdate = new List<SafeToUpdateMod>();
        var blocked = new List<BlockedUpdateMod>();
        var upToDate = new List<UpToDateMod>();
        var incompatible = new List<IncompatibleMod>();
        var anyData = false;

        foreach (var chunkResult in chunkResults)
        {
            // A single chunk error fails the whole call.
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
            // Every chunk succeeded but none carried data: report a clean miss.
            return new NotFound();
        }

        return new ModUpdatesData(safeToUpdate, blocked, upToDate, incompatible);
    }

    /// <summary>
    /// Retrieves batch update information for a single chunk of mods.
    /// </summary>
    private async Task<OneOf<ModUpdatesData, NotFound, ApiError>> GetModUpdatesChunkAsync(
        IReadOnlyList<(int ModId, string CurrentVersion)> chunk,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken
    )
    {
        // Build mods query parameter as comma-separated "id:version" pairs
            var modsParam = string.Join(",", chunk.Select(m => $"{m.ModId}:{Uri.EscapeDataString(m.CurrentVersion)}"));

            var query = new QueryBuilder()
                .AddRaw("mods", modsParam)
                .Add("spt_version", sptVersion.ToString())
                .ToString();

            var url = $"{_options.BaseUrl}mods/updates{query}";

            var res = await apiClient.GetFromJsonAsync(
                url,
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ModUpdatesApiResponse,
                cancellationToken
            );

            if (res.TryPickT2(out var error, out _)) { return error; }
            if (res.TryPickT1(out var notFound, out _)) { return notFound; }
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

        // Build mods query parameter as comma-separated "identifier:version" pairs
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

            if (res.TryPickT2(out var error, out _)) { return error; }
            if (res.TryPickT1(out var notFound, out _)) { return notFound; }
            var apiResponse = res.AsT0;

            if (apiResponse?.Success != true || apiResponse.Data is null)
            {
                return new NotFound();
            }

            return apiResponse.Data;
    }
}

