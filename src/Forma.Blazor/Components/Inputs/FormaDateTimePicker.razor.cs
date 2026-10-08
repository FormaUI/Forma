namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Browser.Interop;
using Forma.Framework.Browser.Interop.Focus;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>A field for a date with its time, the date picked from a calendar and the time typed or chosen from a list of suggested times, under one label.</summary>
/// <typeparam name="TValue">The value the field binds, a <see cref="DateTime"/> or a <see cref="DateTimeOffset"/>, or a nullable one.</typeparam>
public sealed partial class FormaDateTimePicker<TValue> : FormaInputBase<TValue>
{
    // The form an entry takes between the controls and the base's parsing: the date, a separator and the time, the same in every
    // culture and to the tick, either half empty while the reader has not entered it.
    private const string DateFormat = "yyyy-MM-dd";
    private const string TimeFormat = "HH:mm:ss.FFFFFFF";
    private const char EntrySeparator = 'T';

    private static readonly TimeSpan DefaultListInterval = TimeSpan.FromMinutes(30);

    /// <summary>The field's label and the name of its group.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether a value that can be empty is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the field is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the field is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The earliest date with a time the field accepts.</summary>
    [Parameter] public TValue? Min { get; set; }

    /// <summary>The latest date with a time the field accepts.</summary>
    [Parameter] public TValue? Max { get; set; }

    /// <summary>The distance the times the field accepts lie apart, counted from <see cref="Min"/>'s time of day or from midnight.</summary>
    [Parameter] public TimeSpan? Step { get; set; }

    /// <summary>The distance the suggested times lie apart.</summary>
    [Parameter] public TimeSpan? ListInterval { get; set; }

    /// <summary>The offset a <see cref="DateTimeOffset"/> entered where the value was empty takes.</summary>
    [Parameter] public TimeSpan Offset { get; set; }

    /// <summary>Whether months come from the culture's non-Gregorian calendar, such as the Hebrew calendar for <c>he</c>.</summary>
    [Parameter] public bool UseNonGregorianMonths { get; set; }

    private static readonly Type UnderlyingType = Nullable.GetUnderlyingType(typeof(TValue)) ?? typeof(TValue);
    private static readonly bool IsNullable = Nullable.GetUnderlyingType(typeof(TValue)) is not null;

    private static readonly FocusWatchOptions FocusOptions = new();

    private DomObservation<FocusWatchOptions, FocusState>? _focusState;
    private bool _isWithin;

    // What the reader has entered, each half held apart until both are there; the time as read, or the text that could not be.
    private DateOnly? _date;
    private TimeOnly? _time;
    private string? _unreadTimeText;

    // The bound value the entry was last taken from.
    private TValue? _enteredValue;
    private bool _hasEnteredValue;

    private IReadOnlyList<TimeOnly> _suggestions = [];
    private (TimeOnly Origin, TimeSpan Interval, TimeOnly Earliest, TimeOnly Latest)? _suggestionsSource;

    private FormaField? FieldContainer { get; set; }

    /// <inheritdoc />
    public override ElementReference RootElement
    {
        get => FieldContainer?.RootElement ?? default;
        protected set { }
    }

    private bool IsRequired
        => Required || !IsNullable;

    private string DateId
        => $"{ElementId}-date";

    private string DateNameId
        => $"{ElementId}-date-name";

    private string TimeId
        => $"{ElementId}-time";

    private string FieldName
        => DisplayName ?? Label;

    private string? TimeText
        => _unreadTimeText ?? (_time is { } time ? FormatTime(time) : null);

    // The two controls wear the field's box and its validation state.
    private string? ControlClass
        => new CssClassBuilder().Add("forma-field-input").Add(CssClass).ToStringOrNull();

    private bool IsInvalid
        => EditContext is { } editContext && editContext.GetValidationMessages(FieldIdentifier).Any();

    private IReadOnlyDictionary<string, object>? DateAttributes
        => IsInvalid ? new Dictionary<string, object> { ["aria-invalid"] = "true" } : null;

    private Dictionary<string, object> TimeAttributes
    {
        get
        {
            var attributes = new Dictionary<string, object> { ["aria-label"] = L["Time"].Value };

            if (IsInvalid)
            {
                attributes["aria-invalid"] = "true";
            }

            return attributes;
        }
    }

    // One half entered and the other not, which waits for the reader rather than reporting while they are between the two.
    private bool IsHalfEntered
        => (_date is null) != (_time is null && _unreadTimeText is null);

    // The offset an entry is read at, and the kind it takes: the bound value's own, or the host's offset and no kind for a new one.
    private TimeSpan EntryOffset
        => Value is DateTimeOffset current ? current.Offset : Offset;

    private DateTimeKind EntryKind
        => Value is DateTime current ? current.Kind : DateTimeKind.Unspecified;

    private DateTime? MinClock
        => GetClock(Min);

    private DateTime? MaxClock
        => GetClock(Max);

