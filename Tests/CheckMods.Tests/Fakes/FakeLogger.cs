using Microsoft.Extensions.Logging;

namespace CheckMods.Tests.Fakes;

/// <summary>
/// Fake implementation of <see cref="ILogger{T}"/>.
/// </summary>
public sealed class FakeLogger<T> : ILogger<T>
{
    private readonly List<string> _loggedMessages = [];

    /// <summary>
    /// Gets a defensive copy of the messages that have been logged.
    /// </summary>
    public List<string> LoggedMessages
    {
        get
        {
            return _loggedMessages.ToList();
        }
    }

    /// <inheritdoc />
    public IDisposable? BeginScope<TState>(TState state) where TState : notnull
    {
        return null;
    }

    /// <inheritdoc />
    public bool IsEnabled(LogLevel logLevel)
    {
        return true;
    }

    /// <inheritdoc />
    public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
    {
        _loggedMessages.Add(formatter(state, exception));
    }
}






