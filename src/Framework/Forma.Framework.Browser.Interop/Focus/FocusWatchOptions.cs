namespace Forma.Framework.Browser.Interop.Focus;

/// <summary>What an observation of an element's <see cref="FocusState"/> measures.</summary>
public sealed record FocusWatchOptions
{
    /// <summary>The attribute the element's items carry, whose value <see cref="FocusState.Item"/> reports for the item holding the focus.</summary>
    public string? ItemAttribute { get; init; }
}
