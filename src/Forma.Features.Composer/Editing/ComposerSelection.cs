namespace Forma.Features.Composer.Editing;

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

    // The compiler-generated equality would compare ActiveStyles by reference, since HashSet<T> does not
    // override Equals; two selections with the same styles by content would then never compare equal.
    /// <inheritdoc />
    public bool Equals(ComposerSelection? other)
        => other is not null
        && Start == other.Start
        && End == other.End
        && string.Equals(BlockType, other.BlockType, StringComparison.Ordinal)
        && ActiveStyles.SetEquals(other.ActiveStyles);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Start);
        hash.Add(End);
        hash.Add(BlockType, StringComparer.Ordinal);

        foreach (var style in ActiveStyles.Order(StringComparer.OrdinalIgnoreCase))
        {
            hash.Add(style, StringComparer.OrdinalIgnoreCase);
        }

        return hash.ToHashCode();
    }
}
