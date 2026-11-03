namespace Forma.Tests.Shared;

using Microsoft.Extensions.Logging;

/// <summary>A logger factory that keeps every entry written through it, with the category it was written under.</summary>
public sealed class FakeLoggerFactory : ILoggerFactory
{
    private readonly List<FakeLogEntry> _entries = [];

    /// <summary>Every entry written through a logger this factory created, in the order written.</summary>
    public IReadOnlyList<FakeLogEntry> Entries
        => _entries;

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
        => new FakeLogger(categoryName, _entries);

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private sealed class FakeLogger(string category, List<FakeLogEntry> entries) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => entries.Add(new FakeLogEntry(category, logLevel, formatter(state, exception)));
    }
}

/// <summary>One entry a <see cref="FakeLoggerFactory"/> kept.</summary>
/// <param name="Category">The category the entry was written under.</param>
/// <param name="Level">The level it was written at.</param>
/// <param name="Message">The message as the logger formatted it.</param>
public readonly record struct FakeLogEntry(string Category, LogLevel Level, string Message);
