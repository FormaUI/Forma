namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A field for a time of day, typed as the formatting culture writes it or chosen from a list of suggested times.</summary>
/// <typeparam name="TValue">The time the field binds, a <see cref="TimeOnly"/> or a nullable one.</typeparam>
public sealed partial class FormaTimePicker<TValue> : FormaInputBase<TValue>
{
    // The form a chosen time takes between the list and the base's parsing, the same in every culture and to the tick.
    private const string RoundTripFormat = "HH:mm:ss.FFFFFFF";

    private static readonly TimeSpan DefaultListInterval = TimeSpan.FromMinutes(30);

    /// <summary>The field's label and accessible name.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether a time that can be empty is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the field is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the field is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The earliest time the field accepts, which begins the range and the steps.</summary>
    [Parameter] public TimeOnly? Min { get; set; }

    /// <summary>The latest time the field accepts, earlier than <see cref="Min"/> for a range across midnight.</summary>
    [Parameter] public TimeOnly? Max { get; set; }

    /// <summary>The distance the times the field accepts lie apart, counted from <see cref="Min"/> or from midnight.</summary>
    [Parameter] public TimeSpan? Step { get; set; }

    /// <summary>The distance the suggested times lie apart.</summary>
    [Parameter] public TimeSpan? ListInterval { get; set; }

    private static readonly bool IsNullable = typeof(TValue) == typeof(TimeOnly?);

    private IReadOnlyList<TimeOnly> _suggestions = [];
    private (TimeOnly? Min, TimeOnly? Max, TimeSpan Interval)? _suggestionsSource;

    // Text the field could not read stays its value, so a time a later choice writes replaces it even where it is the time it held.
    private string? _unreadText;
    private TValue? _valueWhenUnread;

    private FormaTimeBox? Box { get; set; }

    /// <inheritdoc />
    public override ElementReference RootElement
    {
        get => Box?.InputElement ?? default;
        protected set { }
    }

    private bool IsRequired
        => Required || !IsNullable;

    private TimeOnly? Time
        => (TimeOnly?)(object?)Value;

    private TimeSpan EffectiveListInterval
        => ListInterval ?? Step ?? DefaultListInterval;

    // Seconds show wherever the time, a limit or a distance has them, so 09:30:45 never reads as 09:30.
    private bool HasSeconds
        => HasSecondsIn(Time) || HasSecondsIn(Min) || HasSecondsIn(Max)
            || (Step is { } step && step.Ticks % TimeSpan.TicksPerMinute != 0)
            || EffectiveListInterval.Ticks % TimeSpan.TicksPerMinute != 0;

