namespace Forma.Blazor.Components.Composer;

/// <summary>The composer's own chrome state, independent of the document any editor holds.</summary>
internal sealed class ComposerChrome
{
    /// <summary>Whether the composer fills the viewport.</summary>
    public bool IsMaximized { get; private set; }

    /// <summary>Whether the formatting bar that follows the text selection is shown.</summary>
    public bool IsSelectionBarShown { get; private set; } = true;

    /// <summary>Flips whether the composer fills the viewport.</summary>
    public void ToggleMaximized()
        => IsMaximized = !IsMaximized;

    /// <summary>Flips whether the selection bar is shown.</summary>
    public void ToggleSelectionBar()
        => IsSelectionBarShown = !IsSelectionBarShown;
}
