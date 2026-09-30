namespace Forma.Framework.Catalog.Requests;


/// <summary>A request to drop a folder into a target folder.</summary>
/// <param name="SourceFolder">The folder being dropped.</param>
/// <param name="TargetFolder">The target folder, where null is the top level.</param>
public sealed record CatalogFolderDropRequest(ICatalogFolderItem SourceFolder, ICatalogFolderItem? TargetFolder);
