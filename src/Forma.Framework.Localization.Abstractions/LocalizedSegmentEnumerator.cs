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
            return TakeText(_remaining.Length);
        }

        if (brace > 0)
        {
            return TakeText(brace);
        }

        // A doubled brace stands for one literal brace, which is why the pair is yielded as a run of its own.
        if (_remaining.Length > 1 && _remaining[1] == _remaining[0])
        {
            Current = new LocalizedSegment(_remaining[..1], default, 0, default);
            _remaining = _remaining[2..];

            return true;
        }

        if (_remaining[0] == '}')
        {
            return TakeText(1);
        }

        var close = _remaining.IndexOf('}');
        if (close < 0)
        {
            return TakeText(_remaining.Length);
        }

        return TryReadHole(_remaining[1..close], out var name, out var alignment, out var format)
            ? TakeHole(close + 1, name, alignment, format)
            : TakeText(close + 1);
    }

    // A hole is "{name}", "{name,alignment}", "{name:format}" or the two together.
    private static bool TryReadHole(ReadOnlySpan<char> hole, out ReadOnlySpan<char> name, out int alignment, out ReadOnlySpan<char> format)
    {
        alignment = 0;
        format = default;

        var colon = hole.IndexOf(':');
        var head = colon < 0 ? hole : hole[..colon];
        format = colon < 0 ? default : hole[(colon + 1)..];

        var comma = head.IndexOf(',');
        name = comma < 0 ? head : head[..comma];

        if (comma >= 0 && !int.TryParse(head[(comma + 1)..], NumberStyles.AllowLeadingSign, CultureInfo.InvariantCulture, out alignment))
        {
            return false;
        }

        return LocaleName.IsValid(name);
    }

    private bool TakeText(int length)
    {
        Current = new LocalizedSegment(_remaining[..length], default, 0, default);
        _remaining = _remaining[length..];

        return true;
    }

    private bool TakeHole(int length, ReadOnlySpan<char> name, int alignment, ReadOnlySpan<char> format)
    {
        Current = new LocalizedSegment(default, name, alignment, format);
        _remaining = _remaining[length..];

        return true;
    }
}
