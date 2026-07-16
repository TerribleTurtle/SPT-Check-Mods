using System.Threading;
using System.Threading.Tasks;

namespace CheckModsExtended.Services.Interfaces;

public record ServerModManifestInfo(
    string? Name,
    string? Author,
    string? Version,
    string? SptVersion,
    string? Url
);

public interface IJsonManifestParser
{
    Task<ServerModManifestInfo?> ParseServerModManifestAsync(string packagePath, CancellationToken cancellationToken = default);
}

