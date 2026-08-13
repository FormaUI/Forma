namespace Forma.Framework.Localization;

/// <summary>One run of a composite format string: either literal text or a hole naming an argument.</summary>
public readonly ref struct LocalizedSegment
{
    internal LocalizedSegment(ReadOnlySpan<char> text, int argumentIndex)
    {
        Text = text;
        ArgumentIndex = argumentIndex;
    }

    /// <summary>The argument the hole names, or -1 for literal text.</summary>
    public int ArgumentIndex { get; }

    /// <summary>Whether the segment names an argument rather than carrying literal text.</summary>
    public bool IsHole => ArgumentIndex >= 0;

    /// <summary>The literal text, sliced from the format and empty for a hole.</summary>
    public ReadOnlySpan<char> Text { get; }

    /// <summary>Walks a composite format string as literal runs and holes, without copying either.</summary>
    public static LocalizedSegmentEnumerator Parse(ReadOnlySpan<char> format)
        => new(format);
}
