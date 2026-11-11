namespace Forma.Framework.Localization;

using System.Buffers;
using System.Globalization;

/// <summary>Yields the literal runs and holes of a composite format string as slices of the original.</summary>
/// <param name="format">The composite format to walk.</param>
public ref struct LocalizedSegmentEnumerator(ReadOnlySpan<char> format)
{
    private static readonly SearchValues<char> Braces = SearchValues.Create(['{', '}']);

    private ReadOnlySpan<char> _remainingFormat = format;

    /// <summary>The segment the last <see cref="MoveNext"/> produced.</summary>
    public LocalizedSegment Current { get; private set; } = default;

    /// <summary>Returns this enumerator.</summary>
    public readonly LocalizedSegmentEnumerator GetEnumerator()
        => this;

    /// <summary>Advances to the next literal run or hole.</summary>
    public bool MoveNext()
    {
        if (_remainingFormat.Length == 0)
        {
            return false;
        }

        var brace = _remainingFormat.IndexOfAny(Braces);
        if (brace < 0)
        {
            return TakeText(_remainingFormat.Length);
        }

        if (brace > 0)
        {
            return TakeText(brace);
        }

        // A doubled brace stands for one literal brace.
        if (_remainingFormat.Length > 1 && _remainingFormat[1] == _remainingFormat[0])
        {
            Current = new LocalizedSegment(_remainingFormat[..1], default, 0, default);
            _remainingFormat = _remainingFormat[2..];

            return true;
        }

        if (_remainingFormat[0] == '}')
        {
            return TakeText(1);
        }

        var close = _remainingFormat.IndexOf('}');
        if (close < 0)
        {
            return TakeText(_remainingFormat.Length);
        }

        return TryReadHole(_remainingFormat[1..close], out var name, out var alignment, out var format)
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
        Current = new LocalizedSegment(_remainingFormat[..length], default, 0, default);
        _remainingFormat = _remainingFormat[length..];

        return true;
    }

    private bool TakeHole(int length, ReadOnlySpan<char> name, int alignment, ReadOnlySpan<char> format)
    {
        Current = new LocalizedSegment(default, name, alignment, format);
        _remainingFormat = _remainingFormat[length..];

        return true;
    }
}
