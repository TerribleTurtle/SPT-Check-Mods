using CheckMods.Services.Interfaces;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="IInitializationService"/> for testing.
/// </summary>
public sealed class FakeInitializationService : IInitializationService
{
    /// <summary>
    /// Gets a value indicating whether <see cref="RemoveLegacyApiKeyFileAsync"/> was called.
    /// </summary>
    public bool RemoveLegacyApiKeyFileCalled { get; private set; }

    /// <summary>
    /// Gets a value indicating whether <see cref="GetValidatedSptPath"/> was called.
    /// </summary>
    public bool GetValidatedSptPathCalled { get; private set; }

    /// <summary>
    /// Gets or sets the path to return from <see cref="GetValidatedSptPath"/>.
    /// </summary>
    public string? ValidatedSptPathToReturn { get; set; } = Directory.GetCurrentDirectory();

    /// <inheritdoc />
    public Task RemoveLegacyApiKeyFileAsync()
    {
        RemoveLegacyApiKeyFileCalled = true;
        return Task.CompletedTask;
    }

    /// <inheritdoc />
    public string? GetValidatedSptPath(string[] args)
    {
        GetValidatedSptPathCalled = true;

        if (args.Length > 0 && args[0].Contains("invalid"))
        {
            return null;
        }

        return ValidatedSptPathToReturn;
    }
}
