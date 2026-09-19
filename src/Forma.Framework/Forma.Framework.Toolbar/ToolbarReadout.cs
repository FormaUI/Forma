namespace Forma.Framework.Toolbar;

/// <summary>A non-interactive value a <see cref="FormaToolbar"/> displays.</summary>
public sealed record ToolbarReadout : ToolbarItem
{
    /// <summary>The value currently displayed, localized and formatted by the host.</summary>
    public required string Value { get; init; }

    /// <summary>Whether a screen reader announces the value as it changes.</summary>
    public bool IsAnnounced { get; init; }
}
