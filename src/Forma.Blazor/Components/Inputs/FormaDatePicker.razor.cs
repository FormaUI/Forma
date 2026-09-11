namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Models;
using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A field for picking one date from a calendar that opens under it.</summary>
public sealed partial class FormaDatePicker : FormaInputBase<DateOnly?>
{
    /// <summary>The field's label and accessible name.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The earliest selectable date.</summary>
    [Parameter] public DateOnly? MinDate { get; set; }

    /// <summary>The latest selectable date.</summary>
    [Parameter] public DateOnly? MaxDate { get; set; }

    /// <summary>Raised once a date has been picked.</summary>
    [Parameter] public EventCallback OnSelectionComplete { get; set; }

    /// <summary>Whether months come from the culture's non-Gregorian calendar, such as the Hebrew calendar for <c>he</c>.</summary>
    [Parameter] public bool UseNonGregorianMonths { get; set; }

    private ElementReference _anchorElement;
    private FormaPopover Popover { get; set; } = default!;

    private string ElementId
        => Id ?? $"dp-{Context.RuntimeId:N}";

    private bool IsOpen
        => Popover is { IsOpen: true };

#pragma warning disable CA1822 // Intentionally instance — reads ambient CultureInfo per render
    private CultureInfo Culture => CultureInfo.CurrentCulture;
#pragma warning restore CA1822

    private bool HasValue
        => Value is not null;

    private string DisplayText
        => Value is { } value ? value.ToDateTime(TimeOnly.MinValue).ToString("d", Culture) : string.Empty;

    private DateRange? Highlight
        => Value is { } value ? new DateRange(value, value) : null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);

        if (MinDate.HasValue && MaxDate.HasValue && MinDate.Value > MaxDate.Value)
        {
            throw new ArgumentException($"{nameof(MinDate)} ({MinDate}) must not be greater than {nameof(MaxDate)} ({MaxDate}).");
        }
    }

    private async Task ToggleAsync()
    {
        if (IsOpen)
        {
            await Popover.CloseAsync();
            return;
        }

        await Popover.OpenAsync(RootElement);
    }

    private async Task SelectDayAsync(DateOnly date)
    {
        CurrentValue = date;
        await OnSelectionComplete.InvokeAsync();
        await Popover.CloseAsync();
    }

    private async Task HandleClearAsync()
    {
        CurrentValue = null;
        await Popover.CloseAsync();
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder
            .Add("forma-field-input")
            .Add("forma-date-picker-trigger");

    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out DateOnly? result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        if (string.IsNullOrWhiteSpace(value))
        {
            result = null;
            validationErrorMessage = null;

            return true;
        }

        if (DateOnly.TryParse(value, Culture, out var parsed))
        {
            result = parsed;
            validationErrorMessage = null;

            return true;
        }

        result = null;
        validationErrorMessage = L["The {Field} field must be a date.", DisplayName ?? FieldIdentifier.FieldName];

        return false;
    }
}
