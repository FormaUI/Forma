namespace Forma.Blazor.Showcase;

using Forma.Blazor.Components.Media;
using Forma.Framework.Media;

/// <summary>A player provider resolving the Showcase's own <c>custom:</c> addresses against its media folder.</summary>
public sealed class CustomPlayerProvider : IPlayerProvider
{
    private const string Scheme = "custom:";

    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return ValueTask.FromResult<PlayerFrame?>(source.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase)
            ? new NativePlayerFrame($"/media/{source[Scheme.Length..]}.mp4")
            : null);
    }
}
