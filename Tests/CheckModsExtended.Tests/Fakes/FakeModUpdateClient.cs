using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using OneOf;

namespace CheckModsExtended.Tests.Fakes;

internal sealed class FakeModUpdateClient : IModUpdateClient
{
    public Func<OneOf<ModUpdatesData, NotFound, ApiError>>? OnGetModUpdates { get; set; }
    public Func<string, OneOf<List<ModDependency>, NotFound, ApiError>>? OnGetModDependencies { get; set; }
    public Func<
        (string Identifier, string Version),
        OneOf<List<ModDependency>, NotFound, ApiError>
    >? OnGetModDependenciesVersioned { get; set; }

    public Task<OneOf<ModUpdatesData, NotFound, ApiError>> GetModUpdatesAsync(
        IEnumerable<(int ModId, string CurrentVersion)> modUpdates,
        SemanticVersioning.Version sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        if (OnGetModUpdates is null)
        {
            throw new NotSupportedException();
        }

        return Task.FromResult(OnGetModUpdates());
    }

    public Task<OneOf<List<ModDependency>, NotFound, ApiError>> GetModDependenciesAsync(
        IEnumerable<(string Identifier, string Version)> modVersions,
        CancellationToken cancellationToken = default
    )
    {
        var first = modVersions.FirstOrDefault();
        var identifier = first.Identifier ?? string.Empty;
        var version = first.Version ?? string.Empty;

        if (OnGetModDependenciesVersioned is not null)
        {
            return Task.FromResult(OnGetModDependenciesVersioned((identifier, version)));
        }

        if (OnGetModDependencies is null)
        {
            throw new NotSupportedException();
        }

        return Task.FromResult(OnGetModDependencies(identifier));
    }
}