    private DateOnly? MinDate
        => MinClock is { } min ? DateOnly.FromDateTime(min) : null;

    private DateOnly? MaxDate
        => MaxClock is { } max ? DateOnly.FromDateTime(max) : null;

    private TimeOnly StepOrigin
        => MinClock is { } min ? TimeOnly.FromDateTime(min) : TimeOnly.MinValue;

    private TimeSpan EffectiveListInterval
        => ListInterval ?? Step ?? DefaultListInterval;

    // Seconds show wherever the time, a bound or a distance has them, so 09:30:45 never reads as 09:30.
    private bool HasSeconds
        => _time is { Second: not 0 }
            || MinClock is { Second: not 0 }
            || MaxClock is { Second: not 0 }
            || (Step is { } step && step.Ticks % TimeSpan.TicksPerMinute != 0)
            || EffectiveListInterval.Ticks % TimeSpan.TicksPerMinute != 0;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (UnderlyingType != typeof(DateTime) && UnderlyingType != typeof(DateTimeOffset))
        {
            throw new InvalidOperationException($"{nameof(FormaDateTimePicker<TValue>)} binds a {nameof(DateTime)} or a {nameof(DateTimeOffset)}, or a nullable one, not a {typeof(TValue).Name}.");
        }

        EditContext?.OnValidationRequested += HandleValidationRequested;

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

        if (MinClock is { } min && MaxClock is { } max && min > max)
        {
            throw new ArgumentException($"{nameof(Min)} ({min}) must not be later than {nameof(Max)} ({max}).");
        }

        if (!_hasEnteredValue || !EqualityComparer<TValue>.Default.Equals(Value, _enteredValue))
        {
            _hasEnteredValue = true;
            _enteredValue = Value;
            TakeEntry(Value);
        }

