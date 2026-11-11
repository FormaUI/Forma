namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Catalog;
using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;

/// <summary>What components presenting an explorer's current content may read.</summary>
public interface IExplorerContentView
{
    /// <summary>The field the content is sorted by.</summary>
    CatalogSortField SortField { get; }

    /// <summary>The direction the content is sorted in.</summary>
    CatalogSortDirection SortDirection { get; }

    /// <summary>Which layout the content is shown in.</summary>
    ExplorerViewMode ViewMode { get; }

    /// <summary>Which optional columns the details view shows beside the name.</summary>
    ExplorerDetailsColumns DetailsColumns { get; }

    /// <summary>Whether the details view shows its column header.</summary>
    bool ShowsDetailsHeader { get; }

    /// <summary>The field the shown items are gathered into groups by, or none.</summary>
    ExplorerGroupField GroupField { get; }

    /// <summary>The items currently shown.</summary>
    IReadOnlyList<ICatalogItem> VisibleItems { get; }

    /// <summary>Whether the content is being read.</summary>
    bool IsFetching { get; }

    /// <summary>How many items match the current query, or null when unknown.</summary>
    int? MatchingCount { get; }
}
