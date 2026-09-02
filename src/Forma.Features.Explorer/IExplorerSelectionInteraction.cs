namespace Forma.Features.Explorer;

/// <summary>What components managing an explorer's selection may ask it to do.</summary>
public interface IExplorerSelectionInteraction
{
    /// <summary>Adds the item to the selection, or removes it when already selected.</summary>
    Task ToggleSelectionAsync(IExplorerItem item);

    /// <summary>Enters or leaves selection mode, clearing the selection on the way out.</summary>
    Task ToggleSelectionModeAsync();

    /// <summary>Clears the current selection.</summary>
    Task ClearSelectionAsync();
}
