using System;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services.Interfaces;
using CheckModsExtended.Services.Web;
using CheckModsExtended.Utils;
using SPTarkov.DI.Annotations;
using OneOf;

namespace CheckModsExtended.Services;

[Injectable(InjectionType.Transient)]
public class SettingsService : ISettingsService
{
    private readonly IFileSystem _fileSystem;

    public SettingsService(IFileSystem fileSystem)
    {
        _fileSystem = fileSystem;
    }

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

    public async Task<OneOf<MessageResponse, ApiError>> UpdateSettingsAsync(string jsonPayload, CancellationToken token = default)
    {
        // Validate JSON before saving
        try { JsonDocument.Parse(jsonPayload); }
        catch (JsonException) { return new ApiError("Invalid JSON payload"); }

        await _fileSystem.WriteAllTextAsync("appsettings.json", jsonPayload, token);
        return new MessageResponse("Settings saved successfully. A restart may be required for some settings to take effect.");
    }
}
