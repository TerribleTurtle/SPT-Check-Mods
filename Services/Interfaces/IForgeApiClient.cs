using System.Net;
using System.Text.Json.Serialization.Metadata;
using System.Threading;
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
    /// <param name="url">The URL to send the GET request to.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>The <see cref="HttpResponseMessage"/> containing the HTTP response.</returns>
    Task<HttpResponseMessage> GetJsonAsync(string url, CancellationToken cancellationToken = default);

    /// <summary>
    /// Issues a GET request and deserializes the JSON response into the specified type.
    /// </summary>
    /// <typeparam name="T">The type of the expected JSON response body.</typeparam>
    /// <param name="url">The URL to send the GET request to.</param>
    /// <param name="jsonTypeInfo">The JSON type information for deserialization.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>
    /// Returns a <see cref="OneOf{T0, T1, T2}"/> where:
    /// - <typeparamref name="T"/> containing the deserialized API response on success.
    /// - A <see cref="NotFound"/> if the requested resource could not be found (404) or was empty.
    /// - An <see cref="ApiError"/> if an error occurs during HTTP transport, receiving a non-success status code, or JSON parsing.
    /// </returns>
    Task<OneOf<T, NotFound, ApiError>> GetFromJsonAsync<T>(
        string url,
        JsonTypeInfo<T> jsonTypeInfo,
        CancellationToken cancellationToken = default
    );
}
