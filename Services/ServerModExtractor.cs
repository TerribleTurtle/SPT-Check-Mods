using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using CheckMods.Models;
using CheckMods.Services.Interfaces;
using CheckMods.Utils;
using Microsoft.Extensions.Logging;
using Mono.Cecil;
using Mono.Cecil.Cil;
using SPTarkov.DI.Annotations;

namespace CheckMods.Services;

[Injectable(InjectionType.Transient)]
public sealed class ServerModExtractor(ILogger<ServerModExtractor> logger) : IServerModExtractor
{
    /// <inheritdoc />
    public Mod? ExtractServerModMetadata(string dllPath, string sptDirectory)
    {
        try
        {
            using var stream = new MemoryStream(File.ReadAllBytes(dllPath));
            using var assemblyDef = AssemblyDefinition.ReadAssembly(stream);
            
            var metadataType = GetSptMetadataType(assemblyDef);
            if (metadataType is null)
            {
                return null;
            }

            var modGuid = string.Empty;
            var name = string.Empty;
            var author = string.Empty;
            var modVersion = string.Empty;
            var sptVersion = string.Empty;

            var ctor = metadataType.Methods.FirstOrDefault(m => m.IsConstructor && !m.IsStatic && !m.HasParameters);
            if (ctor != null && ctor.HasBody)
            {
                var instructions = ctor.Body.Instructions;
                for (int i = 0; i < instructions.Count; i++)
                {
                    if (instructions[i].OpCode == OpCodes.Ldstr)
                    {
                        var strValue = (string)instructions[i].Operand;
                        for (int j = i + 1; j < Math.Min(i + 5, instructions.Count); j++)
                        {
                            if (instructions[j].OpCode == OpCodes.Call || instructions[j].OpCode == OpCodes.Callvirt)
                            {
                                if (instructions[j].Operand is MethodReference methodRef && methodRef.Name.StartsWith("set_"))
                                {
                                    var propName = methodRef.Name.Substring(4);
                                    if (propName == "ModGuid") modGuid = strValue;
                                    else if (propName == "Name") name = strValue;
                                    else if (propName == "Author") author = strValue;
                                    else if (propName == "Version") modVersion = strValue;
                                    else if (propName == "SptVersion") sptVersion = strValue;
                                    break;
                                }
                            }
                        }
                    }
                }
            }

            if (string.IsNullOrEmpty(modGuid))
            {
                return null;
            }

            var version = string.IsNullOrEmpty(modVersion) ? GetAssemblyVersion(assemblyDef) : modVersion;

            var warnings = ValidateModMetadata(name, author, version, modGuid);

            return new Mod
            {
                Guid = modGuid,
                FilePath = dllPath,
                IsServerMod = true,
                LocalName = name,
                LocalAuthor = author,
                LocalVersion = version,
                LocalSptVersion = string.IsNullOrEmpty(sptVersion) ? null : sptVersion,
                LoadWarnings = warnings,
            };
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "Could not inspect DLL as a server mod: {DllPath}", dllPath);
            return null;
        }
    }

    private static TypeDefinition? GetSptMetadataType(AssemblyDefinition assembly)
    {
        foreach (var module in assembly.Modules)
        {
            foreach (var type in module.Types)
            {
                if (!type.IsAbstract && type.BaseType != null && type.BaseType.Name == "AbstractModMetadata")
                {
                    return type;
                }
            }
        }
        return null;
    }

    private static string GetAssemblyVersion(AssemblyDefinition assembly)
    {
        var infoVersionAttr = assembly.CustomAttributes.FirstOrDefault(a => a.AttributeType.Name == "AssemblyInformationalVersionAttribute");

        if (infoVersionAttr is null)
        {
            var version = assembly.Name.Version;
            if (version is null)
            {
                return string.Empty;
            }

            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        var fullVersion = (string)infoVersionAttr.ConstructorArguments[0].Value;
        if (string.IsNullOrEmpty(fullVersion))
        {
            var version = assembly.Name.Version;
            if (version is null)
            {
                return string.Empty;
            }

            return $"{version.Major}.{version.Minor}.{version.Build}";
        }

        var plusIndex = fullVersion.IndexOf('+');
        if (plusIndex > 0)
        {
            fullVersion = fullVersion[..plusIndex];
        }

        return fullVersion;
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
        return SemVer.TryParse(version) is not null;
    }
}
