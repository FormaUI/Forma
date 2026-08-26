namespace Forma.UI.Features.Explorer;

using Forma.Framework.Resources;

/// <summary>The media a <see cref="ExplorerResourceQuery"/> asked for, and how much there is to page through.</summary>
/// <param name="Resources">The window itself, which may be shorter than asked for at the end of the collection.</param>
/// <param name="TotalCount">How many resources match in all, or null when the store cannot say without counting them.</param>
public sealed record ExplorerResourcePage(IReadOnlyList<ResourceInfo> Resources, int? TotalCount)
{
    /// <summary>Nothing to show.</summary>
    public static ExplorerResourcePage Empty { get; } = new([], 0);
}
