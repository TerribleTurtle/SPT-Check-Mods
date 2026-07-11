using CheckMods.Models;
using CheckMods.Services.Interfaces;
using OneOf;
using Version = SemanticVersioning.Version;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IForgeApiService"/>.
/// </summary>
public sealed class FakeForgeApiService : IForgeApiService
{
    /// <summary> Gets or sets ValidateSptVersionResult. </summary>
    public OneOf<bool, InvalidSptVersion, ApiError> ValidateSptVersionResult { get; set; } = true;
    /// <summary> Gets or sets GetAllSptVersionsResult. </summary>
    public OneOf<List<SptVersionResult>, ApiError> GetAllSptVersionsResult { get; set; } = new List<SptVersionResult>();
    /// <summary> Gets or sets SearchModsResult. </summary>
    public OneOf<List<ModSearchResult>, ApiError> SearchModsResult { get; set; } = new List<ModSearchResult>();
    /// <summary> Gets or sets GetModByIdResult. </summary>
    public OneOf<ModSearchResult, NotFound, InvalidInput, ApiError> GetModByIdResult { get; set; } = new NotFound();
    /// <summary> Gets or sets GetModByGuidResult. </summary>
    public OneOf<ModSearchResult, NotFound, NoCompatibleVersion, ApiError> GetModByGuidResult { get; set; } = new NotFound();
    /// <summary> Gets or sets GetModUpdatesResult. </summary>
    public OneOf<ModUpdatesData, NotFound, ApiError> GetModUpdatesResult { get; set; } = new NotFound();
    /// <summary> Gets or sets GetModDependenciesResult. </summary>
    public OneOf<List<ModDependency>, NotFound, ApiError> GetModDependenciesResult { get; set; } = new NotFound();

    /// <inheritdoc />
    public Task<OneOf<bool, InvalidSptVersion, ApiError>> ValidateSptVersionAsync(string sptVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(ValidateSptVersionResult);
    }

    /// <inheritdoc />
    public Task<OneOf<List<SptVersionResult>, ApiError>> GetAllSptVersionsAsync(CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetAllSptVersionsResult);
    }

    /// <inheritdoc />
    public Task<OneOf<List<ModSearchResult>, ApiError>> SearchModsAsync(string modName, Version sptVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SearchModsResult);
    }

    /// <inheritdoc />
    public Task<OneOf<List<ModSearchResult>, ApiError>> SearchClientModsAsync(string modName, Version sptVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(SearchModsResult);
    }

    /// <inheritdoc />
    public Task<OneOf<ModSearchResult, NotFound, InvalidInput, ApiError>> GetModByIdAsync(int modId, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetModByIdResult);
    }

    /// <inheritdoc />
    public Task<OneOf<ModSearchResult, NotFound, NoCompatibleVersion, ApiError>> GetModByGuidAsync(string modGuid, Version sptVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetModByGuidResult);
    }

    /// <inheritdoc />
    public Task<OneOf<ModUpdatesData, NotFound, ApiError>> GetModUpdatesAsync(IEnumerable<(int ModId, string CurrentVersion)> modUpdates, Version sptVersion, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetModUpdatesResult);
    }

    /// <inheritdoc />
    public Task<OneOf<List<ModDependency>, NotFound, ApiError>> GetModDependenciesAsync(IEnumerable<(string Identifier, string Version)> modVersions, CancellationToken cancellationToken = default)
    {
        cancellationToken.ThrowIfCancellationRequested();
        return Task.FromResult(GetModDependenciesResult);
    }
}






