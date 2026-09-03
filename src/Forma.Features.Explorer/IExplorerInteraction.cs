namespace Forma.Features.Explorer;

using Forma.Features.Explorer.Content;
using Forma.Features.Explorer.Content.Queries;
using Forma.Features.Explorer.Navigation;
using Forma.Features.Explorer.Selection;

/// <summary>Everything components drawing an explorer may ask it to do, except what it may ask of its files.</summary>
public interface IExplorerInteraction :
    IExplorerContentInteraction,
    IExplorerLocationInteraction,
    IExplorerSelectionInteraction,
    IExplorerSearchInteraction;
