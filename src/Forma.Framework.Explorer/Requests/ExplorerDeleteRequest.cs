namespace Forma.Framework.Explorer.Requests;

using Forma.Framework.Explorer.Items;

/// <summary>Request to delete one or more items.</summary>
/// <param name="Items">The items to delete.</param>
public sealed record ExplorerDeleteRequest(IReadOnlyList<IExplorerItem> Items);
