namespace Forma.Blazor.Components.Inputs;

using System.Globalization;

using Forma.Blazor.Interop.Behaviours;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;
using Microsoft.JSInterop;

/// <summary>A calendar for picking a single date, or a date range when the range parameters are bound.</summary>
public sealed partial class FormaDatePicker : ComponentBase, IAsyncDisposable
{
    // The rolling windows the Last7Days and Last30Days presets cover, ending today.
    private const int ShortRangeDays = 7;
    private const int LongRangeDays = 30;

    // ── Single-date mode ─────────────────────────────

    /// <summary>The selected date.</summary>
    [Parameter] public DateOnly? Value { get; set; }

    /// <summary>Raised when the selected date changes.</summary>
    [Parameter] public EventCallback<DateOnly?> ValueChanged { get; set; }

    // ── Range mode ───────────────────────────────────

    /// <summary>The first date of the selected range.</summary>
    [Parameter] public DateOnly? StartDate { get; set; }

    /// <summary>Raised when the range's first date changes; binding it puts the picker in range mode.</summary>
    [Parameter] public EventCallback<DateOnly?> StartDateChanged { get; set; }

    /// <summary>The last date of the selected range.</summary>
    [Parameter] public DateOnly? EndDate { get; set; }

    /// <summary>Raised when the range's last date changes; binding it puts the picker in range mode.</summary>
    [Parameter] public EventCallback<DateOnly?> EndDateChanged { get; set; }

    // ── Common ───────────────────────────────────────

    /// <summary>The field's label and accessible name.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The earliest selectable date.</summary>
    [Parameter] public DateOnly? MinDate { get; set; }

    /// <summary>The latest selectable date.</summary>
    [Parameter] public DateOnly? MaxDate { get; set; }

    /// <summary>Whether the quick-range presets are offered.</summary>
    [Parameter] public bool ShowPresets { get; set; } = true;

    /// <summary>Raised once a selection is complete: a date in single-date mode, both ends in range mode.</summary>
    [Parameter] public EventCallback OnSelectionComplete { get; set; }

    /// <summary>Whether months come from the culture's non-Gregorian calendar, such as the Hebrew calendar for <c>he</c>.</summary>
    [Parameter] public bool UseNonGregorianMonths { get; set; }

    /// <summary>Attributes applied to the trigger element.</summary>
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocale<FormaDatePicker> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool _isOpen;
    private readonly string _triggerId = $"dp-trigger-{Guid.NewGuid():N}";
    private ElementReference _anchorElement;
    private ElementReference _popupElement;
    private AnchoredPopup _popup = default!;
    private DateOnly _leftMonth;
    private DateOnly _rightMonth;
    private readonly DateRangeDraft _draft = new();
    private CalendarMonthView _months = default!;

    private bool IsRangeMode => StartDateChanged.HasDelegate || EndDateChanged.HasDelegate;
    private int CalendarCount => IsRangeMode ? 2 : 1;

#pragma warning disable CA1822 // Intentionally instance — reads ambient CultureInfo per render
    private CultureInfo Culture => CultureInfo.CurrentCulture;
#pragma warning restore CA1822
    private string TextDirection => Culture.TextInfo.IsRightToLeft ? "rtl" : "ltr";

    private bool HasValue => IsRangeMode
        ? StartDate is not null || EndDate is not null
        : Value is not null;

    private string DisplayText
    {
        get
        {
            if (IsRangeMode)
            {
                if (StartDate is not null && EndDate is not null)
                {
                    return $"{FormatDate(StartDate.Value)} - {FormatDate(EndDate.Value)}";
                }

                return string.Empty;
            }
            return Value is not null ? FormatDate(Value.Value) : string.Empty;
        }
    }

    private string Placeholder => IsRangeMode ? L["Select Range"] : L["Select Date"];

    private DateOnly GetPanelMonth(int panelIndex) =>
        panelIndex == 0 ? _leftMonth : _rightMonth;

