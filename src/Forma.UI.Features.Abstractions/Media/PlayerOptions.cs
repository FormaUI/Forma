namespace Forma.UI.Features.Media;

/// <summary>How a caller asked the player to behave, and what the page can tell it about itself.</summary>
public sealed record PlayerOptions
{
    /// <summary>Starts playback without interaction, always muted because browsers block unmuted autoplay.</summary>
    public bool Autoplay { get; init; }

    /// <summary>Asks the hosting service to collect as little as it offers to, by whatever means it provides.</summary>
    public bool LimitTracking { get; init; } = true;

    /// <summary>Address of the page the player renders on, which some services demand the host of.</summary>
    public Uri? PageAddress { get; init; }
}
