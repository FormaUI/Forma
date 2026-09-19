namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Items.Queries;

/// <summary>A run of items that share a band, in the order they are listed.</summary>
/// <param name="Band">The band the items fall in.</param>
/// <param name="KindLabel">The kind the items share, for a <see cref="ExplorerGroupBand.Kind"/> group; null for every other band.</param>
/// <param name="Items">The items in the group, in list order.</param>
public sealed record ExplorerItemGroup(ExplorerGroupBand Band, string? KindLabel, IReadOnlyList<IExplorerItem> Items)
{
    /// <summary>A key naming the group among the groups of one list.</summary>
    public string Key
        => GetKey(Band, KindLabel);

    /// <summary>The key of the group in <paramref name="band"/>, named <paramref name="kindLabel"/> when the band is a kind.</summary>
    public static string GetKey(ExplorerGroupBand band, string? kindLabel)
        => band == ExplorerGroupBand.Kind ? $"kind:{kindLabel}" : band.ToString();
}
