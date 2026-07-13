import os

base_dir = r'C:\Users\evanw\.gemini\antigravity\brain\8820e550-52fe-46e2-95e7-47af674114b7\.system_generated\worktrees\subagent-Data-Encapsulator-self-8517cb71'

def read_file(path):
    with open(path, 'r', encoding='utf-8') as f:
        return f.read()

def write_file(path, content):
    with open(path, 'w', encoding='utf-8') as f:
        f.write(content)

# 1. Models/MisplacedMod.cs
misplaced_mod_path = os.path.join(base_dir, 'Models', 'MisplacedMod.cs')
content = read_file(misplaced_mod_path)
content = content.replace('using System.Linq;\n\n', '')
idx = content.find('    /// <summary>\n    /// Exact DLL paths to drop')
if idx != -1:
    content = content[:idx] + '}\n'
write_file(misplaced_mod_path, content)

# 2. Models/ModExtensions.cs
mod_ext_path = os.path.join(base_dir, 'Models', 'ModExtensions.cs')
mod_ext = read_file(mod_ext_path)
mod_ext = mod_ext.replace('using System.Linq;\n\n', '')

old_up_to_date = '''    public static Mod WithUpToDate(this Mod mod, UpToDateMod upToDate)
    {
        // Fallback heuristic: If the specific version\'s link cannot be found, fallback to the most recent version (index 0)
        // to provide a best-effort download link.
        var link = mod.Api.ApiVersions?.FirstOrDefault(v => v.Version == upToDate.Version)?.Link
                ?? (mod.Api.ApiVersions?.Count > 0 ? mod.Api.ApiVersions[0].Link : null);

        return mod with
        {
            Update = mod.Update with
            {
                LatestVersion = upToDate.Version,
                DownloadLink = link,
                UpdateStatus = UpdateStatus.UpToDate
            },
        };
    }'''
new_up_to_date = '''    public static Mod WithUpToDate(this Mod mod, UpToDateMod upToDate, string? downloadLink)
    {
        return mod with
        {
            Update = mod.Update with
            {
                LatestVersion = upToDate.Version,
                DownloadLink = downloadLink,
                UpdateStatus = UpdateStatus.UpToDate
            },
        };
    }'''
mod_ext = mod_ext.replace(old_up_to_date, new_up_to_date)

old_incomp = '''    public static Mod WithIncompatible(this Mod mod, IncompatibleMod incompatible)
    {
        // Fallback heuristic: If the latest compatible version is missing a link, fallback to the most recent version (index 0)
        // to ensure a download link is still provided.
        var link = incompatible.LatestCompatibleVersion?.Link ?? (mod.Api.ApiVersions?.Count > 0 ? mod.Api.ApiVersions[0].Link : null);
        return mod with
        {
            Update = mod.Update with
            {
                IncompatibilityReason = incompatible.Reason,
                DownloadLink = link,
                UpdateStatus = UpdateStatus.Incompatible,
            },
        };
    }'''
new_incomp = '''    public static Mod WithIncompatible(this Mod mod, IncompatibleMod incompatible, string? downloadLink)
    {
        return mod with
        {
            Update = mod.Update with
            {
                IncompatibilityReason = incompatible.Reason,
                DownloadLink = downloadLink,
                UpdateStatus = UpdateStatus.Incompatible,
            },
        };
    }'''
mod_ext = mod_ext.replace(old_incomp, new_incomp)
write_file(mod_ext_path, mod_ext)

# 3. Services/ModEnrichmentService.cs
enrich_path = os.path.join(base_dir, 'Services', 'ModEnrichmentService.cs')
enrich = read_file(enrich_path)
enrich = enrich.replace('''public sealed class ModEnrichmentService(
    IModUpdateClient forgeApiService,
    IGitHubReleaseClient gitHubReleaseClient,
    ILogger<ModEnrichmentService> logger)''', '''public sealed class ModEnrichmentService(
    IModUpdateClient forgeApiService,
    IGitHubReleaseClient gitHubReleaseClient,
    ILogger<ModEnrichmentService> logger,
    IModLinkResolverService linkResolver)''')
enrich = enrich.replace('ProcessUpdates(updatesData.UpToDate, u => u.ModId, (m, u) => m.WithUpToDate(u));', 'ProcessUpdates(updatesData.UpToDate, u => u.ModId, (m, u) => m.WithUpToDate(u, linkResolver.ResolveUpToDateLink(m, u)));')
enrich = enrich.replace('ProcessUpdates(updatesData.Incompatible, i => i.ModId, (m, i) => m.WithIncompatible(i));', 'ProcessUpdates(updatesData.Incompatible, i => i.ModId, (m, i) => m.WithIncompatible(i, linkResolver.ResolveIncompatibleLink(m, i)));')
write_file(enrich_path, enrich)

# 4. Commands/CheckModsCommand.cs
cmd_path = os.path.join(base_dir, 'Commands', 'CheckModsCommand.cs')
cmd = read_file(cmd_path)
cmd = cmd.replace('private readonly Microsoft.Extensions.Caching.Memory.IMemoryCache _memoryCache;', 'private readonly ICacheManager _cacheManager;')
cmd = cmd.replace('Microsoft.Extensions.Caching.Memory.IMemoryCache memoryCache,', 'ICacheManager cacheManager,')
cmd = cmd.replace('_memoryCache = memoryCache;', '_cacheManager = cacheManager;')
cmd = cmd.replace('''                    if (_memoryCache is Microsoft.Extensions.Caching.Memory.MemoryCache concreteCache)
                    {
                        concreteCache.Clear();
                    }''', '''                    _cacheManager.Clear();''')
write_file(cmd_path, cmd)
