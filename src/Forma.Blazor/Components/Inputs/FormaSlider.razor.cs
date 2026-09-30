namespace Forma.Blazor.Components.Inputs;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Numerics;

using Forma.Framework.Css;

using Microsoft.AspNetCore.Components;

/// <summary>A bounded numeric input rendered as a native range slider, with its current value shown beside the label.</summary>
public sealed partial class FormaSlider<TValue> : FormaInputBase<TValue> where TValue : struct, INumber<TValue>
{
    /// <summary>The slider's label.</summary>
    [Parameter, EditorRequired] public string Label { get; set; } = default!;

    /// <summary>The lowest value the slider can reach.</summary>
    [Parameter, EditorRequired] public TValue Min { get; set; }

    /// <summary>The highest value the slider can reach.</summary>
    [Parameter, EditorRequired] public TValue Max { get; set; }

    /// <summary>The increment a drag or arrow key moves the value by.</summary>
    [Parameter] public TValue Step { get; set; } = TValue.One;

    /// <summary>A short suffix shown after the value, such as "px", "%" or "s".</summary>
    [Parameter] public string? Unit { get; set; }

    /// <summary>Whether the slider is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    // The visible reading, hidden from assistive technology, which hears the input's own value text.
    private string DisplayValue
        => Unit is null
            ? CurrentValue.ToString(null, CultureInfo.CurrentCulture)
            : $"{CurrentValue.ToString(null, CultureInfo.CurrentCulture)}{Unit}";

    // The reading with its unit, where null leaves the input's native value to be announced.
    private string? AriaValueText
        => Unit is null ? null : DisplayValue;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(Label);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(Min, Max, nameof(Min));
        ArgumentOutOfRangeException.ThrowIfLessThanOrEqual(Step, TValue.Zero);
    }

    // The HTML min, max and step attributes take an invariant number.
    private static string FormatInvariant(TValue value)
        => value.ToString(null, CultureInfo.InvariantCulture);

    /// <inheritdoc />
    protected override string? FormatValueAsString(TValue value)
        => FormatInvariant(value);

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-slider");

    // A range input always holds a valid number.
    /// <inheritdoc />
    protected override bool TryParseValueFromString(string? value, out TValue result, [NotNullWhen(false)] out string? validationErrorMessage)
    {
        result = TValue.TryParse(value, CultureInfo.InvariantCulture, out var parsed) ? parsed : Min;
        validationErrorMessage = null;
        return true;
    }
}
