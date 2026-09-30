namespace Forma.Framework.Catalog.Requests;


/// <summary>A request to drop items into a target folder.</summary>
/// <param name="Items">The items being dropped.</param>
/// <param name="TargetFolder">The target folder, where null is the top level.</param>
public sealed record CatalogDropRequest(IReadOnlyList<ICatalogItem> Items, ICatalogFolderItem? TargetFolder);
