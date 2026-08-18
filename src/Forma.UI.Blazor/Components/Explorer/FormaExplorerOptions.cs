namespace Forma.UI.Blazor.Components.Explorer;

/// <summary>How a <see cref="FormaExplorer"/> presents itself: which surfaces it shows, and how it moves through a collection.</summary>
public sealed record FormaExplorerOptions
{
    /// <summary>The standard presentation.</summary>
    public static FormaExplorerOptions Default { get; } = new();

    /// <summary>Whether the explorer is presented as dialog content, without a frame of its own.</summary>
    public bool AsDialog { get; init; }

    /// <summary>Whether the reader can resize the explorer.</summary>
    public bool Resizable { get; init; }

    /// <summary>Whether the folder tree sidebar is shown.</summary>
    public bool ShowSidebar { get; init; } = true;

    /// <summary>Whether each breadcrumb offers a menu of that folder's children.</summary>
    public bool ShowFolderMenu { get; init; } = true;

    /// <summary>Whether reaching the end of the list appends the next page, and whether a button does so instead.</summary>
    public ExplorerInfiniteScrollMode InfiniteScroll { get; init; } = ExplorerInfiniteScrollMode.Off;

    /// <summary>Which optional columns the details view shows beside the name.</summary>
    public ExplorerDetailsColumns DetailsColumns { get; init; } = ExplorerDetailsColumns.Kind | ExplorerDetailsColumns.Size | ExplorerDetailsColumns.Modified;

    /// <summary>How long typing pauses before a catalog is asked again; a held collection filters immediately.</summary>
    public TimeSpan SearchDebounce { get; init; } = TimeSpan.FromMilliseconds(300);
}
