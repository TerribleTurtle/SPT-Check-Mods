using System.Net.Http.Json;
using System.Text.RegularExpressions;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;
using OneOf;
using SemanticVersioning;

namespace CheckModsExtended.Services;

/// <summary>
/// Service for interacting with mod search endpoints on the Forge API.
/// </summary>
public sealed partial class ModSearchClient(
    IForgeApiClient apiClient,
    IOptions<ForgeApiOptions> options,
    ILogger<ModSearchClient> logger
) : IModSearchClient
{
    private readonly ForgeApiOptions _options = options.Value;

    [GeneratedRegex(@"(?<!^)(?<![\p{Lu}])(?=[\p{Lu}])|(?<=[\p{Ll}])(?=[\p{Lu}])")]
    private static partial Regex ConvertCamelCaseRegex();

    private static string ConvertCamelCaseToSpaces(string input)
    {
        if (string.IsNullOrWhiteSpace(input))
        {
            return input;
        }
        if (input.All(c => !char.IsLetter(c) || char.IsUpper(c)))
        {
            return input;
        }
        return ConvertCamelCaseRegex().Replace(input, " ").Trim();
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

        var query = new CheckModsExtended.Utils.QueryBuilder().Add("include", "versions,source_code_links").ToString();
        var url = $"{_options.BaseUrl}mod/{modId}{query}";

        var result = await apiClient.GetFromJsonAsync(
            url,
            CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ModApiResponse,
            cancellationToken
        );

        if (result.TryPickT2(out var error, out _))
        {
            return error;
        }
        if (result.TryPickT1(out var notFound, out _))
        {
            return notFound;
        }
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

        var url =
            $"{_options.BaseUrl}mods?filter[guid]={Uri.EscapeDataString(modGuid)}&include=versions,source_code_links";

        var res = await apiClient.GetFromJsonAsync(
            url,
            CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ModSearchApiResponse,
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

        if (apiResponse is not { Success: true, Data.Count: > 0 })
        {
            return new NotFound();
        }

        var result = apiResponse.Data[0];

        if (result.Versions is not { Count: > 0 })
        {
            return result;
        }

        var hasCompatibleVersion = result.Versions.Any(v =>
            CheckModsExtended.Utils.SemVer.SatisfiesRange(v.SptVersionConstraint, sptVersion)
        );

        if (!hasCompatibleVersion)
        {
            return new NoCompatibleVersion(result);
        }

        return result;
    }

    private async Task<OneOf<List<ModSearchResult>, ApiError>> SearchModsInternalAsync(
        string searchQuery,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        var url =
            $"{_options.BaseUrl}mods?query={Uri.EscapeDataString(searchQuery)}&filter[spt_version]={Uri.EscapeDataString(sptVersion.ToString())}&include=versions,source_code_links&per_page=50";

        var res = await apiClient.GetFromJsonAsync(
            url,
            CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.ModSearchApiResponse,
            cancellationToken
        );

        if (res.TryPickT2(out var error, out _))
        {
            return error;
        }
        var apiResponse = res.TryPickT1(out var _, out var _) ? null : res.AsT0;

        return apiResponse is { Success: true, Data: not null } ? apiResponse.Data : [];
    }
}
