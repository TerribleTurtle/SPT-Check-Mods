using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using OneOf;

namespace CheckModsExtended.Tests.Fakes;

internal sealed class FakeSptVersionClient : ISptVersionClient
{
    public Func<string, OneOf<bool, InvalidSptVersion, ApiError>>? OnValidateSptVersion { get; set; }
    public Func<OneOf<List<SptVersionResult>, ApiError>>? OnGetAllSptVersions { get; set; }

    public Task<OneOf<bool, InvalidSptVersion, ApiError>> ValidateSptVersionAsync(
        string sptVersion,
        CancellationToken cancellationToken = default
    )
    {
        if (OnValidateSptVersion is null)
        {
            throw new NotSupportedException();
        }
        return Task.FromResult(OnValidateSptVersion(sptVersion));
    }

    public Task<OneOf<List<SptVersionResult>, ApiError>> GetAllSptVersionsAsync(
        CancellationToken cancellationToken = default
    )
    {
        if (OnGetAllSptVersions is null)
        {
            throw new NotSupportedException();
        }
        return Task.FromResult(OnGetAllSptVersions());
    }
}
