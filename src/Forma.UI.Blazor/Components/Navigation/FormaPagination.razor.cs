namespace Forma.UI.Blazor.Components.Navigation;

using System.Globalization;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Components.Inputs;
using Forma.UI.Blazor.Interop.Behaviours;
using Forma.UI.Blazor.Interop.Operations;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A page-number navigator for splitting a list of items across multiple pages.</summary>
public sealed partial class FormaPagination : ComponentBase, IAsyncDisposable
{
    // The navigation steps show the current page and the four after it, then a gap and the last page: seven in all.
    private const int LookAhead = 4;
    private const int StepCount = LookAhead + 3;

    // Beside the steps a navigation button moves a whole window, so pressing it repeatedly walks the list quickly.
    private const int StepsStride = LookAhead;

    private readonly string _navigationInputId = $"forma-pagination-input-{Guid.NewGuid():N}";

    private ImplicitSubmitGuard _submitGuard = default!;
    private WheelScroll _wheelScroll = default!;
    private Text _text = default!;
    private DotNetObjectReference<FormaPagination>? _selfReference;
    private ElementReference _navElement;
    private bool _submitGuardAttached;
    private string? _navigationEntry;
    private int _shownPage;

    /// <summary>Total number of items being paginated.</summary>
    [Parameter, EditorRequired] public int TotalItems { get; set; }

    /// <summary>Number of items shown per page.</summary>
    [Parameter, EditorRequired] public int PageSize { get; set; }

    /// <summary>The current page, 1-based. Values outside the available range are clamped.</summary>
    [Parameter, EditorRequired] public int CurrentPage { get; set; }

    /// <summary>Fired when the user selects a different page.</summary>
    [Parameter] public EventCallback<int> OnPageChanged { get; set; }

    /// <summary>How the reader moves between pages.</summary>
    [Parameter] public FormaPaginationOptions Options { get; set; } = FormaPaginationOptions.Default;

    /// <summary>Additional attributes applied to the navigation element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocale<FormaPagination> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private int PageCount => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));

    // State alone can call for the input; the width does so too, but that is the stylesheet's decision.
    private bool HasInputByState => Options.NavigationInput || PageCount > Options.MaxPagesWithSteps;

    // The page actually shown. A caller's page can fall outside the range whenever the
    // item count shrinks under it — a filter narrowing, a delete, a hand-edited query
    // string — none of which are programming errors, so they resolve to the nearest page.
    private int ActivePage => Math.Clamp(CurrentPage, 1, PageCount);

    private IReadOnlyList<NavigationStep> NavigationSteps => BuildNavigationSteps(ActivePage, PageCount);

    // The renderer owns the box and disposes it with the pager; the reference is only for reaching its element.
    private FormaTextBox NavigationInput { get; set; } = default!;

    // The host's accessible name wins, so a page carrying more than one pager can tell
    // its navigation landmarks apart.
    private string NavLabel => AdditionalAttributes.HostValue("aria-label") ?? L["Pagination"].Value;

    // Host class is merged rather than splatted, so supplying one never drops
    // the component's own class.
    private string CssClass
        => AdditionalAttributes.MergeClass("forma-pagination");

    // The box is another component's, so its handlers reach the input as splatted attributes.
    private EventCallback<KeyboardEventArgs> NavigationInputKeyDown
        => EventCallback.Factory.Create<KeyboardEventArgs>(this, OnNavigationInputKeyDownAsync);

    private EventCallback<FocusEventArgs> NavigationInputFocus
        => EventCallback.Factory.Create<FocusEventArgs>(this, SelectNavigationEntryAsync);

    private EventCallback<FocusEventArgs> NavigationInputBlur
        => EventCallback.Factory.Create<FocusEventArgs>(this, ShowPageIfEmpty);

    // The box reads "7 of 200" at rest: where the reader is, out of how many.
    private string PageSummary
        => L["{PageNumber} of {PageCount}", ActivePage.ToString(CultureInfo.CurrentCulture), PageCount.ToString("N0", CultureInfo.CurrentCulture)].Value;

