using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Utils;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public sealed class ServerModExtractor(
    ILogger<ServerModExtractor> logger,
    IModCheckReporter reporter,
    IBinaryParser binaryParser,
    IJsonManifestParser jsonManifestParser,
    CheckModsExtended.Utils.IFileSystem fileSystem
) : IServerModExtractor
{
    /// <inheritdoc />
    public Task<Mod?> ExtractServerModMetadataAsync(
        string dllPath,
        string sptDirectory,
        CancellationToken cancellationToken = default
    )
    {
        // Extraction is CPU-bound and synchronous, so we run it in a task to avoid blocking the calling thread.
        return Task.Run(() => ExtractServerModMetadata(dllPath, sptDirectory), cancellationToken);
    }

    private Mod? ExtractServerModMetadata(string dllPath, string sptDirectory)
    {
        try
        {
            var metadata = binaryParser.ExtractServerModMetadata(dllPath);

            if (metadata is null || string.IsNullOrEmpty(metadata.Guid))
            {
                return null;
            }

            var warnings = ModMetadataValidator.ValidateModMetadata(
                metadata.Name ?? string.Empty,
                metadata.Author ?? string.Empty,
                metadata.Version ?? string.Empty,
                metadata.Guid
            );

            return new Mod
            {
                Local = new LocalModIdentity
                {
                    Guid = metadata.Guid,
                    FilePath = dllPath,
                    IsServerMod = true,
                    LocalName = metadata.Name ?? string.Empty,
                    LocalAuthor = metadata.Author ?? string.Empty,
                    LocalVersion = metadata.Version ?? string.Empty,
                    LocalSptVersion = metadata.SptVersion,
                    Url = metadata.Url,
                },
                LoadWarnings = warnings,
            };
        }
        catch (Exception ex)
            when (ex
                    is IOException
                        or UnauthorizedAccessException
                        or System.Security.SecurityException
                        or BadImageFormatException
                        or FileLoadException
            )
        {
            logger.LogDebug(ex, "Could not inspect DLL as a server mod: {Path}", dllPath);
            reporter.CouldNotReadModDll(Path.GetFileName(dllPath), ex.Message);
            return null;
        }
    }

    /// <inheritdoc />
    public async Task<Mod?> ExtractServerModPackageMetadataAsync(
        string modDirectory,
        CancellationToken cancellationToken = default
    )
    {
        var packagePath = Path.Combine(modDirectory, "package.json");
        if (!fileSystem.FileExists(packagePath))
        {
            return null;
        }

        try
        {
            var manifest = await jsonManifestParser.ParseServerModManifestAsync(packagePath, cancellationToken);
            var directoryName = Path.GetFileName(modDirectory);
            var name = manifest?.Name ?? directoryName;
            var author = manifest?.Author ?? "Unknown";
            var version = manifest?.Version ?? string.Empty;
            var sptVersion = manifest?.SptVersion;

            var warnings = ModMetadataValidator.ValidateModMetadata(name, author, version, name);

            return new Mod
            {
                Local = new LocalModIdentity
                {
                    Guid = name,
                    FilePath = packagePath,
                    IsServerMod = true,
                    LocalName = name,
                    LocalAuthor = author,
                    LocalVersion = version,
                    LocalSptVersion = sptVersion,
                    Url = manifest?.Url,
                },
                LoadWarnings = warnings,
            };
        }
        catch (Exception ex) when (ex is System.Text.Json.JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Could not parse package.json as a server mod: {Path}", packagePath);
            reporter.CouldNotReadModDll(Path.GetFileName(packagePath), ex.Message);
            return null;
        }
    }
}

