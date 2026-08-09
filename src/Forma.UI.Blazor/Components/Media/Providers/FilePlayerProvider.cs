namespace Forma.UI.Components.Media.Providers;

using Forma.Framework.Media;
using Forma.UI.Components.Media;

/// <summary>Plays a file the browser can fetch, through its own player.</summary>
internal sealed class FilePlayerProvider : IPlayerProvider
{
    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
        => ValueTask.FromResult<PlayerFrame?>(IsReachableFromPage(source) ? new NativePlayerFrame(source) : null);

    // A relative address resolves against the page; an absolute one has to use a scheme the browser will fetch,
    // so file:// and the like are refused here rather than left to fail as an empty player.
    private static bool IsReachableFromPage(string source)
        => Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out var uri)
            && (!uri.IsAbsoluteUri || uri.Scheme is "http" or "https" or "data" or "blob");
}
