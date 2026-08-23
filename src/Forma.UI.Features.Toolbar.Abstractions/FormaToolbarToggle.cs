namespace Forma.UI.Features.Toolbar;

/// <summary>A <see cref="FormaToolbarAction"/> that reflects an on/off state.</summary>
public sealed record FormaToolbarToggle : FormaToolbarAction
{
    /// <summary>Whether the toggle is currently on.</summary>
    public bool IsActive { get; init; }
}
