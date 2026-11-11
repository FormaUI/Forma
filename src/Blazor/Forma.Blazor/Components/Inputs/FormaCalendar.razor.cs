namespace Forma.Blazor.Components.Inputs;

using System.Globalization;

using Forma.Blazor.Models;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>A calendar of one or two months side by side that highlights a date range and reports the day a reader picks or hovers.</summary>
public sealed partial class FormaCalendar : FormaComponentBase
{
    /// <summary>How many months stand side by side, one or two.</summary>
    [Parameter] public int MonthCount { get; set; } = 1;

    /// <summary>The dates drawn as chosen, a single day or a span.</summary>
    [Parameter] public DateRange? Highlight { get; set; }

    /// <summary>The earliest selectable date.</summary>
    [Parameter] public DateOnly? MinDate { get; set; }

    /// <summary>The latest selectable date.</summary>
    [Parameter] public DateOnly? MaxDate { get; set; }

    /// <summary>Whether months come from the culture's non-Gregorian calendar, such as the Hebrew calendar for <c>he</c>.</summary>
    [Parameter] public bool UseNonGregorianMonths { get; set; }

    /// <summary>Raised with the day a reader picks.</summary>
    [Parameter] public EventCallback<DateOnly> OnDaySelected { get; set; }

    /// <summary>Raised with the day the pointer crosses.</summary>
    [Parameter] public EventCallback<DateOnly> OnDayHovered { get; set; }

    private DateOnly _leftMonth;
    private DateOnly _rightMonth;
    private CalendarMonthRange _calendarMonthRange = default!;

    private bool ShowsTwoMonths
        => MonthCount == 2;

    private CultureInfo Culture => Context.CultureState.Culture;
    private string TextDirection => Context.CultureState.IsRightToLeft ? "rtl" : "ltr";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(MonthCount, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(MonthCount, 2);

        _calendarMonthRange = CalendarMonthRange.Create(Culture, UseNonGregorianMonths, MinDate, MaxDate);

        if (MinDate.HasValue && MaxDate.HasValue && MinDate.Value > MaxDate.Value)
        {
            throw new ArgumentException($"{nameof(MinDate)} ({MinDate}) must not be greater than {nameof(MaxDate)} ({MaxDate}).");
        }

        if (_leftMonth == default)
        {
            ShowHighlightedMonths();
        }
        else
        {
            ReckonShownMonths();
        }
    }

    // A month kept from an earlier render is the one holding it in the calendar now in force, whose months may begin on other days.
    private void ReckonShownMonths()
    {
        _leftMonth = GetMonthStart(_leftMonth);

        if (ShowsTwoMonths)
        {
            _rightMonth = GetMonthStart(_rightMonth);

            if (_leftMonth >= _rightMonth)
            {
                _rightMonth = _calendarMonthRange.AddMonths(_leftMonth, 1);
            }
        }
    }

