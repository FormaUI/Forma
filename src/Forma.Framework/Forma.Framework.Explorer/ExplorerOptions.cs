namespace Forma.Framework.Explorer;

using Forma.Framework.Catalog.Queries;
using Forma.Framework.Explorer.Content;
using Forma.Framework.Explorer.Content.Queries;

/// <summary>How an explorer presents itself: which surfaces it shows, and how it moves through a collection.</summary>
public sealed record ExplorerOptions
{
    /// <summary>The standard presentation.</summary>
    public static ExplorerOptions Default { get; } = new();

    /// <summary>Whether the explorer is presented as dialog content.</summary>
    public bool AsDialog { get; init; }

    /// <summary>Whether the explorer opens in selection mode.</summary>
    public bool SelectOnOpen { get; init; }

    /// <summary>The maximum number of items that may be selected at once (0 for unlimited).</summary>
    public int MaxSelection { get; init; }

    /// <summary>Whether the reader can resize the explorer.</summary>
    public bool Resizable { get; init; }

    /// <summary>Whether the folder tree sidebar is shown.</summary>
    public bool ShowSidebar { get; init; } = true;

    /// <summary>Whether each breadcrumb offers a menu of that folder's children.</summary>
    public bool ShowFolderMenu { get; init; } = true;

    /// <summary>How the explorer appends further pages to the list.</summary>
    public ExplorerInfiniteScrollMode InfiniteScroll { get; init; } = ExplorerInfiniteScrollMode.Off;

    /// <summary>Which optional columns the details view initially shows beside the name.</summary>
    public ExplorerDetailsColumns DetailsColumns { get; init; } = ExplorerDetailsColumns.Kind | ExplorerDetailsColumns.Size | ExplorerDetailsColumns.Modified;

    /// <summary>Whether the details view initially shows its column header.</summary>
    public bool ShowDetailsHeader { get; init; } = true;

    /// <summary>How long typing pauses before a catalog is queried.</summary>
    public TimeSpan SearchDebounceDelay { get; init; } = TimeSpan.FromMilliseconds(300);
}
