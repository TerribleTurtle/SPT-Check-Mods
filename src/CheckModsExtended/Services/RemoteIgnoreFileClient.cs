using System.Text.Json;
using System.Text.Json.Serialization;
using CheckModsExtended.Configuration;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Options;

namespace CheckModsExtended.Services;

/// <summary>
/// HTTP client for the author-maintained remote base ignore list. Registered via AddHttpClient; carries no [Injectable]
/// attribute.
/// </summary>
public sealed class RemoteIgnoreFileClient(
    HttpClient httpClient,
    IOptions<IgnoredUpdateOptions> options,
    ILogger<RemoteIgnoreFileClient> logger
) : IRemoteIgnoreFileClient
{
    private readonly IgnoredUpdateOptions _options = options.Value;

    private static readonly JsonSerializerOptions _jsonOptions = new()
    {
        PropertyNameCaseInsensitive = true,
        TypeInfoResolver = CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default,
    };

    /// <inheritdoc />
    public bool IsConfigured
    {
        get { return !string.IsNullOrWhiteSpace(_options.RemoteUrl); }
    }

    /// <inheritdoc />
    public async Task<IReadOnlyList<IgnoredUpdate>?> FetchAsync(CancellationToken cancellationToken = default)
    {
        if (!IsConfigured)
        {
            return null;
        }

        try
        {
            logger.LogDebug("Fetching remote ignore list: GET {Url}", _options.RemoteUrl);
            using var response = await httpClient.GetAsync(
                _options.RemoteUrl,
                HttpCompletionOption.ResponseHeadersRead,
                cancellationToken
            );
            if (!response.IsSuccessStatusCode)
            {
                logger.LogInformation("Remote ignore list returned {Status}", response.StatusCode);
                return null;
            }

            await using var stream = await response.Content.ReadAsStreamAsync(cancellationToken);
            var file = await JsonSerializer.DeserializeAsync(
                stream,
                CheckModsExtended.Configuration.CheckModsExtendedJsonSerializerContext.Default.IgnoredUpdatesFile,
                cancellationToken
            );
            if (file?.Ignored is null)
            {
                return null;
            }

            if (file.SchemaVersion > IgnoredUpdatesFile.CurrentSchemaVersion)
            {
                logger.LogWarning(
                    "Remote ignore list schema v{Remote} is newer than supported v{Supported}; skipping",
                    file.SchemaVersion,
                    IgnoredUpdatesFile.CurrentSchemaVersion
                );
                return null;
            }

            return file.Ignored.Where(e => e.IsWellFormed).ToList();
        }
        catch (OperationCanceledException) when (cancellationToken.IsCancellationRequested)
        {
            throw;
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Could not fetch the remote ignore list");
            return null;
        }
    }
}
