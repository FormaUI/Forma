namespace Forma.Framework.Media.Providers;

using Forma.Framework.Sanitization;

/// <summary>Plays a file the browser can fetch, through its own player.</summary>
public sealed class FilePlayerProvider : IPlayerProvider
{
    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
        => ValueTask.FromResult<PlayerFrame?>(AddressPolicy.Allows(source, AddressKind.VideoSource) ? new NativePlayerFrame(source) : null);
}
