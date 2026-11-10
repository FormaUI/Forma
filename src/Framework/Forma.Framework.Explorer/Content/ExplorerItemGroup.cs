namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Catalog;

/// <summary>A group of items that share a band.</summary>
/// <param name="Key">The key identifying the group, the same in every culture and on every day.</param>
/// <param name="Band">The band the items fall in.</param>
/// <param name="KindLabel">The label of the kind the items of a <see cref="ExplorerGroupBand.Kind"/> group share.</param>
/// <param name="Items">The items in the group.</param>
/// <param name="Count">How many items the group holds across the whole result.</param>
public sealed record ExplorerItemGroup(string Key, ExplorerGroupBand Band, string? KindLabel, IReadOnlyList<ICatalogItem> Items, int Count);
