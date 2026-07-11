using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using Microsoft.Extensions.Logging;

namespace CheckModsExtended.Services;

/// <summary>
/// Default implementation of <see cref="IForgeApiClient"/>.
/// </summary>
public sealed class ForgeApiClient(
    HttpClient httpClient,
    ILogger<ForgeApiClient> logger
) : IForgeApiClient
{
    /// <inheritdoc />
    public async Task<(HttpStatusCode StatusCode, string Body, bool IsSuccessStatusCode)> GetJsonAsync(string url, CancellationToken cancellationToken = default)
    {
        logger.LogDebug("API Request: GET {Url}", url);
        var req = new HttpRequestMessage(HttpMethod.Get, url);
        req.Headers.Accept.Add(new MediaTypeWithQualityHeaderValue("application/json"));
        var response = await httpClient.SendAsync(req, cancellationToken);

        var statusCode = response.StatusCode;
        var body = await response.Content.ReadAsStringAsync(cancellationToken);
        response.Dispose();

        var isSuccess = (int) statusCode is >= 200 and < 300;
        return (statusCode, body, isSuccess);
    }
}