    private string FieldName
        => DisplayName ?? Label;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (typeof(TValue) != typeof(TimeOnly) && !IsNullable)
        {
            throw new InvalidOperationException($"{nameof(FormaTimePicker<TValue>)} binds a {nameof(TimeOnly)} or a nullable one, not a {typeof(TValue).Name}.");
        }

        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);

        if (Step is { } step)
        {
            ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(step, TimeSpan.Zero, nameof(Step));
            ArgumentOutOfRangeException.ThrowIfGreaterThanOrEqual(step, TimeSpan.FromDays(1), nameof(Step));
        }

        if (ListInterval is { } interval)
        {
            ArgumentOutOfRangeException.ThrowIfLessThan(interval, TimeSpan.FromSeconds(1), nameof(ListInterval));
            ArgumentOutOfRangeException.ThrowIfGreaterThan(interval, TimeSpan.FromDays(1), nameof(ListInterval));

            if (Step is { } stepWithin && interval.Ticks % stepWithin.Ticks != 0)
            {
                throw new ArgumentException($"{nameof(ListInterval)} ({interval}) must be a whole number of {nameof(Step)}s ({stepWithin}), so every suggested time is one the field accepts.", nameof(ListInterval));
            }
        }

        var source = (Min, Max, EffectiveListInterval);

        if (_suggestionsSource != source)
        {
            _suggestionsSource = source;
            _suggestions = CreateSuggestions(Min, Max, EffectiveListInterval);
        }

        if (_unreadText is not null && !EqualityComparer<TValue>.Default.Equals(Value, _valueWhenUnread))
        {
            _unreadText = null;
        }
    }

    // The suggested times from the range's start, or midnight, to its end, round midnight where the range runs across it.
    private static TimeOnly[] CreateSuggestions(TimeOnly? min, TimeOnly? max, TimeSpan interval)
    {
        var start = min ?? TimeOnly.MinValue;
        var span = max is { } end
            ? (end.Ticks - start.Ticks + TimeSpan.TicksPerDay) % TimeSpan.TicksPerDay
            : TimeSpan.TicksPerDay - 1 - start.Ticks;
        var count = (int)(span / interval.Ticks) + 1;

        return [.. Enumerable.Range(0, count).Select(index => start.Add(TimeSpan.FromTicks(interval.Ticks * index)))];
    }

    private static bool HasSecondsIn(TimeOnly? time)
        => time is { } value && value.Second != 0;

    private string FormatTime(TimeOnly time)
        => time.ToString(HasSeconds ? "T" : "t", CultureInfo.CurrentCulture);

    private void HandleChange(string? text)
    {
        _unreadText = null;
        CurrentValueAsString = text;
    }

    private void HandleChoose(TimeOnly time)
    {
        _unreadText = null;
        CurrentValueAsString = time.ToString(RoundTripFormat, CultureInfo.InvariantCulture);
    }

    // Inside the range, inclusive at both ends; a range whose end comes before its start runs through midnight.
    private bool IsInRange(TimeOnly time)
        => (Min, Max) switch
        {
            ({ } min, { } max) when min <= max => time >= min && time <= max,
            ({ } min, { } max) => time >= min || time <= max,
            ({ } min, null) => time >= min,
            (null, { } max) => time <= max,
            _ => true,
        };

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-time-picker-input");

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value)
        => (TimeOnly?)(object?)value is { } time ? FormatTime(time) : null;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        validationErrorMessage = Read(value, out result);

        if (validationErrorMessage is null)
        {
            return true;
        }

        _unreadText = value ?? string.Empty;
        _valueWhenUnread = Value;

        return false;
    }

    // The time the text writes, or why the field cannot take it.
    private string? Read(string? value, out TValue result)
    {
        result = default!;

        if (string.IsNullOrWhiteSpace(value))
        {
            return IsNullable ? null : L["The {Field} field is required.", FieldName];
        }

        var text = value.Trim();

        if (!TimeOnly.TryParseExact(text, RoundTripFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var time)
            && !TimeOnly.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out time))
        {
            return L["The {Field} field must be a time.", FieldName];
        }

        if (!IsInRange(time))
        {
            return (Min, Max) switch
            {
                ({ } min, { } max) => L["The {Field} field must be between {Earliest} and {Latest}.", FieldName, FormatTime(min), FormatTime(max)],
                ({ } min, null) => L["The {Field} field must be {Earliest} or later.", FieldName, FormatTime(min)],
                _ => L["The {Field} field must be {Latest} or earlier.", FieldName, FormatTime(Max!.Value)],
            };
        }

        // A time off the step names the two times either side of it that the step reaches.
        if (Step is { } step)
        {
            var origin = Min ?? TimeOnly.MinValue;
            var offset = (time.Ticks - origin.Ticks + TimeSpan.TicksPerDay) % TimeSpan.TicksPerDay;
            var remainder = offset % step.Ticks;

            if (remainder != 0)
            {
                var earlier = time.Add(TimeSpan.FromTicks(-remainder));
                var later = earlier.Add(step);

                return L["The {Field} field must be {Earlier} or {Later}.", FieldName, FormatTime(earlier), FormatTime(later)];
            }
        }

        result = (TValue)(object)time;

        return null;
    }
}
