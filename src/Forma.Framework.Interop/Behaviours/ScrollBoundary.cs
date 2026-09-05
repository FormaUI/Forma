namespace Forma.Framework.Interop.Behaviours;

/// <summary>The watch on how far a target can still scroll along <paramref name="Axis"/>, and how far the parts matching <paramref name="FrozenSelector"/> reach in from its start.</summary>
/// <param name="Axis">The axis the target scrolls along.</param>
/// <param name="FrozenSelector">The selector of the parts that stay at the target's start, or <see langword="null"/> when none do.</param>
public sealed record ScrollBoundary(ScrollAxis Axis = ScrollAxis.Inline, string? FrozenSelector = null);
