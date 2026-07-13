using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public sealed class ModLinkResolverService : IModLinkResolverService
{
    public string? ResolveUpToDateLink(Mod mod, UpToDateMod upToDate)
    {
        return mod.Api.ApiVersions?.FirstOrDefault(v => v.Version == upToDate.Version)?.Link
                ?? (mod.Api.ApiVersions?.Count > 0 ? mod.Api.ApiVersions[0].Link : null);
    }

    public string? ResolveIncompatibleLink(Mod mod, IncompatibleMod incompatible)
    {
        return incompatible.LatestCompatibleVersion?.Link 
                ?? (mod.Api.ApiVersions?.Count > 0 ? mod.Api.ApiVersions[0].Link : null);
    }
}
