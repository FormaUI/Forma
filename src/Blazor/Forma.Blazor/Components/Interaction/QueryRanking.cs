namespace Forma.Blazor.Components.Interaction;

using System.Globalization;

/// <summary>The order a list of named items stands in for what the reader typed.</summary>
/// <remarks>
/// Every word typed must start or appear in an item's name or its place, compared in the culture ignoring case and accents. An
/// item whose name starts with the whole query comes first; the rest follow by how well each word matched, a word starting a word
/// of the name before one appearing inside it, and the name before the place; ties keep the items' own order.
/// </remarks>
internal static class QueryRanking
{
    private const CompareOptions Loose = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType;

    // How well one typed word matched, the lower the better; a word that matched nowhere leaves the item out.
    private const int NameStart = 0;
    private const int NameWordStart = 1;
    private const int InName = 2;
    private const int PlaceWordStart = 3;
    private const int InPlace = 4;
    private const int Unmatched = -1;

    /// <summary>The items that match <paramref name="query"/>, in the order a reader looks for them; every item, in order, where nothing is typed.</summary>
    /// <param name="items">The items, in their own order.</param>
    /// <param name="getName">Reads an item's name.</param>
    /// <param name="getPlace">Reads where an item lives, which a word may match after the name.</param>
    /// <param name="query">What the reader typed.</param>
    /// <param name="culture">The culture the words are compared in.</param>
    public static IReadOnlyList<T> Rank<T>(IReadOnlyList<T> items, Func<T, string> getName, Func<T, string?>? getPlace, string? query, CultureInfo culture)
    {
        ArgumentNullException.ThrowIfNull(items);
        ArgumentNullException.ThrowIfNull(getName);
        ArgumentNullException.ThrowIfNull(culture);

        var words = SplitWords(query);

        if (words.Length == 0)
        {
            return items;
        }

        var compareInfo = culture.CompareInfo;
        var whole = string.Join(' ', words);
        var ranked = new List<(T Item, int IsNamePrefix, int Score, int Index)>();

        for (var index = 0; index < items.Count; index++)
        {
            var item = items[index];
            var name = getName(item);
            var place = getPlace?.Invoke(item);
            var score = 0;
            var matches = true;

            foreach (var word in words)
            {
                var match = Match(compareInfo, name, place, word);

                if (match == Unmatched)
                {
                    matches = false;
                    break;
                }

                score += match;
            }

            if (matches)
            {
                ranked.Add((item, compareInfo.IsPrefix(name, whole, Loose) ? 0 : 1, score, index));
            }
        }

        return [.. ranked.OrderBy(entry => entry.IsNamePrefix).ThenBy(entry => entry.Score).ThenBy(entry => entry.Index).Select(entry => entry.Item)];
    }

    private static int Match(CompareInfo compareInfo, string name, string? place, string word)
    {
        if (compareInfo.IsPrefix(name, word, Loose))
        {
            return NameStart;
        }

        if (StartsAWord(compareInfo, name, word))
        {
            return NameWordStart;
        }

        if (compareInfo.IndexOf(name, word, Loose) >= 0)
        {
            return InName;
        }

        if (place is not null)
        {
            if (compareInfo.IsPrefix(place, word, Loose) || StartsAWord(compareInfo, place, word))
            {
                return PlaceWordStart;
            }

            if (compareInfo.IndexOf(place, word, Loose) >= 0)
            {
                return InPlace;
            }
        }

        return Unmatched;
    }

    private static bool StartsAWord(CompareInfo compareInfo, string text, string word)
        => SplitWords(text).Any(part => compareInfo.IsPrefix(part, word, Loose));

    // Words are what the spaces, the punctuation and the symbols between them leave.
    private static string[] SplitWords(string? text)
    {
        if (string.IsNullOrWhiteSpace(text))
        {
            return [];
        }

        var words = new List<string>();
        var start = -1;

        for (var index = 0; index <= text.Length; index++)
        {
            var isSeparator = index == text.Length || char.IsWhiteSpace(text[index]) || char.IsPunctuation(text[index]) || char.IsSymbol(text[index]);

            if (isSeparator && start >= 0)
            {
                words.Add(text[start..index]);
                start = -1;
            }
            else if (!isSeparator && start < 0)
            {
                start = index;
            }
        }

        return [.. words];
    }
}
