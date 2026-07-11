using CheckMods.Services;
using CheckMods.Tests.Fakes;
using Xunit;

namespace CheckMods.Tests.Services;

public sealed class InitializationServiceTests
{
    private readonly FakeModCheckReporter _reporter = new();
    private readonly FakeLogger<InitializationService> _logger = new();
    private readonly InitializationService _sut;

    public InitializationServiceTests()
    {
        _sut = new InitializationService(_reporter, _logger);
    }

    [Fact]
    public async Task remove_legacy_api_key_file_does_not_throw_if_file_missing()
    {
        // Act
        var exception = await Record.ExceptionAsync(() => _sut.RemoveLegacyApiKeyFileAsync());

        // Assert
        Assert.Null(exception);
    }

    [Fact]
    public void get_validated_spt_path_returns_current_directory_if_no_args()
    {
        // Act
        var result = _sut.GetValidatedSptPath([]);

        // Assert
        Assert.Equal(Directory.GetCurrentDirectory(), result);
    }

    [Fact]
    public void get_validated_spt_path_returns_null_if_path_invalid()
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
    public void get_validated_spt_path_returns_null_if_directory_does_not_exist()
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
    public void get_validated_spt_path_returns_path_if_directory_exists()
    {
        // Arrange
        var tempDirectory = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
        Directory.CreateDirectory(tempDirectory);

        try
        {
            // Act
            var result = _sut.GetValidatedSptPath([tempDirectory]);

            // Assert
            Assert.Equal(Path.GetFullPath(tempDirectory), result);
        }
        finally
        {
            Directory.Delete(tempDirectory);
        }
    }
}
