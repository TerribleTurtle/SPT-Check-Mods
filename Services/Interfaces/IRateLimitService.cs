namespace CheckMods.Services.Interfaces;

/// <summary>
/// Proactively paces API calls using a token bucket.
/// </summary>
public interface IRateLimitService
{
    /// <summary>
    /// Executes an HTTP request with proactive rate limiting and retries on 429 / transient errors.
    /// </summary>
    /// <param name="requestFunc">Function that executes the HTTP request.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The HTTP response.</returns>
    Task<HttpResponseMessage> ExecuteWithRetryAsync(
        Func<Task<HttpResponseMessage>> requestFunc,
        CancellationToken cancellationToken = default
    );
}
