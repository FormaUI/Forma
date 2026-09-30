namespace Forma.Blazor.Components.Inputs;

using System.Globalization;

using Forma.Blazor.Models;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Web;

/// <summary>One or two months side by side, moved by their arrows and selectors within the dates allowed, drawing the dates it is told to highlight and reporting the day a reader picks or hovers.</summary>
public sealed partial class FormaCalendar : FormaComponentBase
{
    /// <summary>How many months stand side by side: one, or two for a range.</summary>
    [Parameter] public int Months { get; set; } = 1;

    /// <summary>The dates drawn as chosen: a single day, or a span whose first and last days are its ends.</summary>
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
    private CalendarMonthRange _calendar = default!;

    private bool ShowsTwoMonths
        => Months == 2;

#pragma warning disable CA1822 // Intentionally instance — reads ambient CultureInfo per render
    private CultureInfo Culture => CultureInfo.CurrentCulture;
#pragma warning restore CA1822
    private string TextDirection => Culture.TextInfo.IsRightToLeft ? "rtl" : "ltr";

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(Months, 1);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Months, 2);

        _calendar = CalendarMonthRange.Create(Culture, UseNonGregorianMonths, MinDate, MaxDate);

        if (MinDate.HasValue && MaxDate.HasValue && MinDate.Value > MaxDate.Value)
        {
            throw new ArgumentException($"{nameof(MinDate)} ({MinDate}) must not be greater than {nameof(MaxDate)} ({MaxDate}).");
        }

        if (_leftMonth == default)
        {
            ShowHighlightedMonths();
        }
    }

    // The months first shown: the highlight's last day on the right and its first day on the left, or today.
    private void ShowHighlightedMonths()
    {
        var today = DateOnly.FromDateTime(DateTime.Today);

        if (ShowsTwoMonths)
        {
            var endReference = Highlight?.End ?? today;
            _rightMonth = _calendar.Clamp(GetMonthStart(endReference));

            var startReference = Highlight?.Start ?? endReference;
            _leftMonth = _calendar.Clamp(GetMonthStart(startReference));

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
            _leftMonth = _calendar.Clamp(GetMonthStart(Highlight?.Start ?? today));
        }
    }

    private DateOnly GetMonthStart(DateOnly date)
        => _calendar.GetMonthStart(_calendar.GetYear(date), _calendar.GetMonth(date));

    private DateOnly GetPanelMonth(int panelIndex)
        => panelIndex == 0 ? _leftMonth : _rightMonth;

    private bool CanPanelGoPrev(int panelIndex)
        => GetPanelMonth(panelIndex) > (ShowsTwoMonths && panelIndex == 1 ? _calendar.AddMonths(_calendar.MinMonth, 1) : _calendar.MinMonth);

    private bool CanPanelGoNext(int panelIndex)
        => GetPanelMonth(panelIndex) < (ShowsTwoMonths && panelIndex == 0 ? _calendar.AddMonths(_calendar.MaxMonth, -1) : _calendar.MaxMonth);

    private void ShowPreviousPanel(int panelIndex)
    {
        if (CanPanelGoPrev(panelIndex))
        {
            SetPanelMonth(panelIndex, _calendar.AddMonths(GetPanelMonth(panelIndex), -1));
        }
    }

    private void ShowNextPanel(int panelIndex)
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
            var calendarMonth = _calendar.GetMonth(current);
            // Find a Gregorian date in the target year, then build from calendar coordinates
            var midYear = new DateOnly(gregorianYear, 6, 1);
            var targetCalendarYear = _calendar.GetYear(midYear);
            var maxMonth = _calendar.GetMonthsInYear(targetCalendarYear);
            if (calendarMonth > maxMonth)
            {
                calendarMonth = maxMonth;
            }

            SetPanelMonth(panelIndex, _calendar.GetMonthStart(targetCalendarYear, calendarMonth));
        }
    }

    private void SetPanelMonth(int panelIndex, DateOnly month)
    {
        month = _calendar.Clamp(month);
        if (panelIndex == 0)
        {
            _leftMonth = month;
            if (ShowsTwoMonths && _leftMonth >= _rightMonth)
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

    private Task SelectDayAsync(DateOnly date)
        => IsDayDisabled(date) ? Task.CompletedTask : OnDaySelected.InvokeAsync(date);

    // A day listens for the pointer only while someone wants to hear where it is.
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
            if (candidate < _calendar.MinMonth || candidate > _calendar.MaxMonth)
            {
                continue;
            }

            if (ShowsTwoMonths && panelIndex == 0 && candidate > _calendar.AddMonths(_calendar.MaxMonth, -1))
            {
                continue;
            }

            if (ShowsTwoMonths && panelIndex == 1 && candidate < _calendar.AddMonths(_calendar.MinMonth, 1))
            {
                continue;
            }

            return false;
        }
        return true;
    }

    private bool IsMonthDisabled(int calendarYear, int calendarMonth, int panelIndex)
    {
        if (calendarMonth > _calendar.GetMonthsInYear(calendarYear))
        {
            return true;
        }

        var candidate = _calendar.GetMonthStart(calendarYear, calendarMonth);
        if (candidate < _calendar.MinMonth || candidate > _calendar.MaxMonth)
        {
            return true;
        }

        if (ShowsTwoMonths && panelIndex == 0 && candidate > _calendar.AddMonths(_calendar.MaxMonth, -1))
        {
            return true;
        }

        if (ShowsTwoMonths && panelIndex == 1 && candidate < _calendar.AddMonths(_calendar.MinMonth, 1))
        {
            return true;
        }

        return false;
    }
}
