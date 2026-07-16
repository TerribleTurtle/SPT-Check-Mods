using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace CheckModsExtended.Services.Utils;

public static class ModPathUtils
{
    public static Dictionary<string, List<string>> GroupDllsByDirectory(List<string> dllFiles, string pluginsDir)
    {
        return dllFiles
            .GroupBy(dllPath => GetModDirectory(dllPath, pluginsDir), StringComparer.OrdinalIgnoreCase)
            .ToDictionary(g => g.Key, g => g.ToList(), StringComparer.OrdinalIgnoreCase);
    }

    public static string GetModDirectory(string dllPath, string pluginsDir)
    {
        var directory = Path.GetDirectoryName(dllPath);

        if (directory is null || directory.Equals(pluginsDir, StringComparison.OrdinalIgnoreCase))
        {
            return pluginsDir;
        }

        while (true)
        {
            var parent = Path.GetDirectoryName(directory);
            if (parent is null || parent.Equals(pluginsDir, StringComparison.OrdinalIgnoreCase))
            {
                return directory;
            }

            directory = parent;
        }
    }
}
