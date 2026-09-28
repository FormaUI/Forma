namespace Forma.Blazor.Components.DataView;

/// <summary>Answers a data view's request for a window of items from a store the data view does not hold.</summary>
/// <typeparam name="TItem">The item the provider answers with.</typeparam>
/// <param name="request">The window and sort asked for.</param>
public delegate ValueTask<DataViewPage<TItem>> DataViewItemsProvider<TItem>(DataViewRequest<TItem> request);
