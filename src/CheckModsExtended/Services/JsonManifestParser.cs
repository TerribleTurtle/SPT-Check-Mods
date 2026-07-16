using System.IO;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public class JsonManifestParser(IFileSystem fileSystem) : IJsonManifestParser
{
    public async Task<ServerModManifestInfo?> ParseServerModManifestAsync(string packagePath, CancellationToken cancellationToken = default)
    {
        using var packageStream = fileSystem.OpenRead(packagePath);
        using var packageDocument = await JsonDocument.ParseAsync(
            packageStream,
            cancellationToken: cancellationToken
        );
        var root = packageDocument.RootElement;

        var name = GetStringPropertyFromJson(root, "name");
        var author = GetStringPropertyFromJson(root, "author");
        var version = GetStringPropertyFromJson(root, "version");
        var sptVersion = GetStringPropertyFromJson(root, "sptVersion") ?? GetStringPropertyFromJson(root, "akiVersion");
        var url = GetStringPropertyFromJson(root, "url");

        return new ServerModManifestInfo(name, author, version, sptVersion, url);
    }

    private static string? GetStringPropertyFromJson(JsonElement root, string propertyName)
    {
        if (!root.TryGetProperty(propertyName, out var value))
        {
            return null;
        }

        return value.ValueKind switch
        {
            JsonValueKind.String => value.GetString(),
            JsonValueKind.Number => value.GetRawText(),
            _ => null,
        };
    }
}

