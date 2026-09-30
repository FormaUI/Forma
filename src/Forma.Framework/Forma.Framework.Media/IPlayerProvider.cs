namespace Forma.Framework.Media;

/// <summary>Turns an address into the frame a player loads.</summary>
public interface IPlayerProvider
{
    /// <summary>The player frame for the source, or null for an address another provider owns.</summary>
    ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken);
}
