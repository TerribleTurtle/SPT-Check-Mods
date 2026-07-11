using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Text.Json;
using System.Text.Json.Serialization.Metadata;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using OneOf;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services;

/// <summary>
/// Default implementation of <see cref="IForgeApiClient"/>.
/// </summary>
public sealed class ForgeApiClient(HttpClient httpClient, ILogger<ForgeApiClient> logger) : IForgeApiClient
{
    /// <inheritdoc />
    public async Task<(HttpStatusCode StatusCode, System.IO.Stream Body, bool IsSuccessStatusCode)> GetJsonAsync(
        string url,
        CancellationToken cancellationToken = default
    )
    {
        logger.LogDebug("API Request: GET {Url}", url);
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await httpClient.SendAsync(req, HttpCompletionOption.ResponseHeadersRead, cancellationToken);

        var statusCode = response.StatusCode;
        var body = await response.Content.ReadAsStreamAsync(cancellationToken);

        var isSuccess = (int) statusCode is >= 200 and < 300;
        return (statusCode, body, isSuccess);
    }
    public async Task<OneOf<T, NotFound, ApiError>> GetFromJsonAsync<T>(
        string url,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default
    )
    {
        try
        {
            var (statusCode, body, isSuccess) = await GetJsonAsync(url, cancellationToken);
            await using var bodyStream = body;

            if (statusCode == HttpStatusCode.NotFound)
            {
                return new NotFound();
            }

            if (!isSuccess)
            {
                logger.LogError("API request to {Url} failed: {StatusCode}", url, statusCode);
                return new ApiError($"API returned status {statusCode}", (int) statusCode);
            }

            var apiResponse = await JsonSerializer.DeserializeAsync(bodyStream, jsonTypeInfo, cancellationToken);
            if (apiResponse is null)
            {
                return new NotFound();
            }

            return apiResponse;
        }
        catch (HttpRequestException ex)
        {
            logger.LogError(ex, "Network error during request to {Url}", url);
            return new ApiError("Network error occurred", Exception: ex);
        }
        catch (JsonException ex)
        {
            logger.LogError(ex, "Failed to parse API response from {Url}", url);
            return new ApiError("Failed to parse API response", Exception: ex);
        }
    }
}