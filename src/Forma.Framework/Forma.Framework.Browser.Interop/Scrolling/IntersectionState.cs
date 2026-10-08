namespace Forma.Framework.Browser.Interop.Scrolling;

/// <summary>Whether an element is in view of its root.</summary>
/// <param name="IsIntersecting">Whether the element intersects its root, grown by the root's margin.</param>
public sealed record IntersectionState(bool IsIntersecting);
