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

    private LocalizedArgument[] _arguments = formattedCount == 0 ? [] : new LocalizedArgument[formattedCount];
    private char[] _buffer = ArrayPool<char>.Shared.Rent(literalLength + (formattedCount * 4) + 8);
    private int _length;
    private int _argumentCount;

    /// <summary>The reconstructed key, such as "Stored {stored} of {total}.".</summary>
    public readonly ReadOnlySpan<char> Key => _buffer.AsSpan(0, _length);

    /// <summary>The values captured from the holes, each under the name its hole used.</summary>
    public readonly LocalizedArgument[] GetArguments()
        => _arguments;

    /// <summary>Appends literal text between holes.</summary>
    /// <param name="value">The literal run to append.</param>
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
            // A brace in prose has to survive the walk that later reads the key's holes.
            if (character is '{' or '}')
            {
                Append(character);
            }

            Append(character);
        }
    }

    /// <summary>Appends a hole and captures its value under the name the hole was written with.</summary>
    /// <param name="value">The value the hole renders.</param>
    /// <param name="name">The source text of the hole, which has to be a plain identifier.</param>
    public void AppendFormatted<T>(T value, [CallerArgumentExpression(nameof(value))] string? name = null)
        => AppendHole(value, Require(name), 0, null);

    /// <summary>Appends a hole carrying a format specifier, capturing the value unrendered.</summary>
    /// <param name="value">The value the hole renders.</param>
    /// <param name="format">The specifier the value is rendered through.</param>
    /// <param name="name">The source text of the hole, which has to be a plain identifier.</param>
    public void AppendFormatted<T>(T value, string? format, [CallerArgumentExpression(nameof(value))] string? name = null)
        => AppendHole(value, Require(name), 0, format);

    /// <summary>Appends a hole carrying an alignment, capturing the value unpadded.</summary>
    /// <param name="value">The value the hole renders.</param>
    /// <param name="alignment">The width the value is padded to.</param>
    /// <param name="name">The source text of the hole, which has to be a plain identifier.</param>
    public void AppendFormatted<T>(T value, int alignment, [CallerArgumentExpression(nameof(value))] string? name = null)
        => AppendHole(value, Require(name), alignment, null);

    /// <summary>Appends a hole carrying an alignment and a format specifier, capturing the value as it is.</summary>
    /// <param name="value">The value the hole renders.</param>
    /// <param name="alignment">The width the value is padded to.</param>
    /// <param name="format">The specifier the value is rendered through.</param>
    /// <param name="name">The source text of the hole, which has to be a plain identifier.</param>
    public void AppendFormatted<T>(T value, int alignment, string? format, [CallerArgumentExpression(nameof(value))] string? name = null)
        => AppendHole(value, Require(name), alignment, format);

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

    private static string Require(string? name)
        => LocaleName.IsValid(name)
            ? name!
            : throw new ArgumentException(
                $"A hole has to be a plain identifier so a translation can name it; '{name}' is not. Assign it to a local first.",
                nameof(name));

    private void AppendHole<T>(T value, string name, int alignment, string? format)
    {
        Append('{');
        Append(name.AsSpan());

        if (alignment != 0)
        {
            Append(',');
            AppendNumber(alignment);
        }

        if (!string.IsNullOrEmpty(format))
        {
            Append(':');
            Append(format.AsSpan());
        }

        Append('}');
        _arguments[_argumentCount++] = new LocalizedArgument(name, value);
    }

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

    private void AppendNumber(int value)
    {
        Span<char> digits = stackalloc char[12];
        _ = value.TryFormat(digits, out var written, provider: CultureInfo.InvariantCulture);

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
