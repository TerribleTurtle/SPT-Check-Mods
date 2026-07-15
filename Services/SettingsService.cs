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
    public async Task UpdateIgnoredUpdateOptionsAsync(bool useCommunityList, CancellationToken token = default)
    {
        var json = await GetSettingsAsync(token);
        var root = System.Text.Json.Nodes.JsonNode.Parse(json) as System.Text.Json.Nodes.JsonObject ?? new System.Text.Json.Nodes.JsonObject();
        
        var optionsNode = root["IgnoredUpdateOptions"] as System.Text.Json.Nodes.JsonObject;
        if (optionsNode == null)
        {
            optionsNode = new System.Text.Json.Nodes.JsonObject();
            root["IgnoredUpdateOptions"] = optionsNode;
        }
        
        optionsNode["UseCommunityList"] = useCommunityList;
        
        var newJson = root.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        await UpdateSettingsAsync(newJson, token);
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

    /// <inheritdoc />
    public async Task<OneOf<MessageResponse, ApiError>> UpdateIgnoredUpdateOptionsAsync(
        System.Action<CheckModsExtended.Configuration.IgnoredUpdateOptions> updateAction,
        CancellationToken token = default
    )
    {
        var settingsJson = await GetSettingsAsync(token);
        
        // Parse the current settings dynamically
        var jsonNode = System.Text.Json.Nodes.JsonNode.Parse(settingsJson)?.AsObject() 
            ?? new System.Text.Json.Nodes.JsonObject();

        // Extract or create the IgnoredUpdateOptions block
        CheckModsExtended.Configuration.IgnoredUpdateOptions options = new();
        if (jsonNode.TryGetPropertyValue("IgnoredUpdateOptions", out var optionsNode) && optionsNode is not null)
        {
            options = JsonSerializer.Deserialize<CheckModsExtended.Configuration.IgnoredUpdateOptions>(
                optionsNode.ToJsonString(),
                new JsonSerializerOptions { PropertyNameCaseInsensitive = true }
            ) ?? new();
        }

        // Apply the requested updates
        updateAction(options);

        // Save back into the JSON node
        jsonNode["IgnoredUpdateOptions"] = System.Text.Json.Nodes.JsonNode.Parse(
            JsonSerializer.Serialize(options, new JsonSerializerOptions { WriteIndented = true })
        );

        // Write the updated document
        var newSettingsJson = jsonNode.ToJsonString(new JsonSerializerOptions { WriteIndented = true });
        return await UpdateSettingsAsync(newSettingsJson, token);
    }
}
