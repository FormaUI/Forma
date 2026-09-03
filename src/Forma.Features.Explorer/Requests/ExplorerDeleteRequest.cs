namespace Forma.Features.Explorer.Requests;

using Forma.Features.Explorer.Items;

/// <summary>Request to delete one or more items.</summary>
/// <param name="Items">The items to delete.</param>
public sealed record ExplorerDeleteRequest(IReadOnlyList<IExplorerItem> Items);
