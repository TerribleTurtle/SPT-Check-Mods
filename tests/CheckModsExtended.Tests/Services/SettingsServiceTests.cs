using System.Text.Json;
using System.Threading.Tasks;
using CheckModsExtended.Models;
using CheckModsExtended.Services;
using CheckModsExtended.Services.Web;
using CheckModsExtended.Tests.Fixtures;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class SettingsServiceTests
{
    private readonly FakeFileSystem _fileSystem;
    private readonly SettingsService _settingsService;

    public SettingsServiceTests()
    {
        _fileSystem = new FakeFileSystem();
        _settingsService = new SettingsService(_fileSystem);
    }

    [Fact]
    public async Task GetSettingsAsync_FallsBackToExample_IfAppSettingsIsMissing()
    {
        string exampleJson = "{\"setting\": \"example\"}";
        _fileSystem.Files["appsettings.example.json"] = System.Text.Encoding.UTF8.GetBytes(exampleJson);

        string result = await _settingsService.GetSettingsAsync();

        Assert.Equal(exampleJson, result);
    }

    [Fact]
    public async Task GetSettingsAsync_ReturnsEmptyObject_IfBothAreMissing()
    {
        string result = await _settingsService.GetSettingsAsync();

        Assert.Equal("{}", result);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ReturnsSuccess_IfJsonIsValid()
    {
        string validJson = "{\"setting\": \"new value\"}";

        OneOf.OneOf<MessageResponse, ApiError> result = await _settingsService.UpdateSettingsAsync(validJson);

        Assert.True(result.IsT0);
        Assert.NotNull(result.AsT0.Message);
        
        string writtenContent = await _fileSystem.ReadAllTextAsync("appsettings.json");
        Assert.Equal(validJson, writtenContent);
    }

    [Fact]
    public async Task UpdateSettingsAsync_ReturnsApiError_IfJsonIsInvalid()
    {
        string invalidJson = "{ invalid json";

        OneOf.OneOf<MessageResponse, ApiError> result = await _settingsService.UpdateSettingsAsync(invalidJson);

        Assert.True(result.IsT1);
        Assert.IsType<ApiError>(result.AsT1);
        Assert.False(_fileSystem.FileExists("appsettings.json"));
    }
}
