using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using OneOf;
using SemanticVersioning;

namespace CheckModsExtended.Services.Interfaces;

/// <summary>
/// Client for retrieving batch updates and dependency information from the Forge API.
/// </summary>
public interface IModUpdateClient
{
    /// <summary>
    /// Retrieves batch update information for a set of installed mods against a specific SPT version.
    /// </summary>
    /// <param name="modUpdates">A collection of tuples containing the installed ModId and its CurrentVersion string.</param>
    /// <param name="sptVersion">The SPT version to evaluate updates against.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>
    /// Returns a <see cref="OneOf{T0, T1, T2}"/> where:
    /// - <see cref="ModUpdatesData"/> containing categorized update states for all requested mods.
    /// - <see cref="NotFound"/> if the update information could not be found.
    /// - <see cref="ApiError"/> if an error occurs while communicating with the Forge API.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await client.GetModUpdatesAsync(modUpdates, version);
    /// result.Switch(
    ///     updates => Console.WriteLine($"Found {updates.Mods.Count} updates"),
    ///     notFound => Console.WriteLine("Updates not found"),
    ///     error => Console.WriteLine($"Error: {error.Message}")
    /// );
    /// </code>
    /// </example>
    Task<OneOf<ModUpdatesData, NotFound, ApiError>> GetModUpdatesAsync(
        IEnumerable<(int ModId, string CurrentVersion)> modUpdates,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    );

    /// <summary>
    /// Retrieves the dependency tree for a collection of mod versions.
    /// </summary>
    /// <param name="modVersions">A collection of tuples containing the mod Identifier (ID or GUID) and the specific Version.</param>
    /// <param name="cancellationToken">A cancellation token that can be used by other objects or threads to receive notice of cancellation.</param>
    /// <returns>
    /// Returns a <see cref="OneOf{T0, T1, T2}"/> where:
    /// - A <see cref="List{ModDependency}"/> containing the requested dependencies.
    /// - <see cref="NotFound"/> if the dependency information could not be found.
    /// - <see cref="ApiError"/> if an error occurs while communicating with the Forge API.
    /// </returns>
    /// <example>
    /// <code>
    /// var result = await client.GetModDependenciesAsync(modVersions);
    /// result.Switch(
    ///     dependencies => Console.WriteLine($"Found {dependencies.Count} dependencies"),
    ///     notFound => Console.WriteLine("Dependencies not found"),
    ///     error => Console.WriteLine($"Error: {error.Message}")
    /// );
    /// </code>
    /// </example>
    Task<OneOf<List<ModDependency>, NotFound, ApiError>> GetModDependenciesAsync(
        IEnumerable<(string Identifier, string Version)> modVersions,
        CancellationToken cancellationToken = default
    );
}