        UpdateSuggestions();
    }

    /// <inheritdoc />
    protected override async Task OnAfterRenderAsync(bool firstRender)
    {
        if (FieldContainer is not { } fieldContainer)
        {
            return;
        }

        if (firstRender)
        {
            _focusState = await Context.DomWatcher.GetElement(fieldContainer.RootElement).Focus.WatchAsync(HandleFocusStateAsync);
        }

        if (_focusState is { } focusState)
        {
            await focusState.SyncAsync(fieldContainer.RootElement, FocusOptions);
        }
    }

    // A first state is no leave, so a half the reader left missing is reported once the reader leaves the field after being within it.
    private Task HandleFocusStateAsync(FocusState state)
    {
        var hasLeft = _isWithin && !state.IsWithin;
        _isWithin = state.IsWithin;

        return hasLeft ? InvokeAsync(CommitHalfEntry) : Task.CompletedTask;
    }

    private void CommitHalfEntry()
    {
        if (IsHalfEntered)
        {
            Commit();
            StateHasChanged();
        }
    }

    // A bound value's own clock: a DateTime as it reads, a DateTimeOffset at its own offset.
    private static DateTime? GetValueClock(TValue? value)
        => value switch
        {
            DateTime dateTime => dateTime,
            DateTimeOffset dateTimeOffset => dateTimeOffset.DateTime,
            _ => null,
        };

    private static bool TryReadTime(string text, out TimeOnly time)
        => TimeOnly.TryParseExact(text, TimeFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out time)
            || TimeOnly.TryParse(text, CultureInfo.CurrentCulture, DateTimeStyles.None, out time);

    // A bound's clock at the entry's offset, where the bound is set.
    private DateTime? GetClock(TValue? bound)
        => bound switch
        {
            DateTime dateTime when dateTime != default => dateTime,
            DateTimeOffset dateTimeOffset when dateTimeOffset != default => dateTimeOffset.ToOffset(EntryOffset).DateTime,
            _ => null,
        };

    private void TakeEntry(TValue? value)
    {
        var clock = GetValueClock(value);
        _date = clock is { } dateClock ? DateOnly.FromDateTime(dateClock) : null;
        _time = clock is { } timeClock ? TimeOnly.FromDateTime(timeClock) : null;
        _unreadTimeText = null;
    }

    // The times the date allows, at the list's interval from the step's origin.
    private void UpdateSuggestions()
    {
        var earliest = _date is { } date && date == MinDate ? StepOrigin : TimeOnly.MinValue;
        var latest = _date is { } lastDate && lastDate == MaxDate && MaxClock is { } max ? TimeOnly.FromDateTime(max) : TimeOnly.MaxValue;
        var source = (StepOrigin, EffectiveListInterval, earliest, latest);

        if (_suggestionsSource == source)
        {
            return;
        }

        _suggestionsSource = source;

        var interval = EffectiveListInterval.Ticks;
        var first = StepOrigin.Ticks % interval;
        var count = (TimeSpan.TicksPerDay - 1 - first) / interval + 1;

        _suggestions = [.. Enumerable.Range(0, (int)count)
            .Select(index => new TimeOnly(first + interval * index))
            .Where(time => time >= earliest && time <= latest)];
    }

    private string FormatTime(TimeOnly time)
        => time.ToString(HasSeconds ? "T" : "t", CultureInfo.CurrentCulture);

    private string FormatClock(DateTime clock)
        => clock.ToString(HasSeconds ? "G" : "g", CultureInfo.CurrentCulture);

    private void HandleChooseDate(DateOnly date)
    {
        _date = date;
        Enter();
    }

    private void HandleClearDate()
    {
        _date = null;
        Enter();
    }

    private void HandleChangeTime(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            _time = null;
            _unreadTimeText = null;
        }
        else if (TryReadTime(text.Trim(), out var time))
        {
            _time = time;
            _unreadTimeText = null;
        }
        else
        {
            _time = null;
            _unreadTimeText = text;
        }

        Enter();
    }

    private void HandleChooseTime(TimeOnly time)
    {
        _time = time;
        _unreadTimeText = null;
        Enter();
    }

    // A form submitted with one half entered reports the other before it validates.
    private void HandleValidationRequested(object? sender, ValidationRequestedEventArgs args)
    {
        if (IsHalfEntered)
        {
            Commit();
        }
    }

    private void Enter()
    {
        UpdateSuggestions();

        if (!IsHalfEntered)
        {
            Commit();
        }
    }

    private void Commit()
    {
        var date = _date is { } entered ? entered.ToString(DateFormat, CultureInfo.InvariantCulture) : string.Empty;
        var time = _unreadTimeText ?? _time?.ToString(TimeFormat, CultureInfo.InvariantCulture) ?? string.Empty;

        CurrentValueAsString = date.Length == 0 && time.Length == 0 ? string.Empty : $"{date}{EntrySeparator}{time}";
    }

    // The value a date with its time makes, keeping the bound value's kind or offset.
    private TValue Create(DateTime clock)
        => UnderlyingType == typeof(DateTimeOffset)
            ? (TValue)(object)new DateTimeOffset(clock, EntryOffset)
            : (TValue)(object)DateTime.SpecifyKind(clock, EntryKind);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-date-time-picker");

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value)
        => GetValueClock(value) is { } clock
            ? $"{clock.ToString(DateFormat, CultureInfo.InvariantCulture)}{EntrySeparator}{clock.ToString(TimeFormat, CultureInfo.InvariantCulture)}"
            : null;

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        validationErrorMessage = Read(value, out result);

        return validationErrorMessage is null;
    }

    // The value an entry writes, or why the field cannot take it.
    private string? Read(string? value, out TValue result)
    {
        result = default!;

        if (string.IsNullOrEmpty(value))
        {
            return IsNullable ? null : L["The {Field} field is required.", FieldName];
        }

        var separator = value.IndexOf(EntrySeparator, StringComparison.Ordinal);
        var dateText = value[..separator];
        var timeText = value[(separator + 1)..];
        DateOnly? date = DateOnly.TryParseExact(dateText, DateFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate) ? parsedDate : null;
        TimeOnly? time = null;

        if (timeText.Length > 0)
        {
            if (!TryReadTime(timeText.Trim(), out var parsedTime))
            {
                return L["The {Field} field must be a time.", FieldName];
            }

            time = parsedTime;
        }

        if (date is not { } day)
        {
            return L["The {Field} field needs a date.", FieldName];
        }

        if (time is not { } timeOfDay)
        {
            return L["The {Field} field needs a time.", FieldName];
        }

        var clock = day.ToDateTime(timeOfDay);
        var earliest = MinClock;
        var latest = MaxClock;

        if ((earliest is { } min && clock < min) || (latest is { } max && clock > max))
        {
            return (earliest, latest) switch
            {
                ({ } from, { } to) => L["The {Field} field must be between {Earliest} and {Latest}.", FieldName, FormatClock(from), FormatClock(to)],
                ({ } from, null) => L["The {Field} field must be {Earliest} or later.", FieldName, FormatClock(from)],
                _ => L["The {Field} field must be {Latest} or earlier.", FieldName, FormatClock(latest!.Value)],
            };
        }

        // A time off the step names the two times either side of it that the step reaches.
        if (Step is { } step)
        {
            var offset = (timeOfDay.Ticks - StepOrigin.Ticks + TimeSpan.TicksPerDay) % TimeSpan.TicksPerDay;
            var remainder = offset % step.Ticks;

            if (remainder != 0)
            {
                var earlier = timeOfDay.Add(TimeSpan.FromTicks(-remainder));
                var later = earlier.Add(step);

                return L["The {Field} field must be {Earlier} or {Later}.", FieldName, FormatTime(earlier), FormatTime(later)];
            }
        }

        result = Create(clock);

        return null;
    }

    /// <inheritdoc />
    protected override async ValueTask DisposeAsyncCore()
    {
        EditContext?.OnValidationRequested -= HandleValidationRequested;

        await base.DisposeAsyncCore();
    }
}
