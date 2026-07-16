using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Web;
using OneOf;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Service for reading and updating application settings.
/// </summary>
public interface ISettingsService
{
    /// <summary>
    /// Gets the application settings JSON.
    /// </summary>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A JSON string of the settings, or an empty object if no settings exist.</returns>
    Task<string> GetSettingsAsync(CancellationToken token = default);

    /// <summary>
    /// Validates and updates the application settings JSON.
    /// </summary>
    /// <param name="jsonPayload">The new settings JSON string.</param>
    /// <param name="token">Cancellation token.</param>
    /// <returns>A message response on success, or an API error if validation fails.</returns>
    Task<OneOf<MessageResponse, ApiError>> UpdateSettingsAsync(string jsonPayload, CancellationToken token = default);

    /// <summary>
    /// Helper to programmatically update the ignored updates options.
    /// </summary>
    /// <param name="updateAction">Action to modify the options object.</param>
    /// <param name="token">Cancellation token.</param>
    Task<OneOf<MessageResponse, ApiError>> UpdateIgnoredUpdateOptionsAsync(
        System.Action<CheckModsExtended.Configuration.IgnoredUpdateOptions> updateAction,
        CancellationToken token = default
    );
}
