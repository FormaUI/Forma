namespace Forma.UI.Blazor.Components.Navigation;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Interop.Behaviours;
using Forma.UI.Blazor.Interop.Operations;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A page-number navigator for splitting a list of items across multiple pages.</summary>
public sealed partial class FormaPagination : ComponentBase, IAsyncDisposable
{
    private const int BoundaryCount = 1;
    private const int SiblingCount = 1;

    private readonly string _pageInputId = $"forma-pagination-input-{Guid.NewGuid():N}";

    private ElementReference _pageInputElement;
    private ImplicitSubmitGuard _submitGuard = default!;
    private Text _text = default!;
    private bool _submitGuardAttached;
    private string? _pageInput;

    /// <summary>Total number of items being paginated.</summary>
    [Parameter, EditorRequired] public int TotalItems { get; set; }

    /// <summary>Number of items shown per page.</summary>
    [Parameter, EditorRequired] public int PageSize { get; set; }

    /// <summary>The current page, 1-based. Values outside the available range are clamped.</summary>
    [Parameter, EditorRequired] public int CurrentPage { get; set; }

    /// <summary>Fired when the user selects a different page.</summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>Which parts of the pager are shown.</summary>
    [Parameter] public FormaPaginationOptions Options { get; set; } = FormaPaginationOptions.Default;

    /// <summary>Additional attributes applied to the navigation element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocale<FormaPagination> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private int PageCount => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));

    private bool ShowsInputBox => PageCount > 1 && Options.EnableNavigationInput;

    // The page actually shown. A caller's page can fall outside the range whenever the
    // item count shrinks under it — a filter narrowing, a delete, a hand-edited query
    // string — none of which are programming errors, so they resolve to the nearest page.
    private int ActivePage => Math.Clamp(CurrentPage, 1, PageCount);

    private IReadOnlyList<int?> PageNumbers => BuildPageNumbers(ActivePage, PageCount);

    // The host's accessible name wins, so a page carrying more than one pager can tell
    // its navigation landmarks apart.
    private string NavLabel => AdditionalAttributes.HostValue("aria-label") ?? L["Pagination"].Value;

    // Host class is merged rather than splatted, so supplying one never drops
    // the component's own class.
    private string CssClass
        => AdditionalAttributes.MergeClass("forma-pagination");

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _submitGuard = new ImplicitSubmitGuard(JS);
        _text = new Text(JS);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);
        ArgumentOutOfRangeException.ThrowIfNegative(TotalItems);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PageSize, 0);
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (!ShowsInputBox)
        {
            // The box went away, so the listener went with it.
            _submitGuardAttached = false;
            await _submitGuard.DetachAsync();
            return;
        }

        if (_submitGuardAttached)
        {
            return;
        }

        _submitGuardAttached = await _submitGuard.AttachAsync(_pageInputElement);
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _submitGuard.DisposeAsync();
        await _text.DisposeAsync();
    }

    // Windowed page-number layout (boundary pages + a sibling range around the
    // current page, with a null placeholder standing in for a collapsed gap).
    private static IReadOnlyList<int?> BuildPageNumbers(int currentPage, int pageCount)
    {
        var maxWithoutGaps = (BoundaryCount * 2) + (SiblingCount * 2) + 3;

        if (pageCount <= maxWithoutGaps)
        {
            return Enumerable.Range(1, pageCount).Select(page => (int?)page).ToArray();
        }

        var pages = new List<int?>();

        for (var page = 1; page <= BoundaryCount; page++)
        {
            pages.Add(page);
        }

        var siblingsStart = Math.Max(currentPage - SiblingCount, BoundaryCount + 1);
        var siblingsEnd = Math.Min(currentPage + SiblingCount, pageCount - BoundaryCount);

        if (siblingsStart > BoundaryCount + 1)
        {
            pages.Add(null);
        }

        for (var page = siblingsStart; page <= siblingsEnd; page++)
        {
            pages.Add(page);
        }

        if (siblingsEnd < pageCount - BoundaryCount)
        {
            pages.Add(null);
        }

        for (var page = pageCount - BoundaryCount + 1; page <= pageCount; page++)
        {
            pages.Add(page);
        }

        return pages;
    }

    private async Task GoToPageAsync(int page)
    {
        if (page < 1 || page > PageCount || page == ActivePage)
        {
            return;
        }

        await OnPageChanged.InvokeAsync(page);
    }

    private void OnJumpInput(ChangeEventArgs args)
        => _pageInput = args.Value?.ToString();

    private async Task OnJumpKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "Enter")
        {
            await JumpAsync();
        }
    }

    // A typed page outside the range settles on the nearest one, matching how an
    // out-of-range CurrentPage behaves. Enter or Go consumes whatever is in the box,
    // so an unparseable entry clears rather than sticking around.
    private async Task JumpAsync()
    {
        var entry = await TakePageEntryAsync();
        _pageInput = null;

        if (!int.TryParse(entry, NumberStyles.Integer, CultureInfo.CurrentCulture, out var page))
        {
            return;
        }

        await GoToPageAsync(Math.Clamp(page, 1, PageCount));
    }

    // Typing that lands before the circuit connects raises no input event, leaving the
    // tracked value empty while the box holds the number. The box is the fallback.
    private async ValueTask<string?> TakePageEntryAsync()
    {
        if (!string.IsNullOrEmpty(_pageInput))
        {
            return _pageInput;
        }

        return await _text.TakeAsync(_pageInputElement);
    }
}
