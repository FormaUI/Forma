namespace Forma.Framework.Browser.Interop.Scrolling;

/// <summary>How far a scroll container can still move along its axis, how far its frozen parts reach, and how much of it is in view.</summary>
/// <param name="CanScrollBack">Whether the content can still move toward the start.</param>
/// <param name="CanScrollForward">Whether the content can still move toward the end.</param>
/// <param name="FrozenExtent">How far the frozen parts reach in from the start along the axis, in pixels.</param>
/// <param name="CornerExtent">How far the frozen corner reaches across the axis, in pixels.</param>
/// <param name="ViewportExtent">The container's visible extent along the axis, in pixels; null is unmeasured.</param>
public sealed record ScrollState(bool CanScrollBack, bool CanScrollForward, double FrozenExtent, double CornerExtent, double? ViewportExtent);
