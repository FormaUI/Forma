namespace Forma.Framework.Localization;

/// <summary>Text localized when it is shown: the type whose locale holds it, its key, and the values for its holes.</summary>
public sealed record LocalizableText
{
    private readonly object?[] _arguments;

    private LocalizableText(Type source, string key, object?[] arguments)
    {
        Source = source;
        Key = key;
        _arguments = arguments;
    }

    /// <summary>The type whose locale holds the key.</summary>
    public Type Source { get; }

    /// <summary>The key, which is the English source text.</summary>
    public string Key { get; }

    /// <summary>One value per distinct hole, in the order the holes first appear in the key.</summary>
    public IReadOnlyList<object?> Arguments
        => _arguments;

    /// <summary>The text <paramref name="key"/> names in the locale of <typeparamref name="TSource"/>, its holes filled with <paramref name="arguments"/>.</summary>
    /// <typeparam name="TSource">The type whose locale holds the key.</typeparam>
    /// <param name="key">The key, which is the English source text.</param>
    /// <param name="arguments">One value per distinct hole, in the order the holes first appear in the key.</param>
    public static LocalizableText For<TSource>([LocaleKey] string key, params object?[] arguments)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentNullException.ThrowIfNull(arguments);

        return new(typeof(TSource), key, [.. arguments]);
    }

    /// <inheritdoc />
    public bool Equals(LocalizableText? other)
        => other is not null && Source == other.Source && string.Equals(Key, other.Key, StringComparison.Ordinal) && _arguments.SequenceEqual(other._arguments);

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Source);
        hash.Add(Key, StringComparer.Ordinal);

        foreach (var argument in _arguments)
        {
            hash.Add(argument);
        }

        return hash.ToHashCode();
    }
}
