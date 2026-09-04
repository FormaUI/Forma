namespace Forma.Framework.Explorer.Content;

using Forma.Framework.Explorer.Content.Queries;
using Forma.Framework.Explorer.Items;

/// <summary>What components presenting an explorer's current content may read.</summary>
public interface IExplorerContentView
{
    /// <summary>The field the content is sorted by.</summary>
    ExplorerSortField SortField { get; }

    /// <summary>The direction that order runs in.</summary>
    ExplorerSortDirection SortDirection { get; }

    /// <summary>Which layout the content is shown in.</summary>
    ExplorerViewMode ViewMode { get; }

    /// <summary>The items currently shown.</summary>
    IReadOnlyList<IExplorerItem> VisibleItems { get; }

    /// <summary>Whether a provider fetch is in flight.</summary>
    bool IsFetching { get; }

    /// <summary>How many items match the current query, or null when unknown.</summary>
    int? MatchingCount { get; }

    /// <summary>How the currently shown items are paged.</summary>
    ExplorerPagingInfo Paging { get; }
}
