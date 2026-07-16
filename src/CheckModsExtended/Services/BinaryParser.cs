using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.IO;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Utils;
using Mono.Cecil;
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
public class BinaryParser(IFileSystem fileSystem) : IBinaryParser
{
    public BepInPluginAttribute? ExtractBepInPlugin(string dllPath)
    {
        var stream = fileSystem.Open(dllPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ModuleDefinition module;
        try
        {
            module = ModuleDefinition.ReadModule(stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        using (module)
        {
            foreach (var type in module.Types)
            {
                var plugin = ExtractBepInPluginAttribute(type);
                if (plugin is not null)
                {
                    return plugin;
                }
            }
        }

        return null;
    }

    public (BepInPluginAttribute? Plugin, string? AssemblyName, HashSet<string>? ReferencedNames) ReadPluginDllInfo(string dllPath)
    {
        var stream = fileSystem.Open(dllPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ModuleDefinition module;
        try
        {
            module = ModuleDefinition.ReadModule(stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        using (module)
        {
            BepInPluginAttribute? plugin = null;
            foreach (var type in module.Types)
            {
                plugin = ExtractBepInPluginAttribute(type);
                if (plugin is not null)
                {
                    break;
                }
            }

            var referencedNames = module
                .AssemblyReferences.Select(r => r.Name)
                .ToHashSet(StringComparer.OrdinalIgnoreCase);

            return (plugin, module.Assembly.Name.Name, referencedNames);
        }
    }

    private static BepInPluginAttribute? ExtractBepInPluginAttribute(TypeDefinition type)
    {
        if (!type.HasCustomAttributes)
        {
            return null;
        }

        var attr = type.CustomAttributes.FirstOrDefault(a =>
            a.AttributeType.Name == "BepInPlugin"
            || a.AttributeType.Name == "BepInPluginAttribute"
            || a.AttributeType.FullName.Contains("BepInPlugin")
        );

        if (attr is null || !attr.HasConstructorArguments || attr.ConstructorArguments.Count < 3)
        {
            return null;
        }

        var guid = attr.ConstructorArguments[0].Value?.ToString() ?? "";
        var name = attr.ConstructorArguments[1].Value?.ToString() ?? "";
        var version = attr.ConstructorArguments[2].Value?.ToString() ?? "";

        if (string.IsNullOrWhiteSpace(guid) || string.IsNullOrWhiteSpace(name) || string.IsNullOrWhiteSpace(version))
        {
            return null;
        }

        return new BepInPluginAttribute(guid, name, version);
    }

    public ServerModBinaryInfo? ExtractServerModMetadata(string dllPath)
    {
        var stream = fileSystem.Open(dllPath, FileMode.Open, FileAccess.Read, FileShare.ReadWrite);
        ModuleDefinition module;
        try
        {
            module = ModuleDefinition.ReadModule(stream);
        }
        catch
        {
            stream.Dispose();
            throw;
        }

        using (module)
        {
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

            var version = modVersion ?? GetAssemblyVersion(module);

            return new ServerModBinaryInfo(modGuid, name, author, version, sptVersion, modUrl);
        }
    }

    private static string? GetStringProperty(TypeDefinition type, string propertyName)
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

    private static string GetAssemblyVersion(ModuleDefinition module)
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
}
