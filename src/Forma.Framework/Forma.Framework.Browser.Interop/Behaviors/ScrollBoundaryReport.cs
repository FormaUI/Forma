namespace Forma.Framework.Browser.Interop.Behaviors;

/// <summary>What a boundary watch reports about its target: how far it can still move each way, how far its frozen parts reach in and across, and how much of it is in view.</summary>
/// <param name="CanScrollBack">Whether the content can still move toward the start.</param>
/// <param name="CanScrollForward">Whether the content can still move toward the end.</param>
/// <param name="FrozenExtent">How far the frozen parts reach in from the start along the axis, in pixels.</param>
/// <param name="CornerExtent">How far the frozen corner reaches across the axis, in pixels.</param>
/// <param name="ViewportExtent">The target's visible extent along the axis, in pixels.</param>
public sealed record ScrollBoundaryReport(bool CanScrollBack, bool CanScrollForward, double FrozenExtent, double CornerExtent, double ViewportExtent);
