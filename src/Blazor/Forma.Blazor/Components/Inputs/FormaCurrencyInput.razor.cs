namespace Forma.Blazor.Components.Inputs;

using System.Globalization;
using System.Linq.Expressions;

using Forma.Framework.Css;
using Forma.Framework.Currencies;

using Microsoft.AspNetCore.Components;

/// <summary>A field for an amount of money in a currency the application names, typed as the formatting culture writes a number beside the symbol it writes the currency with.</summary>
/// <typeparam name="TValue">The amount the field binds, a <see cref="decimal"/> or a nullable one.</typeparam>
public sealed partial class FormaCurrencyInput<TValue> : FormaElementBase
{
    /// <summary>The amount.</summary>
    [Parameter] public TValue? Value { get; set; }

    /// <summary>Raised with the amount once the reader has changed it.</summary>
    [Parameter] public EventCallback<TValue> ValueChanged { get; set; }

    /// <summary>The model member the amount is bound to, which names the field its validation messages belong to.</summary>
    [Parameter] public Expression<Func<TValue>>? ValueExpression { get; set; }

    /// <summary>The ISO 4217 code of the amount's currency, such as <c>USD</c>.</summary>
    [Parameter, EditorRequired] public string CurrencyCode { get; set; } = default!;

    /// <summary>The label of the field the amount sits in, pinned above the amount.</summary>
    [Parameter] public string? Label { get; set; }

    /// <summary>The name the field's validation messages give it.</summary>
    [Parameter] public string? DisplayName { get; set; }

    /// <summary>The placeholder text shown when the amount is empty.</summary>
    [Parameter] public string? Placeholder { get; set; }

    /// <summary>The instructions shown beneath the amount, which describe it.</summary>
    [Parameter] public string? Description { get; set; }

    /// <summary>Whether an amount that can be empty is marked as required.</summary>
    [Parameter] public bool Required { get; set; }

    /// <summary>Whether the field is disabled.</summary>
    [Parameter] public bool Disabled { get; set; }

    /// <summary>Whether the field is read-only.</summary>
    [Parameter] public bool ReadOnly { get; set; }

    /// <summary>The smallest amount the field accepts.</summary>
    [Parameter] public decimal? Min { get; set; }

    /// <summary>The largest amount the field accepts.</summary>
    [Parameter] public decimal? Max { get; set; }

    /// <summary>How far the vertical arrows move the amount.</summary>
    [Parameter] public decimal Step { get; set; } = 1;

    /// <summary>The most decimal places an amount the field accepts carries, where null takes the currency's minor units.</summary>
    [Parameter] public int? MaxDecimalPlaces { get; set; }

    /// <summary>How wide the field stands, where null fills the space it is given.</summary>
    [Parameter] public CssLength? Width { get; set; }

    private int _maxDecimalPlaces;
    private int _minDecimalPlaces;

    private FormaNumberInput<TValue>? NumberInput { get; set; }

    // The culture's currency pattern puts the symbol before the number, $n or $ n, or after it, n$ or n $.
    private NumberAdornment Adornment
        => new(
            CurrencyCatalog.GetSymbol(CurrencyCode, Context.CultureState.Culture),
            Context.CultureState.Culture.NumberFormat.CurrencyPositivePattern is 1 or 3 ? AdornmentSide.End : AdornmentSide.Start,
            CurrencyCode);

    /// <inheritdoc />
    public override ElementReference RootElement
    {
        get => NumberInput?.RootElement ?? default;
        protected set { }
    }

    /// <inheritdoc />
    protected override void OnInitialized()
    {
        if (typeof(TValue) != typeof(decimal) && typeof(TValue) != typeof(decimal?))
        {
            throw new InvalidOperationException($"{nameof(FormaCurrencyInput<TValue>)} binds a {nameof(Decimal)} or a nullable one, not a {typeof(TValue).Name}.");
        }

        base.OnInitialized();
    }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(CurrencyCode);

        if (!CurrencyCatalog.Contains(CurrencyCode))
        {
            throw new ArgumentException($"'{CurrencyCode}' is not the ISO 4217 code of a currency in use.", nameof(CurrencyCode));
        }

        if (MaxDecimalPlaces is { } maxDecimalPlaces)
        {
            ArgumentOutOfRangeException.ThrowIfNegative(maxDecimalPlaces, nameof(MaxDecimalPlaces));
        }

        // The currency's minor units are the places an amount is shown with and, unless the host allows more, the most it takes.
        var minorUnits = CurrencyCatalog.GetMinorUnits(CurrencyCode);
        _maxDecimalPlaces = MaxDecimalPlaces ?? minorUnits;
        _minDecimalPlaces = Math.Min(minorUnits, _maxDecimalPlaces);
    }

    /// <inheritdoc />
    protected override void BuildRootClass(CssClassBuilder builder)
        => builder.Add("forma-currency-input");
}
