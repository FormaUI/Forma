using System.Globalization;

namespace Forma.Web.Css;

/// <summary>An immutable RGBA colour that renders as a CSS colour value.</summary>
public readonly record struct CssColor
{
    private const double OpaqueAlpha = 1d;

    private CssColor(byte red, byte green, byte blue, double alpha)
    {
        Red = red;
        Green = green;
        Blue = blue;
        Alpha = alpha;
    }

    /// <summary>Opacity, from 0 (transparent) to 1 (opaque).</summary>
    public double Alpha { get; }

    /// <summary>Blue channel, from 0 to 255.</summary>
    public byte Blue { get; }

    /// <summary>Green channel, from 0 to 255.</summary>
    public byte Green { get; }

    /// <summary>Whether the colour is fully opaque.</summary>
    public bool IsOpaque
        => Alpha >= OpaqueAlpha;

    /// <summary>Red channel, from 0 to 255.</summary>
    public byte Red { get; }

    /// <summary>Creates an opaque colour from a packed 24-bit value such as <c>0x2c384a</c>.</summary>
    public static CssColor FromHex(int value)
        => FromHex(value, OpaqueAlpha);

    /// <summary>Creates a colour from a packed 24-bit value and an opacity between 0 and 1.</summary>
    public static CssColor FromHex(int value, double alpha)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(value);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(value, 0xFFFFFF);
        ArgumentOutOfRangeException.ThrowIfLessThan(alpha, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alpha, OpaqueAlpha);

        return new CssColor(
            (byte)((value >> 16) & 0xFF),
            (byte)((value >> 8) & 0xFF),
            (byte)(value & 0xFF),
            alpha);
    }

    /// <summary>Creates an opaque colour from its red, green, and blue channels.</summary>
    public static CssColor FromRgb(byte red, byte green, byte blue)
        => new(red, green, blue, OpaqueAlpha);

    /// <summary>Creates a colour from its red, green, and blue channels and an opacity between 0 and 1.</summary>
    public static CssColor FromRgba(byte red, byte green, byte blue, double alpha)
    {
        ArgumentOutOfRangeException.ThrowIfLessThan(alpha, 0);
        ArgumentOutOfRangeException.ThrowIfGreaterThan(alpha, OpaqueAlpha);

        return new CssColor(red, green, blue, alpha);
    }

    /// <summary>Renders the colour as <c>#rrggbb</c>, ignoring opacity.</summary>
    public string ToHexString()
        => string.Create(CultureInfo.InvariantCulture, $"#{Red:x2}{Green:x2}{Blue:x2}");

    /// <summary>Renders the colour as <c>rgba(r, g, b, a)</c>.</summary>
    public string ToRgbaString()
        => string.Create(
            CultureInfo.InvariantCulture,
            $"rgba({Red}, {Green}, {Blue}, {Alpha.ToString("0.####", CultureInfo.InvariantCulture)})");

    /// <summary>Renders the colour as the shortest CSS value that preserves it.</summary>
    public override string ToString()
        => IsOpaque ? ToHexString() : ToRgbaString();
}
