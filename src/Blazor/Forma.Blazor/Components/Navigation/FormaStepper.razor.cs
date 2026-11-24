namespace Forma.Blazor.Components.Navigation;

using Forma.Framework.Browser;
using Forma.Framework.Css;
using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>A numbered strip of the steps in an ordered flow, marking the one the reader is on.</summary>
/// <remarks>
/// Beside its list of steps the stepper lays them out again in a ruler that takes no room and draws nothing, every name shown, and watches
/// the width of both; where the ruler is the wider, the steps not current show their numbers alone, their names kept for assistive
/// technology, so the row never wraps. Until the first widths arrive, and wherever the stepper is rendered statically, every name gives way instead, ending in
/// an ellipsis.
/// </remarks>
public sealed partial class FormaStepper : FormaElementBase
{
    /// <summary>The steps, in the order the reader takes them.</summary>
    [Parameter, EditorRequired] public IReadOnlyList<StepperStep> Steps { get; set; } = [];

    /// <summary>The index of the step the reader is on.</summary>
    [Parameter] public int CurrentIndex { get; set; }

    /// <summary>The accessible name of the steps.</summary>
    [Parameter] public string? AriaLabel { get; set; }

    // The address each step links to, which is none where a link doesn't allow the one it was given.
    private string?[] _stepAddresses = [];

    private ElementReference _list;
    private ElementReference _ruler;
    private IDomObservation<SizeWatchOptions, SizeState>? _listSize;
    private IDomObservation<SizeWatchOptions, SizeState>? _rulerSize;
    private double? _listInlineSize;
    private double? _rulerInlineSize;
    private bool _isCompact;

    // A stepper rendered statically marks itself for the page's script, which measures whether its names fit, as no renderer can.
    private string? StaticMark
        => RendererInfo.IsInteractive ? null : string.Empty;

    private string EffectiveAriaLabel
        => AriaLabel ?? L["Steps"].Value;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentNullException.ThrowIfNull(Steps);

        if (Steps.Count > 0)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(CurrentIndex);
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(CurrentIndex, Steps.Count);
        }

        _stepAddresses = [.. Steps.Select(step => Context.GetAllowedAddress(step.Url?.OriginalString, AddressKind.Link))];

        base.OnParametersSet();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        // Both widths are watched from the first render, each watch started before either is awaited, so the two are established together.
        // The list's is the room the steps are given, whatever padding or border the host gives the stepper.
        if (firstRender)
        {
            var listSize = Context.DomWatcher.GetElement(_list).Geometry.WatchSizeAsync(HandleListSizeAsync);
            var rulerSize = Context.DomWatcher.GetElement(_ruler).Geometry.WatchSizeAsync(HandleRulerSizeAsync);
            _listSize = await listSize;
            _rulerSize = await rulerSize;
        }
    }

    // The sizes arrive off the renderer, the first of each before its watch is returned.
    private Task HandleListSizeAsync(SizeState state)
        => InvokeAsync(() =>
        {
            _listInlineSize = state.InlineSize;
            FitNames();
        });

    private Task HandleRulerSizeAsync(SizeState state)
        => InvokeAsync(() =>
        {
            _rulerInlineSize = state.InlineSize;
            FitNames();
        });

    // The names fit while the ruler showing them all is no wider than the list, and are taken to until both widths are known.
    private void FitNames()
    {
        var isCompact = _rulerInlineSize > _listInlineSize;

        if (isCompact != _isCompact)
        {
            _isCompact = isCompact;
            StateHasChanged();
        }
    }

    private string FormatNumber(int index)
        => (index + 1).ToString(Context.CultureState.Culture);

    private static string GetStepClass(StepperStep step, bool isCurrent)
        => new CssClassBuilder()
            .Add("forma-stepper-step")
            .Add("is-current", isCurrent)
            .Add("is-completed", step.IsCompleted)
            .ToString();

    private static string GetRulerStepClass(bool isCurrent)
        => new CssClassBuilder()
            .Add("forma-stepper-ruler-step")
            .Add("is-current", isCurrent)
            .ToString();

    // A step the list draws as a link takes a link's least size in the ruler too.
    private static string GetRulerTargetClass(string? address, bool isCurrent)
        => new CssClassBuilder()
            .Add("forma-stepper-target")
            .Add("is-link", address is not null && !isCurrent)
            .ToString();

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-stepper").Add("is-compact", _isCompact);

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        if (_listSize is { } listSize)
        {
            await listSize.DisposeAsync();
        }

        if (_rulerSize is { } rulerSize)
        {
            await rulerSize.DisposeAsync();
        }

        await base.DisposeAsyncCore();
    }
}
