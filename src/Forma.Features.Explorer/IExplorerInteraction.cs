namespace Forma.Features.Explorer;

/// <summary>Everything components drawing an explorer may ask it to do.</summary>
public interface IExplorerInteraction :
    IExplorerContentInteraction,
    IExplorerLocationInteraction,
    IExplorerSelectionInteraction,
    IExplorerSearchInteraction;
