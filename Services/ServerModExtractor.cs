using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Microsoft.Extensions.Logging;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
[UnconditionalSuppressMessage(
    "Trimming",
    "IL2026",
    Justification = "We are inspecting dynamically loaded mod assemblies, not application code."
)]
[UnconditionalSuppressMessage(
    "Trimming",
    "IL2072",
    Justification = "We are inspecting dynamically loaded mod assemblies, not application code."
)]
[UnconditionalSuppressMessage(
    "Trimming",
    "IL2075",
    Justification = "We are inspecting dynamically loaded mod assemblies, not application code."
)]
public sealed class ServerModExtractor(ILogger<ServerModExtractor> logger, CheckModsExtended.Utils.IFileSystem fileSystem) : IServerModExtractor
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
            using var stream = fileSystem.Open(dllPath, FileMode.Open, FileAccess.Read, FileShare.Read);
            using var module = Mono.Cecil.ModuleDefinition.ReadModule(stream);

            var metadataType = module.Types.FirstOrDefault(t =>
                t.BaseType?.Name == "AbstractModMetadata" && !t.IsAbstract
            );

            if (metadataType is null)
            {
                return null;
            }

            var modGuid = GetStringProperty(metadataType, "ModGuid");
            var name = GetStringProperty(metadataType, "Name");
            var author = GetStringProperty(metadataType, "Author");
            var modVersion = GetStringProperty(metadataType, "Version");
            var sptVersion = GetStringProperty(metadataType, "SptVersion");
            var modUrl = GetStringProperty(metadataType, "Url");

            if (string.IsNullOrEmpty(modGuid))
            {
                return null;
            }

            var version = modVersion ?? GetAssemblyVersion(module);

            var warnings = ValidateModMetadata(name ?? string.Empty, author ?? string.Empty, version, modGuid);

            return new Mod
            {
                Local = new LocalModIdentity
                {
                    Guid = modGuid,
                    FilePath = dllPath,
                    IsServerMod = true,
                    LocalName = name ?? string.Empty,
                    LocalAuthor = author ?? string.Empty,
                    LocalVersion = version,
                    LocalSptVersion = sptVersion,
                    Url = modUrl,
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
            using var packageStream = fileSystem.OpenRead(packagePath);
            using var packageDocument = await JsonDocument.ParseAsync(
                packageStream,
                cancellationToken: cancellationToken
            );
            var root = packageDocument.RootElement;
            var directoryName = Path.GetFileName(modDirectory);
            var name = GetStringPropertyFromJson(root, "name") ?? directoryName;
            var author = GetStringPropertyFromJson(root, "author") ?? "Unknown";
            var version = GetStringPropertyFromJson(root, "version") ?? string.Empty;
            var sptVersion =
                GetStringPropertyFromJson(root, "sptVersion") ?? GetStringPropertyFromJson(root, "akiVersion");

            var warnings = ValidateModMetadata(name, author, version, name);

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
                },
                LoadWarnings = warnings,
            };
        }
        catch (Exception ex) when (ex is JsonException or IOException or UnauthorizedAccessException)
        {
            logger.LogDebug(ex, "Could not parse package.json as a server mod: {Path}", packagePath);
            return null;
        }
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

    private static string? GetStringProperty(Mono.Cecil.TypeDefinition type, string propertyName)
    {
        var getter = type.Methods.FirstOrDefault(m => m.Name == $"get_{propertyName}");
        if (getter?.HasBody == true)
        {
            foreach (var instruction in getter.Body.Instructions)
            {
                if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldstr)
                {
                    return instruction.Operand as string;
                }
            }
        }

        foreach (var ctor in type.Methods.Where(m => m.IsConstructor && !m.IsStatic && m.HasBody))
        {
            string? lastString = null;
            foreach (var instruction in ctor.Body.Instructions)
            {
                if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Ldstr)
                {
                    lastString = instruction.Operand as string;
                }
                else if (instruction.OpCode.Code == Mono.Cecil.Cil.Code.Stfld)
                {
                    if (
                        instruction.Operand is Mono.Cecil.FieldReference fieldRef
                        && fieldRef.Name.Contains($"<{propertyName}>")
                    )
                    {
                        if (lastString != null)
                        {
                            return lastString;
                        }
                    }

                    // Reset the tracked string after ANY field assignment so it doesn't leak
                    // into properties that are dynamically initialized (e.g. via reflection).
                    lastString = null;
                }
            }
        }

        return null;
    }

    private static string GetAssemblyVersion(Mono.Cecil.ModuleDefinition module)
    {
        var infoVersionAttr = module.Assembly.CustomAttributes.FirstOrDefault(a =>
            a.AttributeType.Name == "AssemblyInformationalVersionAttribute"
        );
        if (infoVersionAttr is not null && infoVersionAttr.HasConstructorArguments)
        {
            var fullVersion = infoVersionAttr.ConstructorArguments[0].Value?.ToString();
            if (!string.IsNullOrEmpty(fullVersion))
            {
                var plusIndex = fullVersion.IndexOf('+');
                if (plusIndex > 0)
                {
                    return fullVersion[..plusIndex];
                }
                return fullVersion;
            }
        }

        var version = module.Assembly.Name.Version;
        if (version is null)
        {
            return string.Empty;
        }

        return $"{version.Major}.{version.Minor}.{version.Build}";
    }

    private static List<string> ValidateModMetadata(string name, string author, string version, string guid)
    {
        List<string> warnings = [];

        if (string.IsNullOrWhiteSpace(name))
        {
            warnings.Add("Missing mod name");
        }

        if (string.IsNullOrWhiteSpace(author))
        {
            warnings.Add("Missing author");
        }

        if (string.IsNullOrWhiteSpace(version))
        {
            warnings.Add("Missing version");
        }
        else if (!IsValidVersion(version))
        {
            warnings.Add($"Invalid version format: {version}");
        }

        if (string.IsNullOrWhiteSpace(guid))
        {
            warnings.Add("Missing GUID");
        }

        return warnings;
    }

    private static bool IsValidVersion(string version)
    {
        return SemVer.TryParse(version, "ServerModExtractor").IsT0;
    }
}

