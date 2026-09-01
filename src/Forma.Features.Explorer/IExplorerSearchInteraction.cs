namespace Forma.Features.Explorer;

/// <summary>What the components drawing an explorer may ask its search to do.</summary>
public interface IExplorerSearchInteraction
{
    /// <summary>Changes the search text.</summary>
    Task ChangeSearchQueryAsync(string query);
}
