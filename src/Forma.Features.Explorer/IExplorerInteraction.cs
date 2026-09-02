namespace Forma.Features.Explorer;

/// <summary>Everything components drawing an explorer may ask it to do, except what it may ask of its files.</summary>
public interface IExplorerInteraction :
    IExplorerContentInteraction,
    IExplorerLocationInteraction,
    IExplorerSelectionInteraction,
    IExplorerSearchInteraction;
