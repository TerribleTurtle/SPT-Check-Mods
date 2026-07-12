using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using OneOf;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Client for searching and retrieving individual mods from the Forge API.
/// </summary>
public interface IModSearchClient
{
    /// <summary>
    /// Searches for mods on the Forge API by name, optionally filtered by compatible SPT version.
    /// </summary>
    /// <param name="modName">The name or partial name of the mod to search for.</param>
    /// <param name="sptVersion">The SPT version to check compatibility against.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>
    /// Returns a <see cref="OneOf{T0, T1}"/> where:
    /// - A <see cref="List{ModSearchResult}"/> containing matching mods.
    /// - An <see cref="ApiError"/> if an API or network error occurs.
    /// </returns>
    Task<OneOf<List<ModSearchResult>, ApiError>> SearchModsAsync(
        string modName,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Searches specifically for client-side mods matching the given name and SPT version.
    /// </summary>
    /// <param name="modName">The name or partial name of the mod to search for.</param>
    /// <param name="sptVersion">The SPT version to check compatibility against.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>
    /// Returns a <see cref="OneOf{T0, T1}"/> where:
    /// - A <see cref="List{ModSearchResult}"/> containing matching client mods.
    /// - An <see cref="ApiError"/> if an API or network error occurs.
    /// </returns>
    Task<OneOf<List<ModSearchResult>, ApiError>> SearchClientModsAsync(
        string modName,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves detailed information for a specific mod by its unique ID.
    /// </summary>
    /// <param name="modId">The unique integer ID of the mod on the Forge.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>
    /// Returns a <see cref="OneOf{T0, T1, T2, T3}"/> where:
    /// - A <see cref="ModSearchResult"/> containing the mod details.
    /// - A <see cref="NotFound"/> if no mod exists with the specified ID.
    /// - An <see cref="InvalidInput"/> if the ID is invalid.
    /// - An <see cref="ApiError"/> if an API or network error occurs.
    /// </returns>
    Task<OneOf<ModSearchResult, NotFound, InvalidInput, ApiError>> GetModByIdAsync(
        int modId,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves detailed information for a specific mod by its GUID and a compatible SPT version.
    /// </summary>
    /// <param name="modGuid">The unique string GUID of the mod.</param>
    /// <param name="sptVersion">The SPT version to find a compatible mod version for.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>
    /// Returns a <see cref="OneOf{T0, T1, T2, T3}"/> where:
    /// - A <see cref="ModSearchResult"/> containing the mod details.
    /// - A <see cref="NotFound"/> if no mod exists with the specified GUID.
    /// - A <see cref="NoCompatibleVersion"/> if the mod exists but lacks a version compatible with the given SPT version.
    /// - An <see cref="ApiError"/> if an API or network error occurs.
    /// </returns>
    Task<OneOf<ModSearchResult, NotFound, NoCompatibleVersion, ApiError>> GetModByGuidAsync(
        string modGuid,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    );
}
