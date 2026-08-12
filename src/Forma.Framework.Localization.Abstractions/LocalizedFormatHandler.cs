namespace Forma.Framework.Localization;

using System.Buffers;
using System.Globalization;
using System.Runtime.CompilerServices;

/// <summary>Rebuilds a composite-format key from an interpolated string while capturing the hole values.</summary>
/// <param name="literalLength">The total length of the literal text between holes.</param>
/// <param name="formattedCount">The number of holes.</param>
[InterpolatedStringHandler]
public ref struct LocalizedFormatHandler(int literalLength, int formattedCount)
{
    private static readonly SearchValues<char> Braces = SearchValues.Create(['{', '}']);

    private object?[] _arguments = formattedCount == 0 ? [] : new object?[formattedCount];
    private char[] _buffer = ArrayPool<char>.Shared.Rent(literalLength + (formattedCount * 4) + 8);
    private int _length;
    private int _argumentCount;

    /// <summary>The reconstructed key, such as "Stored {0} of {1}.".</summary>
    public readonly ReadOnlySpan<char> Key => _buffer.AsSpan(0, _length);

    /// <summary>The values captured from the holes, in index order.</summary>
    public readonly object?[] GetArguments()
        => _arguments;

    /// <summary>Appends literal text between holes.</summary>
    public void AppendLiteral(string value)
    {
        ArgumentNullException.ThrowIfNull(value);

        var span = value.AsSpan();
        if (!span.ContainsAny(Braces))
        {
            Append(span);

            return;
        }

        foreach (var character in span)
        {
            // A brace in prose has to survive the string.Format that later consumes the key.
            if (character is '{' or '}')
            {
                Append(character);
            }

            Append(character);
        }
    }

    /// <summary>Appends a hole and captures its value.</summary>
    public void AppendFormatted<T>(T value)
    {
        Append('{');
        AppendIndex(_argumentCount);
        Append('}');
        _arguments[_argumentCount++] = value;
    }

    /// <summary>Appends a hole, capturing the value already rendered through its format specifier.</summary>
    public void AppendFormatted<T>(T value, string? format)
        => AppendFormatted(value is IFormattable formattable
            ? formattable.ToString(format, CultureInfo.CurrentCulture)
            : value?.ToString());

    /// <summary>Appends a hole, capturing the value already padded to the requested width.</summary>
    public void AppendFormatted<T>(T value, int alignment)
        => AppendFormatted(Align(value?.ToString(), alignment));

    /// <summary>Appends a hole, capturing the value already rendered and padded.</summary>
    public void AppendFormatted<T>(T value, int alignment, string? format)
        => AppendFormatted(Align(
            value is IFormattable formattable ? formattable.ToString(format, CultureInfo.CurrentCulture) : value?.ToString(),
            alignment));

    /// <summary>Returns the rented buffer once the key and arguments have been read.</summary>
    /// <remarks>Detaches the argument array rather than emptying it, so values already read stay valid.</remarks>
    public void Clear()
    {
        if (_buffer.Length > 0)
        {
            ArrayPool<char>.Shared.Return(_buffer);
            _buffer = [];
        }

        _arguments = [];
        _length = 0;
        _argumentCount = 0;
    }

    private static string? Align(string? text, int alignment)
        => alignment == 0 || text is null
            ? text
            : alignment < 0 ? text.PadRight(-alignment) : text.PadLeft(alignment);

    private void Append(char character)
    {
        if (_length == _buffer.Length)
        {
            Grow();
        }

        _buffer[_length++] = character;
    }

    private void Append(ReadOnlySpan<char> value)
    {
        while (_length + value.Length > _buffer.Length)
        {
            Grow();
        }

        value.CopyTo(_buffer.AsSpan(_length));
        _length += value.Length;
    }

    private void AppendIndex(int index)
    {
        if (index < 10)
        {
            Append((char)('0' + index));
            return;
        }

        Span<char> digits = stackalloc char[8];
        _ = index.TryFormat(digits, out var written, provider: CultureInfo.InvariantCulture);

        for (var position = 0; position < written; position++)
        {
            Append(digits[position]);
        }
    }

    private void Grow()
    {
        var bigger = ArrayPool<char>.Shared.Rent(Math.Max(_buffer.Length * 2, 16));
        _buffer.AsSpan(0, _length).CopyTo(bigger);

        if (_buffer.Length > 0)
        {
            ArrayPool<char>.Shared.Return(_buffer);
        }

        _buffer = bigger;
    }
}
