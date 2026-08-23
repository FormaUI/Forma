namespace Forma.UI.Blazor.Components.Bars;

/// <summary>A non-interactive value a <see cref="FormaToolbar"/> displays.</summary>
public sealed record FormaToolbarReadout : FormaToolbarItem
{
    /// <summary>The value currently displayed, localized and formatted by the host.</summary>
    public required string Value { get; init; }

    /// <summary>Whether a screen reader announces the value as it changes.</summary>
    public bool IsAnnounced { get; init; }
}
