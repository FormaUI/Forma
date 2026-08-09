namespace Forma.UI.Blazor.Tests.Support.Components.Media;

using Forma.UI.Components.Media;

/// <summary>A player provider that claims addresses starting with one prefix, standing in for a host's own service.</summary>
public sealed class FakePlayerProvider(string prefix, Func<string, PlayerFrame> resolve) : IPlayerProvider
{
    /// <summary>How many times the provider was asked for a frame.</summary>
    public int Calls { get; private set; }

    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        Calls++;

        var claims = source.StartsWith(prefix, StringComparison.OrdinalIgnoreCase);

        return ValueTask.FromResult(claims ? resolve(source) : null);
    }
}
