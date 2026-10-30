namespace Forma.Blazor.Browser.Components.Client.Services;

using Forma.Framework.Media;

/// <summary>A player provider embedding the checks' own <c>check:</c> addresses as a page of the host's own, standing in for a hosting service's player.</summary>
public sealed class CheckPlayerProvider : IPlayerProvider
{
    private const string Scheme = "check:";
    private const string PlayerPage = "_check/ready";

    /// <inheritdoc />
    public ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        return ValueTask.FromResult<PlayerFrame?>(source.StartsWith(Scheme, StringComparison.OrdinalIgnoreCase)
            ? new EmbeddedPlayerFrame(PlayerPage)
            : null);
    }
}
