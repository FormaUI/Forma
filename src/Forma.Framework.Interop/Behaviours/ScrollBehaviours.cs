namespace Forma.Framework.Interop.Behaviours;

/// <summary>What a <see cref="Scroll"/> attachment does for its target; each part is off until given.</summary>
public sealed record ScrollBehaviours
{
    /// <summary>Watches how far the target can still scroll and how far its frozen parts reach, reported to the owner.</summary>
    public ScrollBoundary? Boundary { get; init; }

    /// <summary>Answers a wheel turned over the target.</summary>
    public ScrollWheel? Wheel { get; init; }

    /// <summary>Treats the target as a sentinel, telling the owner as it comes into view.</summary>
    public ScrollSentinel? Sentinel { get; init; }

    /// <summary>Keeps the arrow, Home and End keys pressed on the target from scrolling the page.</summary>
    public bool GuardsNavigation { get; init; }

    /// <summary>Guarding the navigation keys alone.</summary>
    public static ScrollBehaviours NavigationGuard { get; } = new() { GuardsNavigation = true };
}
