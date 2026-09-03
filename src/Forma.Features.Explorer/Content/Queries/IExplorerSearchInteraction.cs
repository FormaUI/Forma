namespace Forma.Features.Explorer.Content.Queries;

/// <summary>What the components drawing an explorer may ask its search to do.</summary>
public interface IExplorerSearchInteraction
{
    /// <summary>Sets the search text.</summary>
    Task SetSearchQueryAsync(string query);
}
