namespace Forma.Framework.Browser.Interop.Behaviors;

/// <summary>What a <see cref="Scroll"/> attachment does for its target.</summary>
public sealed record ScrollBehaviors
{
    /// <summary>The watch on how far the target can still scroll and how far its frozen parts reach.</summary>
    public ScrollBoundary? Boundary { get; init; }

    /// <summary>The watch on the target as a sentinel coming into view.</summary>
    public ScrollSentinel? Sentinel { get; init; }
}
