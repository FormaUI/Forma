namespace Forma.Framework.Explorer.Content.Queries;

using Forma.Framework.Catalog.Queries;

/// <summary>What the components drawing an explorer may ask its search to do.</summary>
public interface IExplorerSearchInteraction
{
    /// <summary>Sets the search query.</summary>
    Task SetSearchQueryAsync(string query);
}
