using System;
using System.Linq;

namespace CheckModsExtended.Services.Utils;

public static class PluginNamingStrategy
{
    public static string NormalizeModName(string name)
    {
        var dashIndex = name.IndexOf('-');
        if (dashIndex > 0 && dashIndex < name.Length - 1)
        {
            name = name[(dashIndex + 1)..];
        }

        string[] suffixes = ["Client", "Plugin", "Mod", "BepInEx"];
        var matchingSuffix = suffixes.FirstOrDefault(s => name.EndsWith(s, StringComparison.OrdinalIgnoreCase));
        if (matchingSuffix is not null)
        {
            name = name[..^matchingSuffix.Length];
        }

        return name.Trim();
    }

    public static (string Author, string Name) ParseAuthorAndName(string pluginName, string guid)
    {
        if (pluginName.Contains(" - "))
        {
            var parts = pluginName.Split(" - ", 2);
            if (parts.Length == 2)
            {
                return (parts[0].Trim(), parts[1].Trim());
            }
        }

        if (pluginName.Contains(" by ", StringComparison.OrdinalIgnoreCase))
        {
            var byIndex = pluginName.IndexOf(" by ", StringComparison.OrdinalIgnoreCase);
            var name = pluginName[..byIndex].Trim();
            var author = pluginName[(byIndex + 4)..].Trim();
            return (author, name);
        }

        var guidParts = guid.Split('.');
        if (guidParts.Length < 2)
        {
            return ("Unknown", pluginName);
        }

        var potentialAuthor = guidParts.Length >= 3 ? guidParts[^2] : guidParts[0];

        if (
            string.Equals(potentialAuthor, "com", StringComparison.OrdinalIgnoreCase)
            || string.Equals(potentialAuthor, "org", StringComparison.OrdinalIgnoreCase)
            || string.Equals(potentialAuthor, "spt", StringComparison.OrdinalIgnoreCase)
            || string.Equals(potentialAuthor, "aki", StringComparison.OrdinalIgnoreCase)
        )
        {
            return ("Unknown", pluginName);
        }

        return (potentialAuthor, pluginName);
    }
}
