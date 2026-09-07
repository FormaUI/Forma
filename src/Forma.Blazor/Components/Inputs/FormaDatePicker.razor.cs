namespace Forma.Blazor.Components.Inputs;

using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>A calendar for picking a single date, or a date range when the range parameters are bound.</summary>
public sealed partial class FormaDatePicker : ComponentBase
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

    private readonly string _triggerId = $"dp-trigger-{Guid.NewGuid():N}";
    private ElementReference _anchorElement;
    private ElementReference _triggerElement;
    private FormaPopover Popover { get; set; } = default!;
    private DateOnly _leftMonth;
    private DateOnly _rightMonth;
    private readonly DateRangeDraft _draft = new();
    private CalendarMonthWindow _calendar = default!;

    private bool IsOpen => Popover is { IsOpen: true };
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
        GetPanelMonth(panelIndex) > (IsRangeMode && panelIndex == 1 ? _calendar.AddMonths(_calendar.MinMonth, 1) : _calendar.MinMonth);

    private bool CanPanelGoNext(int panelIndex) =>
        GetPanelMonth(panelIndex) < (IsRangeMode && panelIndex == 0 ? _calendar.AddMonths(_calendar.MaxMonth, -1) : _calendar.MaxMonth);

    // ── Lifecycle ────────────────────────────────────

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);

        _calendar = CalendarMonthWindow.Create(Culture, UseNonGregorianMonths, MinDate, MaxDate);

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
            _rightMonth = _calendar.Clamp(_calendar.GetMonthStart(_calendar.GetYear(endRef), _calendar.GetMonth(endRef)));

            var startRef = StartDate ?? endRef;
            _leftMonth = _calendar.Clamp(_calendar.GetMonthStart(_calendar.GetYear(startRef), _calendar.GetMonth(startRef)));

            if (_leftMonth >= _rightMonth)
            {
                _leftMonth = _calendar.AddMonths(_rightMonth, -1);
                if (_leftMonth < _calendar.MinMonth)
                {
                    _leftMonth = _calendar.MinMonth;
                }
            }
        }
        else
        {
            var reference = Value ?? DateOnly.FromDateTime(DateTime.Today);
            _leftMonth = _calendar.Clamp(_calendar.GetMonthStart(_calendar.GetYear(reference), _calendar.GetMonth(reference)));
        }
    }

    // ── Popover ────────────────────────────────────────

    private async Task ToggleAsync()
    {
        if (IsOpen)
        {
            await Popover.CloseAsync();
            return;
        }

        _draft.Clear();
        ResetDisplayMonth();
        await Popover.OpenAsync(_triggerElement);
    }

    // ── Navigation ───────────────────────────────────

    private void PanelPrev(int panelIndex)
    {
        if (CanPanelGoPrev(panelIndex))
        {
            SetPanelMonth(panelIndex, _calendar.AddMonths(GetPanelMonth(panelIndex), -1));
        }
    }

    private void PanelNext(int panelIndex)
    {
        if (CanPanelGoNext(panelIndex))
        {
            SetPanelMonth(panelIndex, _calendar.AddMonths(GetPanelMonth(panelIndex), 1));
        }
    }

    private void OnMonthDropdownChange(int panelIndex, ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var month))
        {
            var current = GetPanelMonth(panelIndex);
            SetPanelMonth(panelIndex, _calendar.GetMonthStart(_calendar.GetYear(current), month));
        }
    }

    private void OnYearDropdownChange(int panelIndex, ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var gregorianYear))
        {
            var current = GetPanelMonth(panelIndex);
            var calMonth = _calendar.GetMonth(current);
            // Find a Gregorian date in the target year, then build from calendar coordinates
            var midYear = new DateOnly(gregorianYear, 6, 1);
            var targetCalYear = _calendar.GetYear(midYear);
            var maxMonth = _calendar.GetMonthsInYear(targetCalYear);
            if (calMonth > maxMonth)
            {
                calMonth = maxMonth;
            }

            SetPanelMonth(panelIndex, _calendar.GetMonthStart(targetCalYear, calMonth));
        }
    }

    private void SetPanelMonth(int panelIndex, DateOnly month)
    {
        month = _calendar.Clamp(month);
        if (panelIndex == 0)
        {
            _leftMonth = month;
            if (IsRangeMode && _leftMonth >= _rightMonth)
            {
                _rightMonth = _calendar.AddMonths(_leftMonth, 1);
                if (_rightMonth > _calendar.MaxMonth)
                {
                    _rightMonth = _calendar.MaxMonth;
                    _leftMonth = _calendar.AddMonths(_rightMonth, -1);
                }
            }
        }
        else
        {
            _rightMonth = month;
            if (_rightMonth <= _leftMonth)
            {
                _leftMonth = _calendar.AddMonths(_rightMonth, -1);
                if (_leftMonth < _calendar.MinMonth)
                {
                    _leftMonth = _calendar.MinMonth;
                    _rightMonth = _calendar.AddMonths(_leftMonth, 1);
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
                await Popover.CloseAsync();
            }
        }
        else
        {
            Value = date;
            await ValueChanged.InvokeAsync(date);
            await OnSelectionComplete.InvokeAsync();
            await Popover.CloseAsync();
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
        await Popover.CloseAsync();
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
        _rightMonth = _calendar.Clamp(_calendar.GetMonthStart(_calendar.GetYear(end), _calendar.GetMonth(end)));
        _leftMonth = _calendar.Clamp(_calendar.GetMonthStart(_calendar.GetYear(start), _calendar.GetMonth(start)));
        if (_leftMonth >= _rightMonth)
        {
            _leftMonth = _calendar.AddMonths(_rightMonth, -1);
            if (_leftMonth < _calendar.MinMonth)
            {
                _leftMonth = _calendar.MinMonth;
            }
        }
        _draft.Clear();
        await StartDateChanged.InvokeAsync(start);
        await EndDateChanged.InvokeAsync(end);
        await OnSelectionComplete.InvokeAsync();
        await Popover.CloseAsync();
    }

    private (DateOnly start, DateOnly end) ComputePresetRange(Preset preset, DateOnly today) =>
        preset switch
        {
            Preset.Today => (today, today),
            Preset.Yesterday => (today.AddDays(-1), today.AddDays(-1)),
            Preset.Last7Days => (today.AddDays(-(ShortRangeDays - 1)), today),
            Preset.Last30Days => (today.AddDays(-(LongRangeDays - 1)), today),
            Preset.ThisMonth => (_calendar.GetMonthStart(_calendar.GetYear(today), _calendar.GetMonth(today)), today),
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
            if (candidate < _calendar.MinMonth || candidate > _calendar.MaxMonth)
            {
                continue;
            }

            if (IsRangeMode && panelIndex == 0 && candidate > _calendar.AddMonths(_calendar.MaxMonth, -1))
            {
                continue;
            }

            if (IsRangeMode && panelIndex == 1 && candidate < _calendar.AddMonths(_calendar.MinMonth, 1))
            {
                continue;
            }

            return false;
        }
        return true;
    }

    private bool IsMonthDisabled(int calYear, int calMonth, int panelIndex)
    {
        if (calMonth > _calendar.GetMonthsInYear(calYear))
        {
            return true;
        }

        var candidate = _calendar.GetMonthStart(calYear, calMonth);
        if (candidate < _calendar.MinMonth || candidate > _calendar.MaxMonth)
        {
            return true;
        }

        if (IsRangeMode && panelIndex == 0 && candidate > _calendar.AddMonths(_calendar.MaxMonth, -1))
        {
            return true;
        }

        if (IsRangeMode && panelIndex == 1 && candidate < _calendar.AddMonths(_calendar.MinMonth, 1))
        {
            return true;
        }

        return false;
    }

    private (DateOnly start, DateOnly end) CalendarLastMonthRange(DateOnly today)
    {
        var thisMonthStart = _calendar.GetMonthStart(_calendar.GetYear(today), _calendar.GetMonth(today));
        var lastMonthEnd = thisMonthStart.AddDays(-1);
        var lastMonthStart = _calendar.GetMonthStart(_calendar.GetYear(lastMonthEnd), _calendar.GetMonth(lastMonthEnd));
        return (lastMonthStart, lastMonthEnd);
    }

    private enum Preset { Today, Yesterday, Last7Days, Last30Days, ThisMonth, LastMonth }
}
