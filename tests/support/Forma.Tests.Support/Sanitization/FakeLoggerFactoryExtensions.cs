namespace Forma.Tests.Sanitization;

using Forma.Tests.Shared;

using Microsoft.Extensions.Logging;

/// <summary>What a fake logger factory kept of the addresses components wrote inert.</summary>
public static class FakeLoggerFactoryExtensions
{
    /// <summary>The entries recording an address a component wrote inert, each naming the address's scheme and kind.</summary>
    /// <param name="loggerFactory">The factory the components logged through.</param>
    public static IReadOnlyList<FakeLogEntry> GetAddressRefusals(this FakeLoggerFactory loggerFactory)
    {
        ArgumentNullException.ThrowIfNull(loggerFactory);

        return [.. loggerFactory.Entries.Where(entry => entry.Level == LogLevel.Debug && entry.Fields.ContainsKey("Scheme") && entry.Fields.ContainsKey("Kind"))];
    }
}
