namespace Forma.UI.Blazor.Showcase;

using Forma.Framework.Media;
using Forma.UI.Components.Media;

/// <summary>
/// Claims the gallery's own <c>custom:</c> addresses and resolves them against its storage,
/// standing in for a host that identifies videos its own way rather than by a public address.
/// </summary>
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
