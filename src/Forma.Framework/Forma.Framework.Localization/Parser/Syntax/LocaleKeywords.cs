namespace Forma.Framework.Localization.Parser.Syntax;

/// <summary>The keywords a parse recognizes.</summary>
public sealed class LocaleKeywords
{
    private readonly LocaleKeyword[] _keywords;

    /// <summary>Creates a set holding the given keywords.</summary>
    /// <param name="keywords">The keywords to recognize.</param>
    public LocaleKeywords(params LocaleKeyword[] keywords)
    {
        ArgumentNullException.ThrowIfNull(keywords);

        foreach (var keyword in keywords)
        {
            ArgumentNullException.ThrowIfNull(keyword, nameof(keywords));
        }

        if (keywords.Select(keyword => keyword.Name).Distinct(StringComparer.Ordinal).Count() != keywords.Length)
        {
            throw new ArgumentException("A keyword name is repeated.", nameof(keywords));
        }

        _keywords = keywords;
    }

    /// <summary>The empty set of keywords.</summary>
    public static LocaleKeywords None { get; } = new();

    /// <summary>The keyword written as the given name.</summary>
    /// <param name="name">The name to look for.</param>
    public LocaleKeyword? Find(ReadOnlySpan<char> name)
    {
        foreach (var keyword in _keywords)
        {
            if (name.SequenceEqual(keyword.Name))
            {
                return keyword;
            }
        }

        return null;
    }
}