    private bool CanPanelGoPrev(int panelIndex) =>
        GetPanelMonth(panelIndex) > (IsRangeMode && panelIndex == 1 ? _months.AddMonths(_months.MinMonth, 1) : _months.MinMonth);

    private bool CanPanelGoNext(int panelIndex) =>
        GetPanelMonth(panelIndex) < (IsRangeMode && panelIndex == 0 ? _months.AddMonths(_months.MaxMonth, -1) : _months.MaxMonth);

    // ── Lifecycle ────────────────────────────────────

    /// <inheritdoc />
    protected override void OnInitialized()
        => _popup = new AnchoredPopup(JS);

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
        => await _popup.SyncAsync(_isOpen, _popupElement, _anchorElement);

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
        => await _popup.DisposeAsync();

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);

        _months = CalendarMonthView.For(Culture, UseNonGregorianMonths, MinDate, MaxDate);

        if (MinDate.HasValue && MaxDate.HasValue && MinDate.Value > MaxDate.Value)
        {
            throw new ArgumentException($"{nameof(MinDate)} ({MinDate}) must not be greater than {nameof(MaxDate)} ({MaxDate}).");
        }

        if (IsRangeMode && StartDate.HasValue && EndDate.HasValue && StartDate.Value > EndDate.Value)
        {
            throw new ArgumentException($"{nameof(StartDate)} ({StartDate}) must not be greater than {nameof(EndDate)} ({EndDate}).");
        }

        if (_leftMonth == default)
        {
            ResetDisplayMonth();
        }
    }

    // ── Helpers ──────────────────────────────────────

    private string FormatDate(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToString("d", Culture);

    private void ResetDisplayMonth()
    {
        if (IsRangeMode)
        {
            var endRef = EndDate ?? StartDate ?? DateOnly.FromDateTime(DateTime.Today);
            _rightMonth = _months.Clamp(_months.MonthStart(_months.YearOf(endRef), _months.MonthOf(endRef)));

            var startRef = StartDate ?? endRef;
            _leftMonth = _months.Clamp(_months.MonthStart(_months.YearOf(startRef), _months.MonthOf(startRef)));

            if (_leftMonth >= _rightMonth)
            {
                _leftMonth = _months.AddMonths(_rightMonth, -1);
                if (_leftMonth < _months.MinMonth)
                {
                    _leftMonth = _months.MinMonth;
                }
            }
        }
        else
        {
            var reference = Value ?? DateOnly.FromDateTime(DateTime.Today);
            _leftMonth = _months.Clamp(_months.MonthStart(_months.YearOf(reference), _months.MonthOf(reference)));
        }
    }

    // ── Popup ────────────────────────────────────────

    private void Toggle()
    {
        _isOpen = !_isOpen;
        if (_isOpen)
        {
            _draft.Clear();
            ResetDisplayMonth();
        }
    }

    private void Close() => _isOpen = false;

    // ── Navigation ───────────────────────────────────

    private void PanelPrev(int panelIndex)
    {
        if (CanPanelGoPrev(panelIndex))
        {
            SetPanelMonth(panelIndex, _months.AddMonths(GetPanelMonth(panelIndex), -1));
        }
    }

    private void PanelNext(int panelIndex)
    {
        if (CanPanelGoNext(panelIndex))
        {
            SetPanelMonth(panelIndex, _months.AddMonths(GetPanelMonth(panelIndex), 1));
        }
    }

    private void OnMonthDropdownChange(int panelIndex, ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var month))
        {
            var current = GetPanelMonth(panelIndex);
            SetPanelMonth(panelIndex, _months.MonthStart(_months.YearOf(current), month));
        }
    }

    private void OnYearDropdownChange(int panelIndex, ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var gregorianYear))
        {
            var current = GetPanelMonth(panelIndex);
            var calMonth = _months.MonthOf(current);
            // Find a Gregorian date in the target year, then build from calendar coordinates
            var midYear = new DateOnly(gregorianYear, 6, 1);
            var targetCalYear = _months.YearOf(midYear);
            var maxMonth = _months.MonthsInYear(targetCalYear);
            if (calMonth > maxMonth)
            {
                calMonth = maxMonth;
            }

            SetPanelMonth(panelIndex, _months.MonthStart(targetCalYear, calMonth));
        }
    }

    private void SetPanelMonth(int panelIndex, DateOnly month)
    {
        month = _months.Clamp(month);
        if (panelIndex == 0)
        {
            _leftMonth = month;
            if (IsRangeMode && _leftMonth >= _rightMonth)
            {
                _rightMonth = _months.AddMonths(_leftMonth, 1);
                if (_rightMonth > _months.MaxMonth)
                {
                    _rightMonth = _months.MaxMonth;
                    _leftMonth = _months.AddMonths(_rightMonth, -1);
                }
            }
        }
        else
        {
            _rightMonth = month;
            if (_rightMonth <= _leftMonth)
            {
                _leftMonth = _months.AddMonths(_rightMonth, -1);
                if (_leftMonth < _months.MinMonth)
                {
                    _leftMonth = _months.MinMonth;
                    _rightMonth = _months.AddMonths(_leftMonth, 1);
                }
            }
        }
    }

    // ── Selection ────────────────────────────────────

    private async Task SelectDay(DateOnly date)
    {
        if (IsDayDisabled(date))
        {
            return;
        }

        if (IsRangeMode)
        {
            if (!_draft.IsAnchored)
            {
                _draft.AnchorAt(date);
            }
            else
            {
                var (start, end) = _draft.CompleteAt(date);
                StartDate = start;
                EndDate = end;
                await StartDateChanged.InvokeAsync(start);
                await EndDateChanged.InvokeAsync(end);
                await OnSelectionComplete.InvokeAsync();
                _isOpen = false;
            }
        }
        else
        {
            Value = date;
            await ValueChanged.InvokeAsync(date);
            await OnSelectionComplete.InvokeAsync();
            _isOpen = false;
        }
    }

    private void HoverDay(DateOnly date)
        => _draft.HoverAt(date);

    private async Task HandleClear()
    {
        if (IsRangeMode)
        {
            StartDate = null;
            EndDate = null;
            await StartDateChanged.InvokeAsync(null);
            await EndDateChanged.InvokeAsync(null);
        }
        else
        {
            Value = null;
            await ValueChanged.InvokeAsync(null);
        }
        _draft.Clear();
        _isOpen = false;
    }

    // ── Calendar grid ────────────────────────────────

    private bool IsDayDisabled(DateOnly date) =>
        (MinDate.HasValue && date < MinDate.Value) ||
        (MaxDate.HasValue && date > MaxDate.Value);

    private static bool MarksToday(string dayClasses)
        => dayClasses.Contains("today", StringComparison.Ordinal);

    private static bool MarksSelected(string dayClasses)
        => dayClasses.Contains("selected", StringComparison.Ordinal);

    private string GetDayClasses(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var classes = string.Empty;

        if (date == today)
        {
            classes += " today";
        }

        if (IsRangeMode)
        {
            if (_draft.Span is { } span)
            {
                var (from, to) = span;

                if (date == from)
                {
                    classes += " selected start";
                }
                else if (date == to)
                {
                    classes += " selected end";
                }
                else if (date > from && date < to)
                {
                    classes += " in-range";
                }
            }
            else
            {
                if (StartDate.HasValue && date == StartDate.Value)
                {
                    classes += " selected start";
                }
                else if (EndDate.HasValue && date == EndDate.Value)
                {
                    classes += " selected end";
                }
                else if (StartDate.HasValue && EndDate.HasValue &&
                         date > StartDate.Value && date < EndDate.Value)
                {
                    classes += " in-range";
                }
            }
        }
        else
        {
            if (Value.HasValue && date == Value.Value)
            {
                classes += " selected";
            }
        }

        return classes;
    }

    // ── Presets (range mode) ─────────────────────────

    private async Task ApplyPreset(Preset preset)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (start, end) = ComputePresetRange(preset, today);

        if (MinDate.HasValue && start < MinDate.Value)
        {
            start = MinDate.Value;
        }

        if (MaxDate.HasValue && end > MaxDate.Value)
        {
            end = MaxDate.Value;
        }

        if (start > end)
        {
            return;
        }

        StartDate = start;
        EndDate = end;
        _rightMonth = _months.Clamp(_months.MonthStart(_months.YearOf(end), _months.MonthOf(end)));
        _leftMonth = _months.Clamp(_months.MonthStart(_months.YearOf(start), _months.MonthOf(start)));
        if (_leftMonth >= _rightMonth)
        {
            _leftMonth = _months.AddMonths(_rightMonth, -1);
            if (_leftMonth < _months.MinMonth)
            {
                _leftMonth = _months.MinMonth;
            }
        }
        _draft.Clear();
        await StartDateChanged.InvokeAsync(start);
        await EndDateChanged.InvokeAsync(end);
        await OnSelectionComplete.InvokeAsync();
        _isOpen = false;
    }

    private (DateOnly start, DateOnly end) ComputePresetRange(Preset preset, DateOnly today) =>
        preset switch
        {
            Preset.Today => (today, today),
            Preset.Yesterday => (today.AddDays(-1), today.AddDays(-1)),
            Preset.Last7Days => (today.AddDays(-(ShortRangeDays - 1)), today),
            Preset.Last30Days => (today.AddDays(-(LongRangeDays - 1)), today),
            Preset.ThisMonth => (_months.MonthStart(_months.YearOf(today), _months.MonthOf(today)), today),
            Preset.LastMonth => CalendarLastMonthRange(today),
            _ => (today, today)
        };

    private bool IsPresetDisabled(Preset preset)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (start, end) = ComputePresetRange(preset, today);
        if (MinDate.HasValue && start < MinDate.Value)
        {
            start = MinDate.Value;
        }

        if (MaxDate.HasValue && end > MaxDate.Value)
        {
            end = MaxDate.Value;
        }

        return start > end;
    }

    private bool IsYearDisabled(int gregorianYear, int panelIndex)
    {
        for (var month = 1; month <= 12; month++)
        {
            var candidate = new DateOnly(gregorianYear, month, 1);
            if (candidate < _months.MinMonth || candidate > _months.MaxMonth)
            {
                continue;
            }

            if (IsRangeMode && panelIndex == 0 && candidate > _months.AddMonths(_months.MaxMonth, -1))
            {
                continue;
            }

            if (IsRangeMode && panelIndex == 1 && candidate < _months.AddMonths(_months.MinMonth, 1))
            {
                continue;
            }

            return false;
        }
        return true;
    }

    private bool IsMonthDisabled(int calYear, int calMonth, int panelIndex)
    {
        if (calMonth > _months.MonthsInYear(calYear))
        {
            return true;
        }

        var candidate = _months.MonthStart(calYear, calMonth);
        if (candidate < _months.MinMonth || candidate > _months.MaxMonth)
        {
            return true;
        }

        if (IsRangeMode && panelIndex == 0 && candidate > _months.AddMonths(_months.MaxMonth, -1))
        {
            return true;
        }

        if (IsRangeMode && panelIndex == 1 && candidate < _months.AddMonths(_months.MinMonth, 1))
        {
            return true;
        }

        return false;
    }

    private (DateOnly start, DateOnly end) CalendarLastMonthRange(DateOnly today)
    {
        var thisMonthStart = _months.MonthStart(_months.YearOf(today), _months.MonthOf(today));
        var lastMonthEnd = thisMonthStart.AddDays(-1);
        var lastMonthStart = _months.MonthStart(_months.YearOf(lastMonthEnd), _months.MonthOf(lastMonthEnd));
        return (lastMonthStart, lastMonthEnd);
    }

    private enum Preset { Today, Yesterday, Last7Days, Last30Days, ThisMonth, LastMonth }
}
