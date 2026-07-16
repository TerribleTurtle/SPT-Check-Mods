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
/// Service for interacting with SPT version endpoints on the Forge API.
/// </summary>
public sealed class SptVersionClient(
    IForgeApiClient apiClient,
    IOptions<ForgeApiOptions> options,
    ILogger<SptVersionClient> logger
) : ISptVersionClient
{
    private readonly ForgeApiOptions _options = options.Value;

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
}
