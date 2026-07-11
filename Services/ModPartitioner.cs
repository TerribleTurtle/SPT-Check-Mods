using System;
using System.Collections.Generic;
using System.Linq;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using SPTarkov.DI.Annotations;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public sealed class ModPartitioner : IModPartitioner
{
    /// <inheritdoc />
    public List<List<PluginDll>> PartitionByRelatedness(List<PluginDll> plugins)
    {
        var parents = Enumerable.Range(0, plugins.Count).ToArray();

        int Find(int node)
        {
            while (parents[node] != node)
            {
                parents[node] = parents[parents[node]];
                node = parents[node];
            }

            return node;
        }

        for (var i = 0; i < plugins.Count; i++)
        {
            for (var j = i + 1; j < plugins.Count; j++)
            {
                if (AreRelated(plugins[i], plugins[j]))
                {
                    parents[Find(i)] = Find(j);
                }
            }
        }

        return plugins
            .Select((plugin, index) => (plugin, Root: Find(index)))
            .GroupBy(item => item.Root)
            .Select(group => group.Select(item => item.plugin).ToList())
            .ToList();
    }

    private static bool AreRelated(PluginDll a, PluginDll b)
    {
        return References(a, b) || References(b, a) || SameAuthorNamespace(a.Plugin.Guid, b.Plugin.Guid);
    }

    private static bool References(PluginDll from, PluginDll to)
    {
        return !string.IsNullOrEmpty(to.AssemblyName) && from.ReferencedAssemblyNames.Contains(to.AssemblyName);
    }

    private static readonly HashSet<string> _genericGuidSegments = new(StringComparer.OrdinalIgnoreCase)
    {
        "com", "org", "net", "io", "dev", "co", "app", "me", "gg", "xyz", "github", "gitlab", "gitee"
    };

    private static bool SameAuthorNamespace(string guidA, string guidB)
    {
        var nsA = AuthorNamespaceSegments(guidA);
        var nsB = AuthorNamespaceSegments(guidB);

        if (nsA.Count == 0 || nsB.Count == 0)
        {
            return false;
        }

        var min = Math.Min(nsA.Count, nsB.Count);

        var shared = 0;
        while (shared < min && string.Equals(nsA[shared], nsB[shared], StringComparison.OrdinalIgnoreCase))
        {
            shared++;
        }

        if (shared < min)
        {
            return false;
        }

        return nsA.Take(shared).Any(segment => !_genericGuidSegments.Contains(segment));
    }

    private static List<string> AuthorNamespaceSegments(string? guid)
    {
        if (string.IsNullOrWhiteSpace(guid))
        {
            return [];
        }

        var parts = guid.Split('.', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries);

        return parts.Length <= 1 ? [] : parts[..^1].ToList();
    }
}
