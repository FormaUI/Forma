namespace Forma.Framework.Explorer;

using Forma.Framework.Catalog.Navigation;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Navigation;
using Forma.Framework.Explorer.Selection;

/// <summary>What components drawing an explorer may ask of its content, location, selection and search.</summary>
public interface IExplorerInteraction :
    IExplorerContentInteraction,
    IExplorerLocationInteraction,
    IExplorerSelectionInteraction,
    IExplorerSearchInteraction;
