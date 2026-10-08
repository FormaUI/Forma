namespace Forma.Framework.Browser.Interop.Focus;

/// <summary>Whether the reader is within an element.</summary>
/// <param name="IsWithin">Whether the reader is within the element, its focus inside it or still held by it.</param>
public sealed record FocusState(bool IsWithin);
