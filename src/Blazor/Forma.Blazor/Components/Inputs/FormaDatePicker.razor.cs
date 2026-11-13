namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A field for picking one date from a calendar that opens under it.</summary>
/// <typeparam name="TValue">The date the field binds, a <see cref="DateOnly"/> or a nullable one.</typeparam>
public sealed partial class FormaDatePicker<TValue> : FormaInputBase<TValue>
{
    // The form a date takes between the picker and the base's parsing, the same in every culture.
    private const string RoundTripFormat = "yyyy-MM-dd";

    /// <summary>The field's label and accessible name.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>Whether a date that can be empty is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the field is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>The earliest selectable date.</summary>
    [Parameter] public DateOnly? MinDate { get; set; }

    /// <summary>The latest selectable date.</summary>
    [Parameter] public DateOnly? MaxDate { get; set; }

    /// <summary>Raised once a date has been picked.</summary>
    [Parameter] public EventCallback OnSelectionComplete { get; set; }

    /// <summary>Whether months come from the culture's non-Gregorian calendar, such as the Hebrew calendar for <c>he</c>.</summary>
    [Parameter] public bool UseNonGregorianMonths { get; set; }

    private static readonly bool IsNullable = typeof(TValue) == typeof(DateOnly?);

    // A date that cannot be empty keeps its last value when cleared, while the field shows it cleared until a date is picked or given.
    private bool _isShownCleared;
    private TValue? _valueWhenCleared;

    private FormaDateBox? Box { get; set; }

    /// <inheritdoc />
    public override ElementReference RootElement
    {
        get => Box?.TriggerElement ?? default;
        protected set { }
    }

    // A date that cannot be empty is required whatever the host says, since clearing it reports the field required.
    private bool IsRequired
        => Required || !IsNullable;

    private DateOnly? Date
        => _isShownCleared ? null : ToDate(Value);

    /// <inheritdoc />
    protected override bool CanFocus
        => !Disabled && base.CanFocus;

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (typeof(TValue) != typeof(DateOnly) && !IsNullable)
        {
            throw new InvalidOperationException($"{nameof(FormaDatePicker<TValue>)} binds a {nameof(DateOnly)} or a nullable one, not a {typeof(TValue).Name}.");
        }

        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);

        if (MinDate.HasValue && MaxDate.HasValue && MinDate.Value > MaxDate.Value)
        {
            throw new ArgumentException($"{nameof(MinDate)} ({MinDate}) must not be greater than {nameof(MaxDate)} ({MaxDate}).");
        }

        if (_isShownCleared && !EqualityComparer<TValue>.Default.Equals(Value, _valueWhenCleared))
        {
            _isShownCleared = false;
        }
    }

    private static DateOnly? ToDate(TValue? value)
        => (DateOnly?)(object?)value;

    private async Task SelectDayAsync(DateOnly date)
    {
        _isShownCleared = false;
        CurrentValueAsString = date.ToString(RoundTripFormat, CultureInfo.InvariantCulture);
        await OnSelectionComplete.InvokeAsync();
    }

    // Clearing goes through the base's parsing, so a date that cannot be empty reports its field invalid and keeps its value.
    private void HandleClear()
    {
        _isShownCleared = !IsNullable;
        _valueWhenCleared = Value;
        CurrentValueAsString = null;
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-date-picker-trigger");

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue? value)
        => ToDate(value)?.ToString(RoundTripFormat, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value) && IsNullable)
        {
            result = default!;
            validationErrorMessage = null;

            return true;
        }

        if (DateOnly.TryParseExact(value, RoundTripFormat, CultureInfo.InvariantCulture, DateTimeStyles.None, out var parsedDate))
        {
            result = (TValue)(object)parsedDate;
            validationErrorMessage = null;

            return true;
        }

        // The picker writes only the dates it picks, so what fails is a date that cannot be empty, cleared.
        result = default!;
        validationErrorMessage = L["The {Field} field is required.", DisplayName ?? Label];

        return false;
    }
}
