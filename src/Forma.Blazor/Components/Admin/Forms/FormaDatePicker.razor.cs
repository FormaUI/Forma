using System.Globalization;
using Forma.Blazor.Interop;
using Microsoft.AspNetCore.Components;
using Microsoft.Extensions.Localization;
using Microsoft.JSInterop;

namespace Forma.Blazor.Components.Admin.Forms;

public sealed partial class FormaDatePicker : ComponentBase, IAsyncDisposable
{
    private const string ModulePath = "./_content/Forma.Blazor/Components/Admin/Forms/FormaDatePicker.razor.js";
    // ── Single-date mode ─────────────────────────────
    [Parameter] public DateOnly? Value { get; set; }
    [Parameter] public EventCallback<DateOnly?> ValueChanged { get; set; }

    // ── Range mode ───────────────────────────────────
    [Parameter] public DateOnly? StartDate { get; set; }
    [Parameter] public EventCallback<DateOnly?> StartDateChanged { get; set; }
    [Parameter] public DateOnly? EndDate { get; set; }
    [Parameter] public EventCallback<DateOnly?> EndDateChanged { get; set; }

    // ── Common ───────────────────────────────────────
    [Parameter] public string? Label { get; set; }
    [Parameter] public DateOnly? MinDate { get; set; }
    [Parameter] public DateOnly? MaxDate { get; set; }
    [Parameter] public bool ShowPresets { get; set; } = true;
    [Parameter] public EventCallback OnSelectionComplete { get; set; }
    [Parameter] public bool UseNonGregorianMonths { get; set; }
    [Parameter(CaptureUnmatchedValues = true)]
    public Dictionary<string, object>? AdditionalAttributes { get; set; }

    [Inject] private IStringLocalizer<FormaDatePicker> L { get; set; } = default!;

    [Inject] private IJSRuntime JS { get; set; } = default!;

    private bool _isOpen;
    private ElementReference _anchorElement;
    private ElementReference _popupElement;
    private JsModule _module = default!;
    private bool _placementAttached;
    private DateOnly _leftMonth;
    private DateOnly _rightMonth;
    private DateOnly? _rangeStart;
    private DateOnly? _hoverDate;
    private Calendar _calendar = null!;
    private DateTimeFormatInfo _dtf = null!;

    private bool IsRangeMode => StartDateChanged.HasDelegate || EndDateChanged.HasDelegate;
    private int CalendarCount => IsRangeMode ? 2 : 1;

#pragma warning disable CA1822 // Intentionally instance — reads ambient CultureInfo per render
    private CultureInfo _culture => CultureInfo.CurrentCulture;
#pragma warning restore CA1822
    private string _textDirection => _culture.TextInfo.IsRightToLeft ? "rtl" : "ltr";

