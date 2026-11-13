namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Models;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A field for picking a range of dates from two months side by side, with presets for the usual spans.</summary>
/// <remarks>The range is one value, committed once both ends are picked.</remarks>
public sealed partial class FormaDateRangePicker : FormaInputBase<DateRange?>
{
    // The rolling spans the Last7Days and Last30Days presets cover, ending today.
    private const int ShortRangeDays = 7;
    private const int LongRangeDays = 30;

    /// <summary>The field's label and accessible name.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether the field is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The earliest selectable date.</summary>
    [Parameter] public DateOnly? MinDate { get; set; }

    /// <summary>The latest selectable date.</summary>
    [Parameter] public DateOnly? MaxDate { get; set; }

    /// <summary>Whether the quick-range presets are offered.</summary>
    [Parameter] public bool ShowPresets { get; set; } = true;

    /// <summary>Raised once both ends of a range have been picked.</summary>
    [Parameter] public EventCallback OnSelectionComplete { get; set; }

    /// <summary>Whether months come from the culture's non-Gregorian calendar, such as the Hebrew calendar for <c>he</c>.</summary>
    [Parameter] public bool UseNonGregorianMonths { get; set; }

    private ElementReference _anchorElement;
    private FormaPopover Popover { get; set; } = default!;
    private readonly DateRangeDraft _draft = new();
    private CalendarMonthRange _calendarMonthRange = default!;

    private bool IsOpen
        => Popover is { IsOpen: true };

    private CultureInfo Culture => Context.CultureState.Culture;
    private string TextDirection => Context.CultureState.IsRightToLeft ? "rtl" : "ltr";

    private bool HasValue
        => Value is not null;

    private string DisplayText
        => Value is { } range ? $"{FormatDate(range.Start)} - {FormatDate(range.End)}" : string.Empty;

    // The span being drawn while a first day is anchored; otherwise the committed range.
    private DateRange? Highlight
        => _draft.Span is { } span ? new DateRange(span.From, span.To) : Value;

    /// <inheritdoc />
    protected override bool CanFocus
        => !Disabled && base.CanFocus;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);

        _calendarMonthRange = CalendarMonthRange.Create(Culture, UseNonGregorianMonths, MinDate, MaxDate);

        if (MinDate.HasValue && MaxDate.HasValue && MinDate.Value > MaxDate.Value)
        {
            throw new ArgumentException($"{nameof(MinDate)} ({MinDate}) must not be greater than {nameof(MaxDate)} ({MaxDate}).");
        }
    }

    private string FormatDate(DateOnly date)
        => date.ToDateTime(TimeOnly.MinValue).ToString("d", Culture);

    private async Task ToggleAsync()
    {
        if (IsOpen)
        {
            await Popover.CloseAsync();
            return;
        }

        _draft.Clear();
        await Popover.OpenAsync(RootElement);
    }

    private async Task SelectDayAsync(DateOnly date)
    {
        if (!_draft.IsAnchored)
        {
            _draft.AnchorAt(date);

            return;
        }

        var (start, end) = _draft.CompleteAt(date);
        await CommitAsync(new DateRange(start, end));
    }

    private void HoverDay(DateOnly date)
        => _draft.HoverAt(date);

    private async Task HandleClearAsync()
    {
        CurrentValue = null;
        _draft.Clear();
        await Popover.CloseAsync();
    }

    private async Task CommitAsync(DateRange range)
    {
        CurrentValue = range;
        _draft.Clear();
        await OnSelectionComplete.InvokeAsync();
        await Popover.CloseAsync();
    }

    // ── Presets ──────────────────────────────────────

    private async Task ApplyPresetAsync(Preset preset)
    {
        if (GetPresetRange(preset) is { } range)
        {
            await CommitAsync(range);
        }
    }

    // The preset's span held within the allowed dates, or null when nothing of it remains.
    private DateRange? GetPresetRange(Preset preset)
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

        return start > end ? null : new DateRange(start, end);
    }

    private bool IsPresetDisabled(Preset preset)
        => GetPresetRange(preset) is null;

    private (DateOnly Start, DateOnly End) ComputePresetRange(Preset preset, DateOnly today) =>
        preset switch
        {
            Preset.Today => (today, today),
            Preset.Yesterday => (today.AddDays(-1), today.AddDays(-1)),
            Preset.Last7Days => (today.AddDays(-(ShortRangeDays - 1)), today),
            Preset.Last30Days => (today.AddDays(-(LongRangeDays - 1)), today),
            Preset.ThisMonth => (_calendarMonthRange.GetMonthStart(_calendarMonthRange.GetYear(today), _calendarMonthRange.GetMonth(today)), today),
            Preset.LastMonth => GetLastMonthRange(today),
            _ => (today, today)
        };

    private (DateOnly Start, DateOnly End) GetLastMonthRange(DateOnly today)
    {
        var thisMonthStart = _calendarMonthRange.GetMonthStart(_calendarMonthRange.GetYear(today), _calendarMonthRange.GetMonth(today));
        var lastMonthEnd = thisMonthStart.AddDays(-1);
        var lastMonthStart = _calendarMonthRange.GetMonthStart(_calendarMonthRange.GetYear(lastMonthEnd), _calendarMonthRange.GetMonth(lastMonthEnd));

        return (lastMonthStart, lastMonthEnd);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-date-range-picker-trigger");

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out DateRange? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = Value;
        validationErrorMessage = null;

        return true;
    }

    private enum Preset { Today, Yesterday, Last7Days, Last30Days, ThisMonth, LastMonth }
}
