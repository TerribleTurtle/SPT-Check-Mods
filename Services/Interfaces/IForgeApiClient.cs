using System.Net;
using System.Threading;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using OneOf;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Handles HTTP transport logic for the Forge API.
/// </summary>
public interface IForgeApiClient
{
    /// <summary>
    /// Issues a GET request and returns its status code and body.
    /// </summary>
    Task<HttpResponseMessage> GetJsonAsync(
        string url,
        CancellationToken cancellationToken = default
    );

    Task<OneOf<T, NotFound, ApiError>> GetFromJsonAsync<T>(
        string url,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default
    );
}

