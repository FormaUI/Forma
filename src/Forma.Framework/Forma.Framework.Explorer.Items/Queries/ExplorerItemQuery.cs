namespace Forma.Framework.Explorer.Items.Queries;

/// <summary>The window of items an explorer is asking for, described by the state the explorer itself owns.</summary>
/// <param name="Skip">How many matching items to pass over before the window begins.</param>
/// <param name="Take">How many items the window holds.</param>
/// <param name="Search">What the reader typed into the explorer's search box, or null when it is empty.</param>
/// <param name="SortBy">Which field the reader is ordering by.</param>
/// <param name="Direction">Which way that order runs.</param>
public readonly record struct ExplorerItemQuery(
    int Skip,
    int Take,
    string? Search,
    ExplorerSortField SortBy,
    ExplorerSortDirection Direction);
