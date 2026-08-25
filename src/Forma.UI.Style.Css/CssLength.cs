namespace Forma.UI.Style.Css;

using System.Globalization;

/// <summary>An immutable measurement that renders as a CSS length value.</summary>
public readonly record struct CssLength
{
    private CssLength(double value, CssLengthUnit unit)
    {
        Value = value;
        Unit = unit;
    }

    /// <summary>The unit the measurement is expressed in.</summary>
    public CssLengthUnit Unit { get; }

    /// <summary>The magnitude, in <see cref="Unit"/>.</summary>
    public double Value { get; }

    private string Suffix
        => Unit switch
        {
            CssLengthUnit.Ch => "ch",
            CssLengthUnit.Em => "em",
            CssLengthUnit.Percent => "%",
            CssLengthUnit.Px => "px",
            CssLengthUnit.Rem => "rem",
            _ => throw new InvalidOperationException($"'{Unit}' is not a known {nameof(CssLengthUnit)}."),
        };

    /// <summary>Creates a length in <c>ch</c>, sized by the width of the font's zero glyph.</summary>
    public static CssLength FromCh(double value)
        => Create(value, CssLengthUnit.Ch);

    /// <summary>Creates a length in <c>em</c>, relative to the element's own font size.</summary>
    public static CssLength FromEm(double value)
        => Create(value, CssLengthUnit.Em);

    /// <summary>Creates a length as a percentage of the containing block.</summary>
    public static CssLength FromPercent(double value)
        => Create(value, CssLengthUnit.Percent);

    /// <summary>Creates a length in <c>px</c>.</summary>
    public static CssLength FromPx(double value)
        => Create(value, CssLengthUnit.Px);

    /// <summary>Creates a length in <c>rem</c>, relative to the root font size.</summary>
    public static CssLength FromRem(double value)
        => Create(value, CssLengthUnit.Rem);

    /// <summary>Renders the length as a CSS value such as <c>20rem</c> or <c>50%</c>.</summary>
    public override string ToString()
        => string.Create(CultureInfo.InvariantCulture, $"{Value.ToString("0.####", CultureInfo.InvariantCulture)}{Suffix}");

    private static CssLength Create(double value, CssLengthUnit unit)
    {
        if (!double.IsFinite(value))
        {
            throw new ArgumentOutOfRangeException(nameof(value), value, "A CSS length must be a finite number.");
        }

        return new CssLength(value, unit);
    }
}
