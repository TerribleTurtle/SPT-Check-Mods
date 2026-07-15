import sys

content = open('Services/SettingsService.cs', 'r').read()

old_str = '''[Injectable(InjectionType.Transient)]
public sealed class SettingsService : ISettingsService
{
    private readonly IFileSystem _fileSystem;

    public SettingsService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

    /// <inheritdoc />
    public async Task<string> GetSettingsAsync(CancellationToken token = default)
    {
        var path = "appsettings.json";
        if (!_fileSystem.FileExists(path))
        {
            if (_fileSystem.FileExists("appsettings.example.json"))
            {
                return await _fileSystem.ReadAllTextAsync("appsettings.example.json", token);
            }
            return "{}";
        }
        return await _fileSystem.ReadAllTextAsync(path, token);
    }

    /// <inheritdoc />
    public async Task<OneOf<MessageResponse, ApiError>> UpdateSettingsAsync(string jsonPayload, CancellationToken token = default)
    {
        // Validate JSON before saving
        try { JsonDocument.Parse(jsonPayload); }
        catch (JsonException) { return new ApiError("Invalid JSON payload"); }

        var tempPath = "appsettings.json.tmp";
        await _fileSystem.WriteAllTextAsync(tempPath, jsonPayload, token);
        _fileSystem.MoveFile(tempPath, "appsettings.json", overwrite: true);
        return new MessageResponse("Settings saved successfully. A restart may be required for some settings to take effect.");
    }
}'''

new_str = '''[Injectable(InjectionType.Transient)]
public sealed class SettingsService(IFileSystem fileSystem) : ISettingsService
{
    /// <inheritdoc />
    public async Task<string> GetSettingsAsync(CancellationToken token = default)
    {
        var path = "appsettings.json";
        if (!fileSystem.FileExists(path))
        {
            if (fileSystem.FileExists("appsettings.example.json"))
            {
                return await fileSystem.ReadAllTextAsync("appsettings.example.json", token);
            }
            return "{}";
        }
        return await fileSystem.ReadAllTextAsync(path, token);
    }

    /// <inheritdoc />
    public async Task<OneOf<MessageResponse, ApiError>> UpdateSettingsAsync(string jsonPayload, CancellationToken token = default)
    {
        // Validate JSON before saving
        try { JsonDocument.Parse(jsonPayload); }
        catch (JsonException) { return new ApiError("Invalid JSON payload"); }

        var tempPath = "appsettings.json.tmp";
        await fileSystem.WriteAllTextAsync(tempPath, jsonPayload, token);
        fileSystem.MoveFile(tempPath, "appsettings.json", overwrite: true);
        return new MessageResponse("Settings saved successfully. A restart may be required for some settings to take effect.");
    }
}'''

if old_str in content:
    content = content.replace(old_str, new_str)
    open('Services/SettingsService.cs', 'w').write(content)
else:
    print('Failed to find old_str in SettingsService.cs')

content = open('Extensions/ServiceCollectionExtensions.cs', 'r').read()
content = content.replace('        services.AddSingleton<ICacheManager, CacheManager>();\n', '')
content = content.replace('        services.AddSingleton<IScanCacheService, ScanCacheService>();\n', '')
content = content.replace('        services.AddSingleton<IIgnoredUpdateStore, IgnoredUpdateStore>();\n', '')
content = content.replace('        services.AddSingleton<IPluginScanCache, PluginScanCache>();\n', '')
open('Extensions/ServiceCollectionExtensions.cs', 'w').write(content)
