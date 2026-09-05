namespace Forma.Framework.Interop.Behaviours;

/// <summary>The parts of a container's content that a step comes to rest on, matched by <paramref name="Selector"/> and drawn up to <paramref name="Inset"/> pixels from the container's start.</summary>
/// <param name="Selector">The selector of the parts scrolling aligns to.</param>
/// <param name="Inset">How far in from the container's start a stop comes to rest, the extent of any frozen part standing there.</param>
public sealed record ScrollStops(string Selector, double Inset = 0);
