namespace Forma.UI.Features.Composer.Editing;

/// <summary>A snapshot of the caret and range, as the browser last reported them.</summary>
public sealed record ComposerSelection
{
    /// <summary>The zero-based start offset of the range.</summary>
    public int Start { get; init; }

    /// <summary>The zero-based end offset of the range.</summary>
    public int End { get; init; }

    /// <summary>Whether the range is a caret rather than a span.</summary>
    public bool IsCollapsed
        => Start == End;

    /// <summary>The block the caret sits in, such as "p", "h1" or "pre", or null when unknown.</summary>
    public string? BlockType { get; init; }

    /// <summary>The inline formatting active across the range, such as "bold" or "italic".</summary>
    public IReadOnlySet<string> ActiveStyles { get; init; } = new HashSet<string>(StringComparer.OrdinalIgnoreCase);

    /// <summary>A selection standing for no caret at all.</summary>
    public static ComposerSelection Empty { get; } = new();
}
