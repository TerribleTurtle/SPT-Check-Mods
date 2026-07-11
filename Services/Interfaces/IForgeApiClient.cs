using System.Net;
using System.Threading;
using System.Threading.Tasks;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Handles HTTP transport logic for the Forge API.
/// </summary>
public interface IForgeApiClient
{
    /// <summary>
    /// Issues a GET request and returns its status code and body. 
    /// </summary>
    Task<(HttpStatusCode StatusCode, string Body, bool IsSuccessStatusCode)> GetJsonAsync(string url, CancellationToken cancellationToken = default);
}
