namespace Forma.Framework.Common.Interop.Behaviours;

/// <summary>What a <see cref="Scroll"/> attachment does for its target; each part is off until given.</summary>
public sealed record ScrollBehaviours
{
    /// <summary>Watches how far the target can still scroll and how far its frozen parts reach, reported to the owner.</summary>
    public ScrollBoundary? Boundary { get; init; }

    /// <summary>Treats the target as a sentinel, telling the owner as it comes into view.</summary>
    public ScrollSentinel? Sentinel { get; init; }
}
