namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Blazor.Components.Explorer;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;
using Forma.Framework.Explorer.Items.Queries;
using Forma.Framework.Resources;

/// <summary>An in-memory store standing in for one the explorer would reach over IO.</summary>
public sealed class SampleMediaStore(IReadOnlyList<SampleResource> resources, TimeSpan latency)
{
    private readonly List<SampleResource> _resources = [.. resources];

    /// <summary>How many windows have been asked for, so a demo can show that only what is seen is fetched.</summary>
    public int QueryCount { get; private set; }

    /// <summary>The last window asked for, so a demo can show what the explorer requested.</summary>
    public ExplorerItemQuery? LastQuery { get; private set; }

    /// <summary>Raised once a window has been answered, so a demo can redraw its own reporting.</summary>
    public Func<Task>? Queried { get; set; }

    /// <summary>Answers for one window of the named folder, or of everything when none is named, after a delay standing in for the round trip.</summary>
    public async ValueTask<ExplorerItemPage> GetPageAsync(string? folder, ExplorerItemQuery query, CancellationToken cancellationToken)
    {
        QueryCount++;
        LastQuery = query;

        await Task.Delay(latency, cancellationToken).ConfigureAwait(false);

        // The sidebar's root is "All media" rather than an unfiled folder, so no folder means no filter.
        IEnumerable<SampleResource> matching = folder is null
            ? _resources
            : _resources.Where(resource => resource.Folder == folder);

        if (query.Search is { Length: > 0 } search)
        {
            matching = matching.Where(resource => resource.Resource.Name.Contains(search, StringComparison.OrdinalIgnoreCase));
        }

        var descending = query.Direction == ExplorerSortDirection.Descending;

        matching = (query.SortBy, descending) switch
        {
            (ExplorerSortField.Size, false) => matching.OrderBy(resource => resource.Resource.Size ?? 0),
            (ExplorerSortField.Size, true) => matching.OrderByDescending(resource => resource.Resource.Size ?? 0),
            (ExplorerSortField.Kind, false) => matching.OrderBy(resource => resource.Resource.GetType().Name, StringComparer.Ordinal),
            (ExplorerSortField.Kind, true) => matching.OrderByDescending(resource => resource.Resource.GetType().Name, StringComparer.Ordinal),
            (ExplorerSortField.DateAdded, false) => matching.OrderBy(resource => resource.Resource.CreatedAt),
            (ExplorerSortField.DateAdded, true) => matching.OrderByDescending(resource => resource.Resource.CreatedAt),
            (ExplorerSortField.DateModified, false) => matching.OrderBy(resource => resource.Resource.ModifiedAt),
            (ExplorerSortField.DateModified, true) => matching.OrderByDescending(resource => resource.Resource.ModifiedAt),
            (_, true) => matching.OrderByDescending(resource => resource.Resource.Name, StringComparer.OrdinalIgnoreCase),
            _ => matching.OrderBy(resource => resource.Resource.Name, StringComparer.OrdinalIgnoreCase)
        };

        var ordered = matching.ToList();
        var page = new ExplorerItemPage(
            ExplorerItem.ForAll(ordered.Skip(query.Skip).Take(query.Take).Select(resource => resource.Resource)),
            ordered.Count);

        if (Queried is { } queried)
        {
            await queried().ConfigureAwait(false);
        }

        return page;
    }

    /// <summary>Everything held, for a demo that hands the explorer its collection instead of a provider.</summary>
    public IReadOnlyList<ResourceInfo> GetAll()
        => [.. _resources.Select(held => held.Resource)];

    /// <summary>How many resources the folder holds.</summary>
    public int CountIn(string folder)
        => _resources.Count(held => held.Folder == folder);

    /// <summary>How large the folder's contents are together.</summary>
    public long GetSize(string folder)
        => _resources.Where(held => held.Folder == folder).Sum(held => held.Resource.Size ?? 0);

    /// <summary>Moves the resources into the folder, or to the root when none is named.</summary>
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

    /// <summary>Removes everything the folder held, as deleting a folder takes its contents with it.</summary>
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
/// <param name="Resource">The resource itself.</param>
/// <param name="Folder">The source of the folder it belongs to, or null when it sits at the root.</param>
public sealed record SampleResource(FileResourceInfo Resource, string? Folder);
