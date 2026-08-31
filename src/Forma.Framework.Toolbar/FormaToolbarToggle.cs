namespace Forma.Framework.Toolbar;

using Forma.Framework.Icons;

/// <summary>A <see cref="FormaToolbarAction"/> that reflects an on/off state.</summary>
public sealed record FormaToolbarToggle : FormaToolbarAction
{
    /// <summary>Whether the toggle is currently on.</summary>
    public bool IsActive { get; init; }

    /// <summary>The alternate title shown while on, or null to keep <see cref="FormaToolbarItem.Title"/> unchanged.</summary>
    public string? AlternateTitle { get; init; }

    /// <summary>The alternate icon shown while on, or null to keep <see cref="FormaToolbarItem.Icon"/> unchanged.</summary>
    public Icon? AlternateIcon { get; init; }
}
