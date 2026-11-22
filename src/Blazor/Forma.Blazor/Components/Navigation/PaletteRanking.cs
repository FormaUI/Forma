namespace Forma.Blazor.Components.Navigation;

using System.Globalization;

/// <summary>The order a palette lists its commands in for what the reader typed.</summary>
/// <remarks>
/// Every word typed must start or appear in a command's name or its place, compared in the culture ignoring case and accents. A
/// command whose name starts with the whole query comes first; the rest follow by how well each word matched, a word starting a
/// word of the name before one appearing inside it, and the name before the place; ties keep the host's order.
/// </remarks>
internal static class PaletteRanking
{
    private const CompareOptions Loose = CompareOptions.IgnoreCase | CompareOptions.IgnoreNonSpace | CompareOptions.IgnoreWidth | CompareOptions.IgnoreKanaType;

    // How well one typed word matched, the lower the better; a word that matched nowhere leaves the command out.
    private const int NameStart = 0;
    private const int NameWordStart = 1;
    private const int InName = 2;
    private const int PlaceWordStart = 3;
    private const int InPlace = 4;
    private const int Unmatched = -1;

    /// <summary>The commands that match <paramref name="query"/>, in the order a reader looks for them; every command, in order, where nothing is typed.</summary>
    public static IReadOnlyList<PaletteCommand> Rank(IReadOnlyList<PaletteCommand> commands, string? query, CultureInfo culture)
    {
        var words = SplitWords(query);

        if (words.Length == 0)
        {
            return commands;
        }

        var compareInfo = culture.CompareInfo;
        var whole = string.Join(' ', words);
        var ranked = new List<(PaletteCommand Command, int IsNamePrefix, int Score, int Index)>();

        for (var index = 0; index < commands.Count; index++)
        {
            var command = commands[index];
            var score = 0;
            var matches = true;

            foreach (var word in words)
            {
                var match = Match(compareInfo, command, word);

                if (match == Unmatched)
                {
                    matches = false;
                    break;
                }

                score += match;
            }

            if (matches)
            {
                ranked.Add((command, compareInfo.IsPrefix(command.Name, whole, Loose) ? 0 : 1, score, index));
            }
        }

        return [.. ranked.OrderBy(entry => entry.IsNamePrefix).ThenBy(entry => entry.Score).ThenBy(entry => entry.Index).Select(entry => entry.Command)];
    }

    private static int Match(CompareInfo compareInfo, PaletteCommand command, string word)
    {
        if (compareInfo.IsPrefix(command.Name, word, Loose))
        {
            return NameStart;
        }

        if (StartsAWord(compareInfo, command.Name, word))
        {
            return NameWordStart;
        }

        if (compareInfo.IndexOf(command.Name, word, Loose) >= 0)
        {
            return InName;
        }

        if (command.Place is { } place)
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
