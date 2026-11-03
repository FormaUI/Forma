namespace Forma.Framework.Toolbar;

using Forma.Framework.Icons;

/// <summary>A <see cref="ToolbarAction"/> that reflects an on/off state.</summary>
public sealed record ToolbarToggle : ToolbarAction
{
    /// <summary>Whether the toggle is currently on.</summary>
    public bool IsActive { get; init; }

    /// <summary>The alternate title shown while the toggle is on.</summary>
    public string? AlternateTitle { get; init; }

    /// <summary>The alternate icon shown while the toggle is on.</summary>
    public Icon? AlternateIcon { get; init; }
}
