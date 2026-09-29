namespace Forma.Blazor.Components.Navigation;

using System.Globalization;

using Forma.Blazor.Components.Inputs;
using Forma.Framework.Browser.Interop.Behaviors;
using Forma.Framework.Browser.Interop.Operations;
using Forma.Framework.Css;
using Forma.Framework.Input.Interop;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;
using Microsoft.JSInterop;

/// <summary>A page-number navigator for splitting a list of items across multiple pages.</summary>
public sealed partial class FormaPagination : FormaElementBase
{
    // The navigation steps show the current page and the four after it, then a gap and the last page: seven in all.
    private const int LookAhead = 4;
    private const int StepCount = LookAhead + 3;

    // Beside the steps a navigation button moves a whole window.
    private const int StepsStride = LookAhead;

    private const string PageAction = "pagination.page";

    private static readonly TextBoxOptions NavigationEntryOptions = new() { InputMode = TextInputMode.Numeric, EnterKeyHint = TextEnterKeyHint.Go };

    [Inject] private InputRouter InputRouter { get; set; } = default!;

    private ImplicitSubmitGuard _submitGuard = default!;
    private Wheel _wheel = default!;
    private TextEntry _textEntry = default!;
    private DotNetObjectReference<FormaPagination>? _selfReference;
    private bool _isSubmitGuardAttached;
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
    [Parameter] public PaginationOptions Options { get; set; } = PaginationOptions.Default;

    private string NavigationInputId
        => $"{ElementId}-input";

    private int PageCount => Math.Max(1, (int)Math.Ceiling(TotalItems / (double)PageSize));

    // Whether state calls for the input; the width is the stylesheet's decision.
    private bool HasInputByState => Options.NavigationInput || PageCount > Options.MaxPagesWithSteps;

    // The page shown; a page outside the range resolves to the nearest.
    private int ActivePage => Math.Clamp(CurrentPage, 1, PageCount);

    private IReadOnlyList<NavigationStep> NavigationSteps => BuildNavigationSteps(ActivePage, PageCount);

    // The navigation box; the reference reaches its element.
    private FormaTextBox NavigationInput { get; set; } = default!;

    // The host's accessible name wins.
    private string NavLabel => AdditionalAttributes?.GetValueOrDefault("aria-label")?.ToString() ?? L["Pagination"].Value;

    // Handlers splatted onto the box's input.
    private EventCallback<KeyboardEventArgs> NavigationInputKeyDown
        => EventCallback.Factory.Create<KeyboardEventArgs>(this, OnNavigationInputKeyDownAsync);

    private EventCallback<FocusEventArgs> NavigationInputFocus
        => EventCallback.Factory.Create<FocusEventArgs>(this, SelectNavigationEntryAsync);

    private EventCallback<FocusEventArgs> NavigationInputBlur
        => EventCallback.Factory.Create<FocusEventArgs>(this, ShowPageIfEmpty);

    // The box reads "7 of 200" at rest: where the reader is, out of how many.
    private string PageSummary
        => L["{PageNumber} of {PageCount}", ActivePage.ToString(CultureInfo.CurrentCulture), PageCount.ToString("N0", CultureInfo.CurrentCulture)].Value;

    /// <summary>Turns one page in the direction of a wheel gesture; called from the wheel behavior.</summary>
    [JSInvokable]
    public Task HandleWheelAsync(int direction)
        => InvokeAsync(() => MoveAsync(direction < 0 ? NavigationDirection.Previous : NavigationDirection.Next, 1));

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        _submitGuard = new ImplicitSubmitGuard(Context.JS);
        _wheel = new Wheel(Context.JS);
        _textEntry = new TextEntry(Context.JS);
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Options);
        ArgumentOutOfRangeException.ThrowIfNegative(TotalItems);
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(PageSize, 0);

        // The input shows the page the reader is on.
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
            _isSubmitGuardAttached = false;
            await _submitGuard.DetachAsync();
            await _wheel.DetachAsync();
            return;
        }

        if (!_isSubmitGuardAttached)
        {
            _isSubmitGuardAttached = await _submitGuard.AttachAsync(NavigationInput.RootElement);
        }

        if (Options.PageOnWheelGesture && !_wheel.IsAttached)
        {
            _selfReference ??= DotNetObjectReference.Create(this);

            // The turn is a route the script declares itself, so the host's policy reaches the router before it does.
            await InputRouter.ConfigureAsync();
            await _wheel.AttachAsync(RootElement, ScrollWheel.Tells(PageAction), _selfReference);
        }
        else if (!Options.PageOnWheelGesture && _wheel.IsAttached)
        {
            await _wheel.DetachAsync();
        }
    }

    // The window starts at the current page and looks ahead; near the end the last seven pages show whole; a shorter list pads with empty steps so the first page sits in the same place.
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

    // A button stops at either end.
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

    // A typed page outside the range settles on the nearest one; an unparseable entry gives way to the page shown.
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

    // Only an emptied box reverts to the page shown.
    private void ShowPageIfEmpty()
    {
        if (string.IsNullOrEmpty(_navigationEntry))
        {
            ShowPage();
        }
    }

    // Selects the whole entry, so the first key typed replaces it.
    private async Task SelectNavigationEntryAsync()
    {
        await Task.Yield();
        await _textEntry.TryWriteSelectionAsync(NavigationInput.RootElement, 0, _navigationEntry?.Length ?? 0);
    }

    // Typing before the circuit connects raises no input event, so the box is read when the tracked value is empty.
    private async ValueTask<string?> TakeNavigationEntryAsync()
    {
        if (!string.IsNullOrEmpty(_navigationEntry))
        {
            return _navigationEntry;
        }

        return await _textEntry.ReadAndClearAsync(NavigationInput.RootElement);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-pagination");

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        await _submitGuard.DisposeAsync();
        await _wheel.DisposeAsync();
        await _textEntry.DisposeAsync();
        _selfReference?.Dispose();
        await base.DisposeAsyncCore();
    }
}
