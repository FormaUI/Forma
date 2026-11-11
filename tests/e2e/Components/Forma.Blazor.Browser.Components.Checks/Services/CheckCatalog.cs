namespace Forma.Blazor.Browser.Components.Checks.Services;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Resources;

/// <summary>A catalog of the checks' picture in folders, with a disabled picture and a document beside them, answering a folder's own items or every folder's.</summary>
public sealed class CheckCatalog : ICatalogProvider
{
    private const string Picture = "_content/Forma.Blazor.Browser.Components.Checks/images/check.svg";

    private static readonly FolderResourceInfo Stages = new("check-catalog:stages") { Name = "Stages" };

    private static readonly FolderResourceInfo Finishes = new("check-catalog:finishes") { Name = "Finishes", Parent = Stages.Source };

    private static readonly FolderResourceInfo Ceremonies = new("check-catalog:ceremonies") { Name = "Ceremonies" };

    // Each item with the source of the folder holding it, where null is the top level.
    private static readonly (ICatalogItem Item, string? Folder)[] Items =
    [
        (CatalogItem.For(Stages), null),
        (CatalogItem.For(Finishes), Stages.Source),
        (CatalogItem.For(Ceremonies), null),
        (CreatePicture("first.svg"), null),
        (CreatePicture("second.svg"), null),
        (CreateDisabledPicture("locked.svg"), null),
        (CatalogItem.For(new DocumentResourceInfo($"{Picture}?name=notes.pdf") { Name = "notes.pdf" }), null),
        (CreatePicture("start.svg"), Stages.Source),
        (CreatePicture("sprint.svg"), Finishes.Source),
        (CreatePicture("podium.svg"), Ceremonies.Source),
    ];

    /// <inheritdoc />
    public ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        // The kinds asked for and the search apply before the slice, so its positions and total count only those.
        List<ICatalogItem> matching = [.. Items
            .Where(entry => query.FolderKey is not { } folder || entry.Folder == folder)
            .Select(entry => entry.Item)
            .Where(item => query.AsksFor(item.GetKind()))
            .Where(item => query.Search is not { Length: > 0 } search || item.Name.Contains(search, StringComparison.OrdinalIgnoreCase))
            .OrderBy(item => item.Name, StringComparer.OrdinalIgnoreCase)];

        if (query.Direction == CatalogSortDirection.Descending)
        {
            matching.Reverse();
        }

        return ValueTask.FromResult(new CatalogPage([.. matching.Skip(query.Skip).Take(query.Take)], matching.Count));
    }

    private static CatalogItem CreatePicture(string name)
        => CatalogItem.For(new ImageResourceInfo($"{Picture}?name={name}") { Name = name });

    private static CatalogItem CreateDisabledPicture(string name)
    {
        var picture = CreatePicture(name);

        return new CatalogItem { Key = picture.Key, Name = picture.Name, Capabilities = picture.Capabilities, Resource = picture.Resource, IsDisabled = true };
    }
}
