namespace Forma.Framework.Css;

using System.Text.RegularExpressions;

/// <summary>A color as a style takes it: a literal <see cref="CssColor"/>, or the custom property a theme defines one in.</summary>
public abstract partial record CssColorValue
{
    /// <summary>A literal color.</summary>
    /// <param name="color">The color to paint with.</param>
    public static CssColorValue FromColor(CssColor color)
        => new LiteralColorValue(color);

    /// <summary>The color a custom property holds, read where the style applies.</summary>
    /// <param name="propertyName">The custom property's name, such as <c>--success-subtle-color</c>.</param>
    /// <exception cref="InvalidOperationException"><paramref name="propertyName"/> isn't a custom property's name.</exception>
    public static CssColorValue FromProperty(string propertyName)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(propertyName);

        if (!PropertyNamePattern().IsMatch(propertyName))
        {
            throw new InvalidOperationException($"'{propertyName}' isn't a custom property's name, which starts with two hyphens and holds letters, digits, hyphens and underscores.");
        }

        return new PropertyColorValue(propertyName);
    }

    /// <summary>Takes a literal color.</summary>
    public static implicit operator CssColorValue(CssColor color)
    {
        return FromColor(color);
    }

    /// <summary>Closes the hierarchy to the kinds of value this project defines.</summary>
    private protected CssColorValue()
    {
    }

    [GeneratedRegex("^--[A-Za-z0-9_-]+$")]
    private static partial Regex PropertyNamePattern();

    private sealed record LiteralColorValue(CssColor Color) : CssColorValue
    {
        /// <summary>Renders the color as its own CSS.</summary>
        public override string ToString()
            => Color.ToString();
    }

    private sealed record PropertyColorValue(string PropertyName) : CssColorValue
    {
        /// <summary>Renders the property's reference, <c>var(--name)</c>.</summary>
        public override string ToString()
            => $"var({PropertyName})";
    }
}