    // The months first shown: the highlight's last day on the right and its first day on the left, or today.
    private void ShowHighlightedMonths()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (ShowsTwoMonths)
        {
            var endReference = Highlight?.End ?? today;
            _rightMonth = _calendarMonthRange.Clamp(GetMonthStart(endReference));

            var startReference = Highlight?.Start ?? endReference;
            _leftMonth = _calendarMonthRange.Clamp(GetMonthStart(startReference));

            if (_leftMonth >= _rightMonth)
            {
                _leftMonth = _calendarMonthRange.AddMonths(_rightMonth, -1);
                if (_leftMonth < _calendarMonthRange.MinMonth)
                {
                    _leftMonth = _calendarMonthRange.MinMonth;
                }
            }
        }
        else
        {
            _leftMonth = _calendarMonthRange.Clamp(GetMonthStart(Highlight?.Start ?? today));
        }
    }

    private DateOnly GetMonthStart(DateOnly date)
        => _calendarMonthRange.GetMonthStart(_calendarMonthRange.GetYear(date), _calendarMonthRange.GetMonth(date));

    private DateOnly GetPanelMonth(int panelIndex)
        => panelIndex == 0 ? _leftMonth : _rightMonth;

    private bool CanPanelGoPrev(int panelIndex)
        => GetPanelMonth(panelIndex) > (ShowsTwoMonths && panelIndex == 1 ? _calendarMonthRange.AddMonths(_calendarMonthRange.MinMonth, 1) : _calendarMonthRange.MinMonth);

    private bool CanPanelGoNext(int panelIndex)
        => GetPanelMonth(panelIndex) < (ShowsTwoMonths && panelIndex == 0 ? _calendarMonthRange.AddMonths(_calendarMonthRange.MaxMonth, -1) : _calendarMonthRange.MaxMonth);

    private void ShowPreviousPanel(int panelIndex)
    {
        if (CanPanelGoPrev(panelIndex))
        {
            SetPanelMonth(panelIndex, _calendarMonthRange.AddMonths(GetPanelMonth(panelIndex), -1));
        }
    }

    private void ShowNextPanel(int panelIndex)
    {
        if (CanPanelGoNext(panelIndex))
        {
            SetPanelMonth(panelIndex, _calendarMonthRange.AddMonths(GetPanelMonth(panelIndex), 1));
        }
    }

    private void OnMonthDropdownChange(int panelIndex, ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var month))
        {
            var panelMonth = GetPanelMonth(panelIndex);
            SetPanelMonth(panelIndex, _calendarMonthRange.GetMonthStart(_calendarMonthRange.GetYear(panelMonth), month));
        }
    }

    private void OnYearDropdownChange(int panelIndex, ChangeEventArgs args)
    {
        if (int.TryParse(args.Value?.ToString(), out var gregorianYear))
        {
            var panelMonth = GetPanelMonth(panelIndex);
            var calendarMonth = _calendarMonthRange.GetMonth(panelMonth);
            // The year selector holds Gregorian years; the calendar year is the one in force at mid-year.
            var midYear = new DateOnly(gregorianYear, 6, 1);
            var targetCalendarYear = _calendarMonthRange.GetYear(midYear);
            var maxMonth = _calendarMonthRange.GetMonthsInYear(targetCalendarYear);
            if (calendarMonth > maxMonth)
            {
                calendarMonth = maxMonth;
            }

            SetPanelMonth(panelIndex, _calendarMonthRange.GetMonthStart(targetCalendarYear, calendarMonth));
        }
    }

    private void SetPanelMonth(int panelIndex, DateOnly month)
    {
        month = _calendarMonthRange.Clamp(month);
        if (panelIndex == 0)
        {
            _leftMonth = month;
            if (ShowsTwoMonths && _leftMonth >= _rightMonth)
            {
                _rightMonth = _calendarMonthRange.AddMonths(_leftMonth, 1);
                if (_rightMonth > _calendarMonthRange.MaxMonth)
                {
                    _rightMonth = _calendarMonthRange.MaxMonth;
                    _leftMonth = _calendarMonthRange.AddMonths(_rightMonth, -1);
                }
            }
        }
        else
        {
            _rightMonth = month;
            if (_rightMonth <= _leftMonth)
            {
                _leftMonth = _calendarMonthRange.AddMonths(_rightMonth, -1);
                if (_leftMonth < _calendarMonthRange.MinMonth)
                {
                    _leftMonth = _calendarMonthRange.MinMonth;
                    _rightMonth = _calendarMonthRange.AddMonths(_leftMonth, 1);
                }
            }
        }
    }

    private Task SelectDayAsync(DateOnly date)
        => IsDayDisabled(date) ? Task.CompletedTask : OnDaySelected.InvokeAsync(date);

    private EventCallback<MouseEventArgs> GetHoverHandler(DateOnly date)
        => OnDayHovered.HasDelegate ? EventCallback.Factory.Create<MouseEventArgs>(this, () => OnDayHovered.InvokeAsync(date)) : default;

    private bool IsDayDisabled(DateOnly date) =>
        (MinDate.HasValue && date < MinDate.Value) ||
        (MaxDate.HasValue && date > MaxDate.Value);

    private static bool IsToday(string dayClasses)
        => dayClasses.Contains("today", StringComparison.Ordinal);

    private static bool IsSelected(string dayClasses)
        => dayClasses.Contains("selected", StringComparison.Ordinal);

    private string GetDayClasses(DateOnly date)
    {
        var today = DateOnly.FromDateTime(DateTime.Today);
        var classes = string.Empty;

        if (date == today)
        {
            classes += " today";
        }

        if (IsDayDisabled(date))
        {
            classes += " unavailable";
        }

        if (Highlight is not { } highlight)
        {
            return classes;
        }

        if (highlight.IsSingleDay)
        {
            if (date == highlight.Start)
            {
                classes += " selected";
            }
        }
        else if (date == highlight.Start)
        {
            classes += " selected start";
        }
        else if (date == highlight.End)
        {
            classes += " selected end";
        }
        else if (highlight.Contains(date))
        {
            classes += " in-range";
        }

        return classes;
    }

    private bool IsYearDisabled(int gregorianYear, int panelIndex)
    {
        for (var month = 1; month <= 12; month++)
        {
            var candidate = new DateOnly(gregorianYear, month, 1);
            if (candidate < _calendarMonthRange.MinMonth || candidate > _calendarMonthRange.MaxMonth)
            {
                continue;
            }

            if (ShowsTwoMonths && panelIndex == 0 && candidate > _calendarMonthRange.AddMonths(_calendarMonthRange.MaxMonth, -1))
            {
                continue;
            }

            if (ShowsTwoMonths && panelIndex == 1 && candidate < _calendarMonthRange.AddMonths(_calendarMonthRange.MinMonth, 1))
            {
                continue;
            }

            return false;
        }
        return true;
    }

    private bool IsMonthDisabled(int calendarYear, int calendarMonth, int panelIndex)
    {
        if (calendarMonth > _calendarMonthRange.GetMonthsInYear(calendarYear))
        {
            return true;
        }

        var candidate = _calendarMonthRange.GetMonthStart(calendarYear, calendarMonth);
        if (candidate < _calendarMonthRange.MinMonth || candidate > _calendarMonthRange.MaxMonth)
        {
            return true;
        }

        if (ShowsTwoMonths && panelIndex == 0 && candidate > _calendarMonthRange.AddMonths(_calendarMonthRange.MaxMonth, -1))
        {
            return true;
        }

        if (ShowsTwoMonths && panelIndex == 1 && candidate < _calendarMonthRange.AddMonths(_calendarMonthRange.MinMonth, 1))
        {
            return true;
        }

        return false;
    }
}
