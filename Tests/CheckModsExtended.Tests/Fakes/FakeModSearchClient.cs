using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using OneOf;
using SemanticVersioning;

namespace CheckModsExtended.Tests.Fakes;

internal sealed class FakeModSearchClient : IModSearchClient
{
    public Func<string, OneOf<ModSearchResult, NotFound, NoCompatibleVersion, ApiError>>? OnGetModByGuid { get; set; }
    public Func<string, OneOf<List<ModSearchResult>, ApiError>>? OnSearch { get; set; }
    public Func<int, OneOf<ModSearchResult, NotFound, InvalidInput, ApiError>>? OnGetModById { get; set; }

    public Task<OneOf<List<ModSearchResult>, ApiError>> SearchModsAsync(
        string modName,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        OneOf<List<ModSearchResult>, ApiError> result = OnSearch is not null
            ? OnSearch(modName)
            : new List<ModSearchResult>();
        return Task.FromResult(result);
    }

    public Task<OneOf<List<ModSearchResult>, ApiError>> SearchClientModsAsync(
        string modName,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        return SearchModsAsync(modName, sptVersion, cancellationToken);
    }

    public Task<OneOf<ModSearchResult, NotFound, InvalidInput, ApiError>> GetModByIdAsync(
        int modId,
        CancellationToken cancellationToken = default
    )
    {
        if (OnGetModById is null)
        {
            throw new NotSupportedException();
        }

        return Task.FromResult(OnGetModById(modId));
    }

    public Task<OneOf<ModSearchResult, NotFound, NoCompatibleVersion, ApiError>> GetModByGuidAsync(
        string modGuid,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        cancellationToken.ThrowIfCancellationRequested();
        OneOf<ModSearchResult, NotFound, NoCompatibleVersion, ApiError> result = OnGetModByGuid is not null
            ? OnGetModByGuid(modGuid)
            : new NotFound();
        return Task.FromResult(result);
    }
}
