namespace Forma.Framework.Browser.Interop.Focus;

/// <summary>Whether the reader is within an element, and which of its items holds the focus.</summary>
/// <param name="IsWithin">Whether the reader is within the element, its focus inside it or still held by it.</param>
/// <param name="Item">The value of the item attribute on the item nearest the focused element.</param>
public sealed record FocusState(bool IsWithin, string? Item = null);
