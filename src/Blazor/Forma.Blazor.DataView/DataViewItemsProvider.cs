namespace Forma.Blazor.Components.DataView;

/// <summary>Answers a data view's request for a slice of items.</summary>
/// <typeparam name="TItem">The item the provider answers with.</typeparam>
/// <param name="request">The slice and sort asked for.</param>
public delegate ValueTask<DataViewPage<TItem>> DataViewItemsProvider<TItem>(DataViewRequest<TItem> request);
