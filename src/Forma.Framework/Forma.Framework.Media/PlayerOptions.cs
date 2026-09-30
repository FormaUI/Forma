namespace Forma.Framework.Media;

/// <summary>How a player behaves, and the page it renders on.</summary>
public sealed record PlayerOptions
{
    /// <summary>Whether playback starts on its own, muted.</summary>
    public bool Autoplay { get; init; }

    /// <summary>Whether the hosting service is asked to limit tracking.</summary>
    public bool LimitTracking { get; init; } = true;

    /// <summary>The address of the page the player renders on.</summary>
    public Uri? PageAddress { get; init; }
}
