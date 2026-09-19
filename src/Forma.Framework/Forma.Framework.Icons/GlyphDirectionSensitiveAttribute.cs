namespace Forma.Framework.Icons;

/// <summary>Marks a glyph whose presentation requires adaptation for the specified text direction: it is drawn for the other direction and turned over when text runs this way.</summary>
/// <param name="direction">The text direction the glyph adapts for, as the HTML <c>dir</c> token: <c>rtl</c> or <c>ltr</c>.</param>
[AttributeUsage(AttributeTargets.Field)]
internal sealed class GlyphDirectionSensitiveAttribute(string direction) : Attribute
{
    /// <summary>The text direction the glyph adapts for, as the HTML <c>dir</c> token.</summary>
    public string Direction { get; } = direction;
}