    private int CalendarYear(DateOnly d) => _calendar.GetYear(d.ToDateTime(TimeOnly.MinValue));
    private int CalendarMonth(DateOnly d) => _calendar.GetMonth(d.ToDateTime(TimeOnly.MinValue));
    private DateOnly CalendarMonthStart(int calYear, int calMonth) =>
        DateOnly.FromDateTime(_calendar.ToDateTime(calYear, calMonth, 1, 0, 0, 0, 0));
    private DateOnly AddCalendarMonths(DateOnly d, int months) =>
        DateOnly.FromDateTime(_calendar.AddMonths(d.ToDateTime(TimeOnly.MinValue), months));

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
                    return $"{FormatDate(StartDate.Value)} - {FormatDate(EndDate.Value)}";
                return string.Empty;
            }
            return Value is not null ? FormatDate(Value.Value) : string.Empty;
        }
    }

    private string Placeholder => IsRangeMode ? L["SelectRange"] : L["SelectDate"];

    private int MinYear => MinDate.HasValue
        ? MinDate.Value.Year
        : DateOnly.FromDateTime(DateTime.Today).Year - 10;
    private int MaxYear => MaxDate.HasValue
        ? MaxDate.Value.Year
        : DateOnly.FromDateTime(DateTime.Today).Year + 10;

    private DateOnly MinMonth
    {
        get
        {
            if (MinDate.HasValue)
                return CalendarMonthStart(CalendarYear(MinDate.Value), CalendarMonth(MinDate.Value));
            var fallback = new DateOnly(MinYear, 1, 1);
            return CalendarMonthStart(CalendarYear(fallback), CalendarMonth(fallback));
        }
    }

    private DateOnly MaxMonth
    {
        get
        {
            if (MaxDate.HasValue)
                return CalendarMonthStart(CalendarYear(MaxDate.Value), CalendarMonth(MaxDate.Value));
            var fallback = new DateOnly(MaxYear, 12, 1);
            return CalendarMonthStart(CalendarYear(fallback), CalendarMonth(fallback));
        }
    }

    private DateOnly GetPanelMonth(int panelIndex) =>
        panelIndex == 0 ? _leftMonth : _rightMonth;

    private bool CanPanelGoPrev(int panelIndex) =>
        GetPanelMonth(panelIndex) > (IsRangeMode && panelIndex == 1 ? AddCalendarMonths(MinMonth, 1) : MinMonth);

    private bool CanPanelGoNext(int panelIndex) =>
        GetPanelMonth(panelIndex) < (IsRangeMode && panelIndex == 0 ? AddCalendarMonths(MaxMonth, -1) : MaxMonth);

    // ── Lifecycle ────────────────────────────────────

    protected override void OnInitialized()
        => _module = new JsModule(JS, ModulePath);

    // The popup only exists in the DOM while open, so placement is attached and released
    // against the trigger, which outlives it.
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (_isOpen == _placementAttached)
        {
            return;
        }

        try
        {
            if (_isOpen)
            {
                await _module.InvokeVoidAsync("open", _popupElement, _anchorElement).ConfigureAwait(false);
            }
            else
            {
                await _module.InvokeVoidAsync("close", _anchorElement).ConfigureAwait(false);
            }

            _placementAttached = _isOpen;
        }
        catch (JSException)
        {
            // Circuit disconnected before the module loaded — safe to ignore.
        }
    }

    /// <inheritdoc />
    public async ValueTask DisposeAsync()
    {
        try
        {
            if (_placementAttached)
            {
                await _module.InvokeVoidAsync("close", _anchorElement).ConfigureAwait(false);
            }
        }
        catch (JSDisconnectedException)
        {
            // Circuit disconnected — safe to ignore.
        }
        catch (JSException)
        {
            // Module never loaded — nothing to release.
        }

        await _module.DisposeAsync().ConfigureAwait(false);
    }

    protected override void OnParametersSet()
    {
        _calendar = UseNonGregorianMonths
            ? (_culture.OptionalCalendars.FirstOrDefault(c => c is not GregorianCalendar)
               ?? _culture.DateTimeFormat.Calendar)
            : _culture.DateTimeFormat.Calendar;

        if (UseNonGregorianMonths && _calendar is not GregorianCalendar)
        {
            var dtf = (DateTimeFormatInfo)_culture.DateTimeFormat.Clone();
            dtf.Calendar = _calendar;
            _dtf = dtf;
        }
        else
        {
            _dtf = _culture.DateTimeFormat;
        }

        if (MinDate.HasValue && MaxDate.HasValue && MinDate.Value > MaxDate.Value)
            throw new ArgumentException($"{nameof(MinDate)} ({MinDate}) must not be greater than {nameof(MaxDate)} ({MaxDate}).");

        if (IsRangeMode && StartDate.HasValue && EndDate.HasValue && StartDate.Value > EndDate.Value)
            throw new ArgumentException($"{nameof(StartDate)} ({StartDate}) must not be greater than {nameof(EndDate)} ({EndDate}).");

        if (_leftMonth == default)
            ResetDisplayMonth();
    }

    // ── Helpers ──────────────────────────────────────

    private string FormatDate(DateOnly date) =>
        date.ToDateTime(TimeOnly.MinValue).ToString("d", _culture);

    private void ResetDisplayMonth()
    {
        if (IsRangeMode)
        {
            var endRef = EndDate ?? StartDate ?? DateOnly.FromDateTime(DateTime.Today);
            _rightMonth = ClampMonth(CalendarMonthStart(CalendarYear(endRef), CalendarMonth(endRef)));

            var startRef = StartDate ?? endRef;
            _leftMonth = ClampMonth(CalendarMonthStart(CalendarYear(startRef), CalendarMonth(startRef)));

            if (_leftMonth >= _rightMonth)
            {
                _leftMonth = AddCalendarMonths(_rightMonth, -1);
                if (_leftMonth < MinMonth)
                    _leftMonth = MinMonth;
            }
        }
        else
        {
            var reference = Value ?? DateOnly.FromDateTime(DateTime.Today);
            _leftMonth = ClampMonth(CalendarMonthStart(CalendarYear(reference), CalendarMonth(reference)));
        }
    }

    private DateOnly ClampMonth(DateOnly month)
    {
        if (month < MinMonth) return MinMonth;
        if (month > MaxMonth) return MaxMonth;
        return month;
    }

    // ── Popup ────────────────────────────────────────

    private void Toggle()
    {
        _isOpen = !_isOpen;
        if (_isOpen)
        {
            _rangeStart = null;
            _hoverDate = null;
            ResetDisplayMonth();
        }
    }

    private void Close() => _isOpen = false;

    // ── Navigation ───────────────────────────────────

    private void PanelPrev(int panelIndex)
    {
        if (CanPanelGoPrev(panelIndex))
            SetPanelMonth(panelIndex, AddCalendarMonths(GetPanelMonth(panelIndex), -1));
    }

    private void PanelNext(int panelIndex)
    {
        if (CanPanelGoNext(panelIndex))
            SetPanelMonth(panelIndex, AddCalendarMonths(GetPanelMonth(panelIndex), 1));
    }

    private void OnMonthDropdownChange(int panelIndex, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var month))
        {
            var current = GetPanelMonth(panelIndex);
            SetPanelMonth(panelIndex, CalendarMonthStart(CalendarYear(current), month));
        }
    }

    private void OnYearDropdownChange(int panelIndex, ChangeEventArgs e)
    {
        if (int.TryParse(e.Value?.ToString(), out var gregorianYear))
        {
            var current = GetPanelMonth(panelIndex);
            var calMonth = CalendarMonth(current);
            // Find a Gregorian date in the target year, then build from calendar coordinates
            var midYear = new DateOnly(gregorianYear, 6, 1);
            var targetCalYear = CalendarYear(midYear);
            var maxMonth = _calendar.GetMonthsInYear(targetCalYear);
            if (calMonth > maxMonth) calMonth = maxMonth;
            SetPanelMonth(panelIndex, CalendarMonthStart(targetCalYear, calMonth));
        }
    }

    private void SetPanelMonth(int panelIndex, DateOnly month)
    {
        month = ClampMonth(month);
        if (panelIndex == 0)
        {
            _leftMonth = month;
            if (IsRangeMode && _leftMonth >= _rightMonth)
            {
                _rightMonth = AddCalendarMonths(_leftMonth, 1);
                if (_rightMonth > MaxMonth)
                {
                    _rightMonth = MaxMonth;
                    _leftMonth = AddCalendarMonths(_rightMonth, -1);
                }
            }
        }
        else
        {
            _rightMonth = month;
            if (_rightMonth <= _leftMonth)
            {
                _leftMonth = AddCalendarMonths(_rightMonth, -1);
                if (_leftMonth < MinMonth)
                {
                    _leftMonth = MinMonth;
                    _rightMonth = AddCalendarMonths(_leftMonth, 1);
                }
            }
        }
    }

    // ── Selection ────────────────────────────────────

    private async Task SelectDay(DateOnly date)
    {
        if (IsDayDisabled(date))
            return;

        if (IsRangeMode)
        {
            if (_rangeStart is null)
            {
                _rangeStart = date;
                _hoverDate = date;
            }
            else
            {
                var a = _rangeStart.Value;
                var start = a <= date ? a : date;
                var end = a <= date ? date : a;
                _rangeStart = null;
                _hoverDate = null;
                StartDate = start;
                EndDate = end;
                await StartDateChanged.InvokeAsync(start).ConfigureAwait(false);
                await EndDateChanged.InvokeAsync(end).ConfigureAwait(false);
                await OnSelectionComplete.InvokeAsync().ConfigureAwait(false);
                _isOpen = false;
            }
        }
        else
        {
            Value = date;
            await ValueChanged.InvokeAsync(date).ConfigureAwait(false);
            await OnSelectionComplete.InvokeAsync().ConfigureAwait(false);
            _isOpen = false;
        }
    }

    private void HoverDay(DateOnly date)
    {
        if (_rangeStart is not null)
            _hoverDate = date;
    }

    private async Task HandleClear()
    {
        if (IsRangeMode)
        {
            StartDate = null;
            EndDate = null;
            await StartDateChanged.InvokeAsync(null).ConfigureAwait(false);
            await EndDateChanged.InvokeAsync(null).ConfigureAwait(false);
        }
        else
        {
            Value = null;
            await ValueChanged.InvokeAsync(null).ConfigureAwait(false);
        }
        _rangeStart = null;
        _isOpen = false;
    }

    // ── Calendar grid ────────────────────────────────

    private bool IsDayDisabled(DateOnly date) =>
        (MinDate.HasValue && date < MinDate.Value) ||
        (MaxDate.HasValue && date > MaxDate.Value);

    private string GetDayClasses(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var css = string.Empty;

        if (date == today) css += " today";

        if (IsRangeMode)
        {
            if (_rangeStart is not null)
            {
                var rs = _rangeStart.Value;
                var hover = _hoverDate ?? rs;
                var lo = rs <= hover ? rs : hover;
                var hi = rs <= hover ? hover : rs;

                if (date == lo) css += " selected start";
                else if (date == hi) css += " selected end";
                else if (date > lo && date < hi) css += " in-range";
            }
            else
            {
                if (StartDate.HasValue && date == StartDate.Value) css += " selected start";
                else if (EndDate.HasValue && date == EndDate.Value) css += " selected end";
                else if (StartDate.HasValue && EndDate.HasValue &&
                         date > StartDate.Value && date < EndDate.Value) css += " in-range";
            }
        }
        else
        {
            if (Value.HasValue && date == Value.Value) css += " selected";
        }

        return css;
    }

    private List<DateOnly?> GetDaysForMonth(DateOnly monthStart, DayOfWeek firstDayOfWeek)
    {
        var count = _calendar.GetDaysInMonth(CalendarYear(monthStart), CalendarMonth(monthStart));
        var dow = ((int)monthStart.DayOfWeek - (int)firstDayOfWeek + 7) % 7;

        var days = new List<DateOnly?>(dow + count);
        for (var i = 0; i < dow; i++) days.Add(null);
        for (var d = 0; d < count; d++) days.Add(monthStart.AddDays(d));
        return days;
    }

    // ── Presets (range mode) ─────────────────────────

    private async Task ApplyPreset(Preset preset)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (start, end) = ComputePresetRange(preset, today);

        if (MinDate.HasValue && start < MinDate.Value) start = MinDate.Value;
        if (MaxDate.HasValue && end > MaxDate.Value) end = MaxDate.Value;
        if (start > end) return;

        StartDate = start;
        EndDate = end;
        _rightMonth = ClampMonth(CalendarMonthStart(CalendarYear(end), CalendarMonth(end)));
        _leftMonth = ClampMonth(CalendarMonthStart(CalendarYear(start), CalendarMonth(start)));
        if (_leftMonth >= _rightMonth)
        {
            _leftMonth = AddCalendarMonths(_rightMonth, -1);
            if (_leftMonth < MinMonth)
                _leftMonth = MinMonth;
        }
        _rangeStart = null;
        await StartDateChanged.InvokeAsync(start).ConfigureAwait(false);
        await EndDateChanged.InvokeAsync(end).ConfigureAwait(false);
        await OnSelectionComplete.InvokeAsync().ConfigureAwait(false);
        _isOpen = false;
    }

    private (DateOnly start, DateOnly end) ComputePresetRange(Preset preset, DateOnly today) =>
        preset switch
        {
            Preset.Today => (today, today),
            Preset.Yesterday => (today.AddDays(-1), today.AddDays(-1)),
            Preset.Last7Days => (today.AddDays(-6), today),
            Preset.Last30Days => (today.AddDays(-29), today),
            Preset.ThisMonth => (CalendarMonthStart(CalendarYear(today), CalendarMonth(today)), today),
            Preset.LastMonth => CalendarLastMonthRange(today),
            _ => (today, today)
        };

    private bool IsPresetDisabled(Preset preset)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var (start, end) = ComputePresetRange(preset, today);
        if (MinDate.HasValue && start < MinDate.Value) start = MinDate.Value;
        if (MaxDate.HasValue && end > MaxDate.Value) end = MaxDate.Value;
        return start > end;
    }

    private bool IsYearDisabled(int gregorianYear, int panelIndex)
    {
        for (var m = 1; m <= 12; m++)
        {
            var candidate = new DateOnly(gregorianYear, m, 1);
            if (candidate < MinMonth || candidate > MaxMonth) continue;
            if (IsRangeMode && panelIndex == 0 && candidate > AddCalendarMonths(MaxMonth, -1)) continue;
            if (IsRangeMode && panelIndex == 1 && candidate < AddCalendarMonths(MinMonth, 1)) continue;
            return false;
        }
        return true;
    }

    private bool IsMonthDisabled(int calYear, int calMonth, int panelIndex)
    {
        if (calMonth > _calendar.GetMonthsInYear(calYear)) return true;
        var candidate = CalendarMonthStart(calYear, calMonth);
        if (candidate < MinMonth || candidate > MaxMonth) return true;
        if (IsRangeMode && panelIndex == 0 && candidate > AddCalendarMonths(MaxMonth, -1)) return true;
        if (IsRangeMode && panelIndex == 1 && candidate < AddCalendarMonths(MinMonth, 1)) return true;
        return false;
    }

    private (DateOnly start, DateOnly end) CalendarLastMonthRange(DateOnly today)
    {
        var thisMonthStart = CalendarMonthStart(CalendarYear(today), CalendarMonth(today));
        var lastMonthEnd = thisMonthStart.AddDays(-1);
        var lastMonthStart = CalendarMonthStart(CalendarYear(lastMonthEnd), CalendarMonth(lastMonthEnd));
        return (lastMonthStart, lastMonthEnd);
    }

    private enum Preset { Today, Yesterday, Last7Days, Last30Days, ThisMonth, LastMonth }
}
