using CheckModsExtended.Models;
using OneOf;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;

namespace CheckModsExtended.Services.Interfaces;

public interface IForgeSptVersions
{
    Task<OneOf<bool, InvalidSptVersion, ApiError>> ValidateSptVersionAsync(string sptVersion, CancellationToken cancellationToken = default);
    Task<OneOf<List<SptVersionResult>, ApiError>> GetAllSptVersionsAsync(CancellationToken cancellationToken = default);
}

public interface IForgeSearch
{
    Task<OneOf<List<ModSearchResult>, ApiError>> SearchModsAsync(string modName, SemanticVersioning.Version sptVersion, CancellationToken cancellationToken = default);
    Task<OneOf<List<ModSearchResult>, ApiError>> SearchClientModsAsync(string modName, SemanticVersioning.Version sptVersion, CancellationToken cancellationToken = default);
}

public interface IForgeModRetrieval
{
    Task<OneOf<ModSearchResult, NotFound, InvalidInput, ApiError>> GetModByIdAsync(int modId, CancellationToken cancellationToken = default);
    Task<OneOf<ModSearchResult, NotFound, NoCompatibleVersion, ApiError>> GetModByGuidAsync(string modGuid, SemanticVersioning.Version sptVersion, CancellationToken cancellationToken = default);
}

public interface IForgeUpdates
{
    Task<OneOf<ModUpdatesData, NotFound, ApiError>> GetModUpdatesAsync(IEnumerable<(int ModId, string CurrentVersion)> modUpdates, SemanticVersioning.Version sptVersion, CancellationToken cancellationToken = default);
}

public interface IForgeDependencies
{
    Task<OneOf<List<ModDependency>, NotFound, ApiError>> GetModDependenciesAsync(IEnumerable<(string Identifier, string Version)> modVersions, CancellationToken cancellationToken = default);
}

/// <summary>
/// Interacts with the Forge API to search mods, validate versions, and retrieve mod data.
/// </summary>
public interface IForgeApiService : IForgeSptVersions, IForgeSearch, IForgeModRetrieval, IForgeUpdates, IForgeDependencies
{
}
