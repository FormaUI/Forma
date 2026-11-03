namespace Forma.Framework.Media.Providers;

/// <summary>Plays a file the browser can fetch, through its own player.</summary>
public sealed class FilePlayerProvider : IPlayerProvider
{
    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
        => ValueTask.FromResult<PlayerFrame?>(IsReachableFromPage(source) ? new NativePlayerFrame(source) : null);

    private static bool IsReachableFromPage(string source)
        => Uri.TryCreate(source, UriKind.RelativeOrAbsolute, out var uri)
            && (!uri.IsAbsoluteUri || uri.Scheme is "http" or "https" or "data" or "blob");
}
