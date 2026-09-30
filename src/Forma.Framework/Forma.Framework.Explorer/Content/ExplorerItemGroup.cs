namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Items;

/// <summary>A group of items that share a band.</summary>
/// <param name="Band">The band the items fall in.</param>
/// <param name="KindLabel">The label of the kind the items of a <see cref="ExplorerGroupBand.Kind"/> group share.</param>
/// <param name="Items">The items in the group.</param>
public sealed record ExplorerItemGroup(ExplorerGroupBand Band, string? KindLabel, IReadOnlyList<ICatalogItem> Items)
{
    /// <summary>The key identifying the group.</summary>
    public string Key
        => GetKey(Band, KindLabel);

    /// <summary>The key of the group with <paramref name="band"/> and <paramref name="kindLabel"/>.</summary>
    public static string GetKey(ExplorerGroupBand band, string? kindLabel)
        => band == ExplorerGroupBand.Kind ? $"kind:{kindLabel}" : band.ToString();
}
