namespace Forma.Framework.Browser.Interop.Behaviors;

/// <summary>What a <see cref="Scroll"/> attachment does for its target.</summary>
public sealed record ScrollBehaviors
{
    /// <summary>The watch on the target as a sentinel coming into view.</summary>
    public ScrollSentinel? Sentinel { get; init; }
}
