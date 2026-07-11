using CheckModsExtended.Services;
using CheckModsExtended.Tests.Fakes;
using CheckModsExtended.Tests.Fixtures;
using Xunit;

namespace CheckModsExtended.Tests.Services;

public sealed class InitializationServiceTests
{
    private readonly FakeModCheckReporter _reporter = new();
    private readonly FakeLogger<InitializationService> _logger = new();
    private readonly InitializationService _sut;

    public InitializationServiceTests()
    {
        _sut = new InitializationService(_reporter, _logger, Microsoft.Extensions.Options.Options.Create(new CheckModsExtended.Configuration.AppPaths()));
    }

    [Fact]
    public async Task Remove_legacy_api_key_file_does_not_throw_if_file_missing()
    {
        // Act
        var exception = await Record.ExceptionAsync(() => _sut.RemoveLegacyApiKeyFileAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void Get_validated_spt_path_returns_current_directory_if_no_args()
    {
        // Act
        var result = _sut.GetValidatedSptPath([]);

        // Assert
        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Fact]
    public void Get_validated_spt_path_returns_null_if_path_invalid()
    {
        // Act
        // Invalid path chars usually make GetFullPath throw or SecurityHelper return null.
        // Or we just pass an empty string, or string with null char.
        var result = _sut.GetValidatedSptPath([""]);

        // Assert
        Assert.Null(result);
        Assert.Contains("Error: Invalid path provided.", _reporter.Errors);
    }

    [Fact]
    public void Get_validated_spt_path_returns_null_if_directory_does_not_exist()
    {
        // Arrange
        var nonExistentPath = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

        // Act
        var result = _sut.GetValidatedSptPath([nonExistentPath]);

        // Assert
        Assert.Null(result);
        Assert.Contains(_reporter.Errors, e => e.StartsWith("DirectoryDoesNotExist:"));
    }

    [Fact]
    public void Get_validated_spt_path_returns_path_if_directory_exists()
    {
        // Arrange
        var tempDirectory = TempWorkspace.CreateDirectory("InitTests");

        try
        {
            // Act
            var result = _sut.GetValidatedSptPath([tempDirectory]);

            // Assert
            Assert.Equal(Path.GetFullPath(tempDirectory), result);
        }
        finally
        {
            TempWorkspace.SafeDelete(tempDirectory);
        }
    }
}

