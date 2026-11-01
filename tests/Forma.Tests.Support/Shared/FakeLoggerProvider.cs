namespace Forma.Tests.Shared;

using Microsoft.Extensions.Logging;

/// <summary>A logger provider that keeps every entry written through the loggers it creates, as a host's provider receives what its application logs.</summary>
public sealed class FakeLoggerProvider : ILoggerProvider
{
    private readonly FakeLoggerFactory _loggerFactory = new();

    /// <summary>Every entry written through a logger this provider created, in the order written.</summary>
    public IReadOnlyList<FakeLogEntry> Entries
        => _loggerFactory.Entries;

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
        => _loggerFactory.CreateLogger(categoryName);

    /// <inheritdoc />
    public void Dispose()
        => _loggerFactory.Dispose();
}
