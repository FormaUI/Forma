namespace Forma.Tests.Shared;

using Microsoft.Extensions.Logging;

/// <summary>A logger factory that keeps every entry written through it, with the category it was written under.</summary>
public sealed class FakeLoggerFactory : ILoggerFactory
{
    private readonly Lock _lock = new();
    private readonly List<FakeLogEntry> _entries = [];
    private readonly List<(Func<FakeLogEntry, bool> Predicate, TaskCompletionSource<FakeLogEntry> Entry)> _waiters = [];

    /// <summary>Every entry written through a logger this factory created, in the order written.</summary>
    public IReadOnlyList<FakeLogEntry> Entries
    {
        get
        {
            lock (_lock)
            {
                return [.. _entries];
            }
        }
    }

    /// <inheritdoc />
    public ILogger CreateLogger(string categoryName)
        => new FakeLogger(categoryName, this);

    /// <summary>The first entry <paramref name="predicate"/> matches, once it is written.</summary>
    /// <param name="predicate">Whether an entry is the one waited for.</param>
    /// <returns>The entry.</returns>
    public Task<FakeLogEntry> WaitForEntryAsync(Func<FakeLogEntry, bool> predicate)
    {
        ArgumentNullException.ThrowIfNull(predicate);

        lock (_lock)
        {
            foreach (var entry in _entries)
            {
                if (predicate(entry))
                {
                    return Task.FromResult(entry);
                }
            }

            // Its continuation runs apart from the logger's caller, which goes on as it would with no test waiting.
            var waiter = new TaskCompletionSource<FakeLogEntry>(TaskCreationOptions.RunContinuationsAsynchronously);
            _waiters.Add((predicate, waiter));

            return waiter.Task;
        }
    }

    /// <inheritdoc />
    public void AddProvider(ILoggerProvider provider)
    {
    }

    /// <inheritdoc />
    public void Dispose()
    {
    }

    private void Add(FakeLogEntry entry)
    {
        List<TaskCompletionSource<FakeLogEntry>> matchedWaiters;

        lock (_lock)
        {
            _entries.Add(entry);
            matchedWaiters = [.. _waiters.Where(waiter => waiter.Predicate(entry)).Select(waiter => waiter.Entry)];
            _waiters.RemoveAll(waiter => matchedWaiters.Contains(waiter.Entry));
        }

        foreach (var waiter in matchedWaiters)
        {
            waiter.SetResult(entry);
        }
    }

    // A template naming a field twice lists it twice, and the value written last is the one kept.
    private static Dictionary<string, object?> GetFields(IEnumerable<KeyValuePair<string, object?>> fields)
    {
        var named = new Dictionary<string, object?>(StringComparer.Ordinal);
        foreach (var (name, value) in fields)
        {
            named[name] = value;
        }

        return named;
    }

    private sealed class FakeLogger(string category, FakeLoggerFactory factory) : ILogger
    {
        public IDisposable? BeginScope<TState>(TState state)
            where TState : notnull
            => null;

        public bool IsEnabled(LogLevel logLevel)
            => true;

        public void Log<TState>(LogLevel logLevel, EventId eventId, TState state, Exception? exception, Func<TState, Exception?, string> formatter)
            => factory.Add(new FakeLogEntry(category, logLevel, formatter(state, exception))
            {
                EventId = eventId,
                Fields = state is IEnumerable<KeyValuePair<string, object?>> fields ? GetFields(fields) : [],
            });
    }
}

/// <summary>One entry a <see cref="FakeLoggerFactory"/> kept.</summary>
/// <param name="Category">The category the entry was written under.</param>
/// <param name="Level">The level it was written at.</param>
/// <param name="Message">The message as the logger formatted it.</param>
public readonly record struct FakeLogEntry(string Category, LogLevel Level, string Message)
{
    /// <summary>The event the entry was written for.</summary>
    public EventId EventId { get; init; }

    /// <summary>The structured fields the entry was written with, keyed by their names.</summary>
    public IReadOnlyDictionary<string, object?> Fields { get; init; } = new Dictionary<string, object?>();
}
