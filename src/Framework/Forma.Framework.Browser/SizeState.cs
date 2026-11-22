namespace Forma.Framework.Browser;

/// <summary>An element's size along its writing mode's axes, its border box as laid out, in CSS pixels.</summary>
/// <param name="InlineSize">The size along the inline axis, the width in a horizontal writing mode.</param>
/// <param name="BlockSize">The size along the block axis, the height in a horizontal writing mode.</param>
public sealed record SizeState(double InlineSize, double BlockSize);