    /// <summary>Turns one page in the direction of a wheel gesture; called from the wheel behaviour.</summary>
    [JSInvokable]
    public Task HandleWheelAsync(int direction)
        => InvokeAsync(() => MoveAsync(direction < 0 ? NavigationDirection.Previous : NavigationDirection.Next, 1));

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _submitGuard = new ImplicitSubmitGuard(JS);
        _wheelScroll = new WheelScroll(JS);
        _text = new Text(JS);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);
        ArgumentOutOfRangeException.ThrowIfNegative(TotalItems);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PageSize, 0);

        // The input shows the page the reader is on, so a navigation button visibly moves it; typing replaces it.
        if (_shownPage != ActivePage)
        {
            _shownPage = ActivePage;
            ShowPage();
        }
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (PageCount <= 1)
        {
            // The nav went away, so the listeners went with it.
            _submitGuardAttached = false;
            await _submitGuard.DetachAsync();
            await _wheelScroll.DetachAsync();
            return;
        }

        if (!_submitGuardAttached)
        {
            _submitGuardAttached = await _submitGuard.AttachAsync(NavigationInput.Element);
        }

        if (Options.WheelScrollOnHover && !_wheelScroll.IsAttached)
        {
            _selfReference ??= DotNetObjectReference.Create(this);
            await _wheelScroll.AttachAsync(_navElement, _selfReference);
        }
        else if (!Options.WheelScrollOnHover && _wheelScroll.IsAttached)
        {
            await _wheelScroll.DetachAsync();
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        await _submitGuard.DisposeAsync();
        await _wheelScroll.DisposeAsync();
        await _text.DisposeAsync();
        _selfReference?.Dispose();
    }

    // The window starts at the current page and looks ahead, so the steps read as what comes next; near the end the
    // last seven pages show whole, since a gap there would hide nothing. A list shorter than the strip pads with empty
    // steps, so the first page sits in the same place however many there are.
    private static List<NavigationStep> BuildNavigationSteps(int currentPage, int pageCount)
    {
        var steps = new List<NavigationStep>(StepCount);

        if (pageCount <= StepCount)
        {
            steps.AddRange(Enumerable.Range(1, pageCount).Select(NavigationStep.Page));
            steps.AddRange(Enumerable.Repeat(NavigationStep.Empty, StepCount - pageCount));
            return steps;
        }

        if (currentPage + LookAhead >= pageCount - 2)
        {
            steps.AddRange(Enumerable.Range(pageCount - StepCount + 1, StepCount).Select(NavigationStep.Page));
            return steps;
        }

        steps.AddRange(Enumerable.Range(currentPage, LookAhead + 1).Select(NavigationStep.Page));
        steps.Add(NavigationStep.Gap);
        steps.Add(NavigationStep.Page(pageCount));

        return steps;
    }

    /// <summary>What a position in the navigation steps holds.</summary>
    private enum StepKind
    {
        Page,
        Gap,
        Empty,
    }

    /// <summary>Which way a navigation button moves.</summary>
    private enum NavigationDirection
    {
        Previous,
        Next,
    }

    /// <summary>One position in the navigation steps.</summary>
    /// <param name="Kind">What the step holds.</param>
    /// <param name="Number">The page, when the step holds one.</param>
    private readonly record struct NavigationStep(StepKind Kind, int Number)
    {
        public static readonly NavigationStep Gap = new(StepKind.Gap, 0);

        public static readonly NavigationStep Empty = new(StepKind.Empty, 0);

        public static NavigationStep Page(int number) => new(StepKind.Page, number);
    }

    private async Task GoToPageAsync(int page)
    {
        if (page < 1 || page > PageCount || page == ActivePage)
        {
            return;
        }

        await OnPageChanged.InvokeAsync(page);
    }

    // A button never overshoots an end: the last press before it lands on the first or the last page.
    private Task MoveAsync(NavigationDirection direction, int stride)
        => GoToPageAsync(direction is NavigationDirection.Previous
            ? Math.Max(ActivePage - stride, 1)
            : Math.Min(ActivePage + stride, PageCount));

    private async Task OnNavigationInputKeyDownAsync(KeyboardEventArgs args)
    {
        if (args.Key is "Enter")
        {
            await GoAsync();
        }
    }

    // A typed page outside the range settles on the nearest one, matching how an
    // out-of-range CurrentPage behaves. Enter or Go consumes whatever is in the box,
    // which then reads the page landed on; an unparseable entry gives way to the page shown.
    private async Task GoAsync()
    {
        var entry = await TakeNavigationEntryAsync();

        if (int.TryParse(entry, NumberStyles.Integer, CultureInfo.CurrentCulture, out var page))
        {
            await GoToPageAsync(Math.Clamp(page, 1, PageCount));
        }

        ShowPage();
        await SelectNavigationEntryAsync();
    }

    private void ShowPage()
        => _navigationEntry = PageSummary;

    // Leaving the box blurs it before a Go click lands, so an entry in progress stays; only an emptied box reverts.
    private void ShowPageIfEmpty()
    {
        if (string.IsNullOrEmpty(_navigationEntry))
        {
            ShowPage();
        }
    }

    // Selected whole, the summary gives way to the first key typed rather than taking it on the end.
    private async Task SelectNavigationEntryAsync()
    {
        await Task.Yield();
        await _text.WriteSelectionAsync(NavigationInput.Element, 0, _navigationEntry?.Length ?? 0);
    }

    // Typing that lands before the circuit connects raises no input event, leaving the
    // tracked value stale while the box holds the number. The box is the fallback.
    private async ValueTask<string?> TakeNavigationEntryAsync()
    {
        if (!string.IsNullOrEmpty(_navigationEntry))
        {
            return _navigationEntry;
        }

        return await _text.TakeAsync(NavigationInput.Element);
    }
}
