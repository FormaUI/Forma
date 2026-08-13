namespace Forma.Framework.Localization;

using System.Buffers;
using System.Globalization;

/// <summary>Yields the literal runs and holes of a composite format string as slices of the original.</summary>
/// <param name="format">The composite format to walk.</param>
public ref struct LocalizedSegmentEnumerator(ReadOnlySpan<char> format)
{
    private static readonly SearchValues<char> Braces = SearchValues.Create(['{', '}']);

    private ReadOnlySpan<char> _remaining = format;

    /// <summary>The segment the last <see cref="MoveNext"/> produced.</summary>
    public LocalizedSegment Current { get; private set; } = default;

    /// <summary>Returns this enumerator, so it can drive a foreach directly.</summary>
    public readonly LocalizedSegmentEnumerator GetEnumerator()
        => this;

    /// <summary>Advances to the next literal run or hole.</summary>
    public bool MoveNext()
    {
        if (_remaining.Length == 0)
        {
            return false;
        }

        var brace = _remaining.IndexOfAny(Braces);
        if (brace < 0)
        {
            return Take(_remaining.Length, -1);
        }

        if (brace > 0)
        {
            return Take(brace, -1);
        }

        // A doubled brace stands for one literal brace, which is why the pair is yielded as a run of its own.
        if (_remaining.Length > 1 && _remaining[1] == _remaining[0])
        {
            Current = new LocalizedSegment(_remaining[..1], -1);
            _remaining = _remaining[2..];

            return true;
        }

        if (_remaining[0] == '}')
        {
            return Take(1, -1);
        }

        var close = _remaining.IndexOf('}');
        if (close < 0)
        {
            return Take(_remaining.Length, -1);
        }

        return TryReadIndex(_remaining[1..close], out var index)
            ? Take(close + 1, index)
            : Take(close + 1, -1);
    }

    // A hole may carry alignment and a format specifier, neither of which survives into a segment.
    private static bool TryReadIndex(ReadOnlySpan<char> hole, out int index)
    {
        var end = hole.IndexOfAny(',', ':');
        var digits = end < 0 ? hole : hole[..end];

        return int.TryParse(digits, NumberStyles.None, CultureInfo.InvariantCulture, out index);
    }

    private bool Take(int length, int argumentIndex)
    {
        Current = new LocalizedSegment(argumentIndex < 0 ? _remaining[..length] : default, argumentIndex);
        _remaining = _remaining[length..];

        return true;
    }
}
