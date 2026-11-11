namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>A range of characters inside the text that was parsed.</summary>
/// <param name="Start">The index of the first character.</param>
/// <param name="Length">The number of characters.</param>
public readonly record struct LocaleTextSpan(int Start, int Length)
{
    /// <summary>The index one past the last character.</summary>
    public int End
        => Start + Length;

    /// <summary>Whether the span covers no characters.</summary>
    public bool IsEmpty
        => Length == 0;

    /// <summary>The characters this span covers, taken from the text it was parsed from.</summary>
    /// <param name="text">The text the span indexes into.</param>
    public ReadOnlySpan<char> Slice(string text)
    {
        ArgumentNullException.ThrowIfNull(text);

        return text.AsSpan(Start, Length);
    }

    /// <summary>The index of the first occurrence of a character within this span, or -1 when absent.</summary>
    /// <param name="text">The text the span indexes into.</param>
    /// <param name="value">The character to look for.</param>
    public int IndexOf(string text, char value)
    {
        ArgumentNullException.ThrowIfNull(text);

        var foundIndex = Slice(text).IndexOf(value);

        return foundIndex < 0 ? -1 : Start + foundIndex;
    }
}
