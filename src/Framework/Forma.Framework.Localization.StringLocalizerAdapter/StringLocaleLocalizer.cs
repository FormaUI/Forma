namespace Forma.Framework.Localization.StringLocalizerAdapter;

using System.Collections.Concurrent;
using System.Collections.Frozen;
using System.Globalization;

using Forma.Framework.Localization.Files;
using Forma.Framework.Localization.Parser;

using Microsoft.Extensions.Localization;

/// <summary>A Forma locale read through Microsoft's string localizer.</summary>
/// <param name="locale">The locale the strings are read from.</param>
/// <remarks>
/// The key a caller passes decides how its text is formatted, never the text a culture gives it. A key naming a Forma hole,
/// <c>{Name}</c>, or holding an expression, <c>[$ … $]</c>, is Forma's, and Forma alone fills it. A key holding a composite
/// placeholder, <c>{0}</c>, or neither is composite: its text is read as its culture writes it, which Forma evaluates none of, and
/// <see cref="string.Format(IFormatProvider, string, object[])"/> alone fills it in the formatting culture, as Microsoft's localizer
/// does. A doubled brace is an escape in both and is resolved once, by the one formatter that runs. A key mixing the two, and a text
/// using the syntax its key doesn't, throw rather than leave a placeholder unfilled or let one formatter read the other's output.
/// </remarks>
internal sealed class StringLocaleLocalizer(IStringLocale locale) : IStringLocalizer
{
    // The texts each UI culture gives the keys as written, read once per culture.
    private readonly ConcurrentDictionary<string, FrozenDictionary<string, LocalizedText>> _writtenByCulture = new(StringComparer.Ordinal);

    /// <inheritdoc />
    /// <remarks>A composite key's text is returned as written, unformatted, as Microsoft's localizer returns it.</remarks>
    /// <exception cref="FormatException">The key mixes composite placeholders with Forma's holes or expressions, or its text uses the syntax its key doesn't.</exception>
    public LocalizedString this[string name]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);

            return IsFormaKey(name) ? Render(name, locale.Resolve(name)) : ReadComposite(name);
        }
    }

    /// <inheritdoc />
    /// <exception cref="FormatException">The key mixes composite placeholders with Forma's holes or expressions, or its text uses the syntax its key doesn't.</exception>
    public LocalizedString this[string name, params object[] arguments]
    {
        get
        {
            ArgumentNullException.ThrowIfNull(name);
            ArgumentNullException.ThrowIfNull(arguments);

            if (IsFormaKey(name))
            {
                return Render(name, locale.Resolve(name, arguments));
            }

            var text = ReadComposite(name);

            return new LocalizedString(name, string.Format(CultureInfo.CurrentCulture, text.Value, arguments), text.ResourceNotFound, text.SearchedLocation);
        }
    }

    /// <inheritdoc />
    public IEnumerable<LocalizedString> GetAllStrings(bool includeParentCultures)
        => locale.GetAllStrings(includeParentCultures).Select(text => new LocalizedString(text.Name, text.Value, text.ResourceNotFound, text.SearchedLocation));

    private static LocalizedString Render(string name, LocalizedPattern pattern)
    {
        RequireSyntax(name, pattern.Format, isForma: true);

        return new LocalizedString(name, LocalizedFormat.Render(pattern.Format, pattern.Arguments, CultureInfo.CurrentCulture, CultureInfo.CurrentUICulture), pattern.ResourceNotFound, pattern.SearchedLocation);
    }

    // A key no culture gives a text is its own text, which names no Forma hole or expression, so the locale resolves it unchanged.
    private LocalizedString ReadComposite(string name)
    {
        var written = _writtenByCulture.GetOrAdd(
            CultureInfo.CurrentUICulture.Name,
            static (_, source) => source.GetAllStrings(includeParentCultures: true).ToFrozenDictionary(text => text.Name, StringComparer.Ordinal),
            locale);

        if (written.TryGetValue(LocalizationSchema.Unescape(name), out var text))
        {
            RequireSyntax(name, text.Value, isForma: false);

            return new LocalizedString(name, text.Value, resourceNotFound: false, text.SearchedLocation);
        }

        var pattern = locale.Resolve(name);

        return new LocalizedString(name, pattern.Format, pattern.ResourceNotFound, pattern.SearchedLocation);
    }

    private static bool IsFormaKey(string name)
    {
        var hasFormaSyntax = HasFormaSyntax(name);

        if (hasFormaSyntax && HasCompositePlaceholder(name))
        {
            throw new FormatException($"'{name}' mixes composite placeholders such as {{0}} with Forma's holes or expressions; a key uses one or the other.");
        }

        return hasFormaSyntax;
    }

    // The text a culture gives a key keeps the key's syntax, since only the key's formatter runs over it.
    private static void RequireSyntax(string name, string text, bool isForma)
    {
        if (isForma ? HasCompositePlaceholder(text) : HasFormaSyntax(text))
        {
            var (used, expected) = isForma ? ("composite placeholders", "Forma's holes or expressions") : ("Forma's holes or expressions", "composite placeholders");

            throw new FormatException($"The '{CultureInfo.CurrentUICulture.Name}' text for '{name}' uses {used}, where its key uses {expected} or neither; a text keeps its key's syntax.");
        }
    }

    private static bool HasFormaSyntax(string text)
    {
        if (text.Contains(LocaleExpressionParser.OpeningDelimiter, StringComparison.Ordinal))
        {
            return true;
        }

        foreach (var segment in LocalizedSegment.Parse(text))
        {
            if (segment.IsHole)
            {
                return true;
            }
        }

        return false;
    }

    // A composite placeholder is a brace, an index and a closing brace, an alignment or a format; a doubled brace is an escape.
    private static bool HasCompositePlaceholder(string text)
    {
        for (var index = 0; index < text.Length; index++)
        {
            if (text[index] != '{')
            {
                continue;
            }

            if (index + 1 < text.Length && text[index + 1] == '{')
            {
                index++;

                continue;
            }

            var end = index + 1;
            while (end < text.Length && char.IsAsciiDigit(text[end]))
            {
                end++;
            }

            if (end > index + 1 && end < text.Length && text[end] is '}' or ',' or ':')
            {
                return true;
            }
        }

        return false;
    }
}
