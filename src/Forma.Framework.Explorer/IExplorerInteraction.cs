namespace Forma.Framework.Explorer;

using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Navigation;
using Forma.Framework.Explorer.Selection;

/// <summary>Everything components drawing an explorer may ask it to do, except what it may ask of its files.</summary>
public interface IExplorerInteraction :
    IExplorerContentInteraction,
    IExplorerLocationInteraction,
    IExplorerSelectionInteraction,
    IExplorerSearchInteraction;
