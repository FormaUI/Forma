namespace Forma.Features.Explorer.Content.Queries;

using Forma.Features.Explorer.Items;

/// <summary>The items a <see cref="ExplorerItemQuery"/> asked for, and how much there is to page through.</summary>
/// <param name="Items">The window itself, which may be shorter than asked for at the end of the collection.</param>
/// <param name="TotalCount">How many items match in all, including any the store returns hidden, or null when the store cannot say without counting them.</param>
public sealed record ExplorerItemPage(IReadOnlyList<IExplorerItem> Items, int? TotalCount)
{
    /// <summary>Nothing to show.</summary>
    public static ExplorerItemPage Empty { get; } = new([], 0);
}
