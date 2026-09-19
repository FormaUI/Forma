namespace Forma.Framework.Toolbar;

using Forma.Framework.Icons;

/// <summary>A <see cref="ToolbarAction"/> that reflects an on/off state.</summary>
public sealed record ToolbarToggle : ToolbarAction
{
    /// <summary>Whether the toggle is currently on.</summary>
    public bool IsActive { get; init; }

    /// <summary>The alternate title shown while on, or null to keep <see cref="ToolbarItem.Title"/> unchanged.</summary>
    public string? AlternateTitle { get; init; }

    /// <summary>The alternate icon shown while on, or null to keep <see cref="ToolbarItem.Icon"/> unchanged.</summary>
    public Icon? AlternateIcon { get; init; }
}
