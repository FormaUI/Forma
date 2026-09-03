namespace Forma.Features.Explorer.Content.Queries;

/// <summary>Answers an explorer with one window of items at a time, for collections too large to hold.</summary>
public interface IExplorerItemsProvider
{
    /// <summary>The window the query asks for. Anything the explorer does not own, such as the folder being browsed, belongs to the implementation.</summary>
    ValueTask<ExplorerItemPage> GetItemsAsync(ExplorerItemQuery query, CancellationToken cancellationToken);
}
