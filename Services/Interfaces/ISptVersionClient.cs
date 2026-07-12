using CheckModsExtended.Models;
using OneOf;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Client for interacting with SPT version information from the Forge API.
/// </summary>
public interface ISptVersionClient
{
    /// <summary>
    /// Validates if the provided SPT version is a known and valid version.
    /// </summary>
    /// <param name="sptVersion">The SPT version string to validate (e.g., "3.8.0").</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>
    /// Returns a <see cref="OneOf{T0, T1, T2}"/> where:
    /// - <see cref="bool"/> indicates whether the version is valid (true) or not.
    /// - <see cref="InvalidSptVersion"/> if the provided SPT version format is invalid.
    /// - <see cref="ApiError"/> if an error occurs while communicating with the Forge API.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await client.ValidateSptVersionAsync("3.8.0");
    /// result.Switch(
    ///     isValid => Console.WriteLine($"Valid: {isValid}"),
    ///     invalid => Console.WriteLine($"Invalid format"),
    ///     error => Console.WriteLine($"API Error: {error.Message}")
    /// );
    /// </code>
    /// </example>
    Task<OneOf<bool, InvalidSptVersion, ApiError>> ValidateSptVersionAsync(string sptVersion, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all known SPT versions from the Forge API.
    /// </summary>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>
    /// Returns a <see cref="OneOf{T0, T1}"/> where:
    /// - A <see cref="List{SptVersionResult}"/> containing all SPT versions on success.
    /// - An <see cref="ApiError"/> if an error occurs while communicating with the Forge API.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await client.GetAllSptVersionsAsync();
    /// if (result.TryPickT0(out var versions, out _)) {
    ///     Console.WriteLine($"Found {versions.Count} versions.");
    /// }
    /// </code>
    /// </example>
    Task<OneOf<List<SptVersionResult>, ApiError>> GetAllSptVersionsAsync(CancellationToken cancellationToken = default);
}
