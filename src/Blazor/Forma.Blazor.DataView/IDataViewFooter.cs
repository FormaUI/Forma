namespace Forma.Blazor.Components.DataView;

using Forma.Blazor.Components.Navigation;

using Microsoft.AspNetCore.Components;

/// <summary>What a data view's footer reads and invokes: the paginator over the pages, the status and the offer of the whole set.</summary>
internal interface IDataViewFooter
{
    /// <summary>Whether the footer has anything to show.</summary>
    bool HasContent { get; }

    /// <summary>Whether the paginator shows.</summary>
    bool ShowsPaginator { get; }

    /// <summary>How many items the paginator counts its pages over.</summary>
    int PagedCount { get; }

    /// <summary>How many items a page holds.</summary>
    int PageSize { get; }

    /// <summary>The page showing, counted from one.</summary>
    int CurrentPage { get; }

    /// <summary>Which parts of the page navigation are shown.</summary>
    PaginationOptions PaginationOptions { get; }

    /// <summary>What the status says.</summary>
    RenderFragment? StatusContent { get; }

    /// <summary>Whether the footer offers selecting the whole set.</summary>
    bool OffersSet { get; }

    /// <summary>How many items the whole set holds.</summary>
    int SetCount { get; }

    /// <summary>Moves to a page.</summary>
    /// <param name="page">The page, counted from one.</param>
    Task MoveToPageAsync(int page);

    /// <summary>Selects every item in the whole set.</summary>
    Task SelectAllAsync();
}
