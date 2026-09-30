namespace Forma.Tests.Share;

using Forma.Framework.Share;

/// <summary>A server source answering from a table of servers by target id.</summary>
public sealed class FakeShareServerSource : IShareServerSource
{
    /// <summary>The server answered for each target id.</summary>
    public Dictionary<string, Uri> Servers { get; } = new(StringComparer.Ordinal);

    /// <inheritdoc />
    public Uri? GetServer(string targetId)
        => Servers.TryGetValue(targetId, out var server) ? server : null;
}
