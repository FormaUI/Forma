namespace Forma.Features.Explorer;
/// <summary>Answers an explorer with one window of media at a time, for collections too large to hold.</summary>
public interface IExplorerResourcesProvider
{
    /// <summary>The window the query asks for. Anything the explorer does not own, such as the folder being browsed, belongs to the implementation.</summary>
    ValueTask<ExplorerResourcePage> GetResourcesAsync(ExplorerResourceQuery query, CancellationToken cancellationToken);
}
