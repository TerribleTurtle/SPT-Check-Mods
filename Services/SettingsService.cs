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

/// <summary>
/// Service for reading and updating application settings.
/// </summary>
[Injectable(InjectionType.Transient)]
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
}
