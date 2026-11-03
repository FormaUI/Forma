namespace Forma.Framework.Localization;

/// <summary>One run of a composite format string: either literal text or a hole naming an argument.</summary>
public readonly ref struct LocalizedSegment
{
    internal LocalizedSegment(ReadOnlySpan<char> text, ReadOnlySpan<char> name, int alignment, ReadOnlySpan<char> format)
    {
        Text = text;
        Name = name;
        Alignment = alignment;
        Format = format;
    }

    /// <summary>The name the hole gives its argument.</summary>
    public ReadOnlySpan<char> Name { get; }

    /// <summary>The width the hole pads its value to, negative for left alignment and zero for none.</summary>
    public int Alignment { get; }

    /// <summary>The format specifier the hole renders its value through.</summary>
    public ReadOnlySpan<char> Format { get; }

    /// <summary>Whether the segment names an argument rather than carrying literal text.</summary>
    public bool IsHole => !Name.IsEmpty;

    /// <summary>The literal text.</summary>
    public ReadOnlySpan<char> Text { get; }

    /// <summary>Enumerates a composite format string's literal runs and holes.</summary>
    /// <param name="format">The composite format to walk.</param>
    public static LocalizedSegmentEnumerator Parse(ReadOnlySpan<char> format)
        => new(format);
}
