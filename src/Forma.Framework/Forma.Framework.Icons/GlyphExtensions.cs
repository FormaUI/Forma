namespace Forma.Framework.Icons;

using System.Collections.Frozen;
using System.Reflection;

/// <summary>What a glyph declares about itself beyond its name.</summary>
public static class GlyphExtensions
{
    private static readonly FrozenDictionary<Glyph, string> SensitiveDirections = typeof(Glyph)
        .GetFields(BindingFlags.Public | BindingFlags.Static)
        .Select(field => (Glyph: (Glyph)field.GetValue(null)!, Sensitivity: field.GetCustomAttribute<GlyphDirectionSensitiveAttribute>()))
        .Where(entry => entry.Sensitivity is not null)
        .ToFrozenDictionary(entry => entry.Glyph, entry => entry.Sensitivity!.Direction);

    /// <summary>Gets the text direction the glyph's presentation adapts for, as the HTML <c>dir</c> token, or null where the glyph reads the same either way.</summary>
    /// <param name="glyph">The glyph.</param>
    public static string? GetDirectionSensitivity(this Glyph glyph)
        => SensitiveDirections.GetValueOrDefault(glyph);

    /// <summary>Whether the glyph's presentation adapts for a text direction.</summary>
    /// <param name="glyph">The glyph.</param>
    /// <param name="direction">The text direction, as the HTML <c>dir</c> token.</param>
    public static bool IsDirectionSensitive(this Glyph glyph, string direction)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(direction);

        return string.Equals(SensitiveDirections.GetValueOrDefault(glyph), direction, StringComparison.OrdinalIgnoreCase);
    }
}
