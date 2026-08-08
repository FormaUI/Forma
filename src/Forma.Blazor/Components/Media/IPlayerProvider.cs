namespace Forma.Blazor.Components.Media;

/// <summary>Turns an address into the frame a player loads, or null when the address is not its own.</summary>
internal interface IPlayerProvider
{
    /// <summary>The frame for this address, or null when another provider owns it.</summary>
    ValueTask<PlayerFrame?> GetPlayerFrameAsync(string source, PlayerOptions options, CancellationToken cancellationToken);
}
