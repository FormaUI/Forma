namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Blazor.Components.Explorer;
using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Resources;

/// <summary>An in-memory media store answering after a set latency.</summary>
public sealed class SampleMediaStore(IReadOnlyList<SampleResource> resources, TimeSpan latency) : ICatalogProvider
{
    private readonly List<SampleResource> _resources = [.. resources];

    /// <summary>How many slices have been asked for.</summary>
    public int QueryCount { get; private set; }

    /// <summary>The last slice asked for.</summary>
    public CatalogQuery? LastQuery { get; private set; }

    /// <summary>Raised once a slice has been answered.</summary>
    public Func<Task>? Queried { get; set; }

    /// <inheritdoc />
    public async ValueTask<CatalogPage> GetItemsAsync(CatalogQuery query, CancellationToken cancellationToken)
    {
        QueryCount++;
        LastQuery = query;

        await Task.Delay(latency, cancellationToken).ConfigureAwait(false);

        IEnumerable<SampleResource> matching = query.FolderKey is not { } folder
            ? _resources
            : _resources.Where(resource => resource.Folder == folder);

        if (query.Search is { Length: > 0 } search)
        {
            matching = matching.Where(resource => resource.Resource.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var descending = query.Direction == CatalogSortDirection.Descending;

        // The sort is stable, so the resources it ties keep the order of their keys.
        matching = matching.OrderBy(resource => resource.Resource.Source, StringComparer.Ordinal);

        matching = (query.SortBy, descending) switch
        {
            (CatalogSortField.Size, false) => matching.OrderBy(resource => resource.Resource.Size ?? 0),
            (CatalogSortField.Size, true) => matching.OrderByDescending(resource => resource.Resource.Size ?? 0),
            (CatalogSortField.Kind, false) => matching.OrderBy(resource => resource.Resource.GetType().Name, StringComparer.Ordinal),
            (CatalogSortField.Kind, true) => matching.OrderByDescending(resource => resource.Resource.GetType().Name, StringComparer.Ordinal),
            (CatalogSortField.DateAdded, false) => matching.OrderBy(resource => resource.Resource.CreatedAt),
            (CatalogSortField.DateAdded, true) => matching.OrderByDescending(resource => resource.Resource.CreatedAt),
            (CatalogSortField.DateModified, false) => matching.OrderBy(resource => resource.Resource.ModifiedAt),
            (CatalogSortField.DateModified, true) => matching.OrderByDescending(resource => resource.Resource.ModifiedAt),
            (_, true) => matching.OrderByDescending(resource => resource.Resource.Name, StringComparer.OrdinalIgnoreCase),
            _ => matching.OrderBy(resource => resource.Resource.Name, StringComparer.OrdinalIgnoreCase)
        };

        // The groups order before the sort, which the stable sort keeps within each group.
        if (query.Grouping is { } grouping)
        {
            matching = matching.OrderBy(resource => grouping.IndexOf(CatalogItem.For(resource.Resource)));
        }

        var ordered = matching.ToList();
        var page = new CatalogPage(
            CatalogItem.ForAll(ordered.Skip(query.Skip).Take(query.Take).Select(resource => resource.Resource)),
            ordered.Count)
        {
            GroupSizes = query.IncludesGroupSizes ? query.Grouping?.CountGroups(CatalogItem.ForAll(ordered.Select(resource => resource.Resource))) : null
        };

        if (Queried is { } queried)
        {
            await queried().ConfigureAwait(false);
        }

        return page;
    }

    /// <summary>The resources held.</summary>
    public IReadOnlyList<ResourceInfo> GetAll()
        => [.. _resources.Select(held => held.Resource)];

    /// <summary>How many resources the folder holds.</summary>
    public int CountIn(string folder)
        => _resources.Count(held => held.Folder == folder);

    /// <summary>How large the folder's contents are together.</summary>
    public long GetSize(string folder)
        => _resources.Where(held => held.Folder == folder).Sum(held => held.Resource.Size ?? 0);

    /// <summary>Moves the resources into the folder, where null is the root.</summary>
    public void Move(IReadOnlyList<ResourceInfo> resources, string? folder)
    {
        ArgumentNullException.ThrowIfNull(resources);

        foreach (var moved in resources)
        {
            var index = IndexOf(moved);

            if (index >= 0)
            {
                _resources[index] = _resources[index] with { Folder = folder };
            }
        }
    }

    /// <summary>Removes the resources.</summary>
    public void Remove(IReadOnlyList<ResourceInfo> resources)
    {
        ArgumentNullException.ThrowIfNull(resources);

        foreach (var removed in resources)
        {
            _resources.RemoveAll(held => held.Resource.Source == removed.Source);
        }
    }

    /// <summary>Removes the resources the folder holds.</summary>
    public void RemoveFolder(string folder)
        => _resources.RemoveAll(held => held.Folder == folder);

    /// <summary>Renames the resource.</summary>
    public void Rename(ResourceInfo resource, string name)
    {
        ArgumentNullException.ThrowIfNull(resource);

        var index = IndexOf(resource);

        if (index >= 0)
        {
            _resources[index] = _resources[index] with { Resource = _resources[index].Resource with { Name = name } };
        }
    }

    private int IndexOf(ResourceInfo resource)
        => _resources.FindIndex(held => held.Resource.Source == resource.Source);
}

/// <summary>A stored resource together with the folder holding it.</summary>
/// <param name="Resource">The resource.</param>
/// <param name="Folder">The source of the folder holding the resource, where null is the root.</param>
public sealed record SampleResource(FileResourceInfo Resource, string? Folder);
