namespace Forma.Framework.Input.Keyboard;

using System.Globalization;

/// <summary>The search a list makes as a reader types its labels: characters typed within a pause of each other build a prefix, and a character typed again on its own moves on through the labels it starts.</summary>
/// <param name="clock">The clock the pause between characters is measured on.</param>
public sealed class TypeAhead(TimeProvider clock)
{
    private string _prefix = string.Empty;
    private long _typedAt;

    /// <summary>How long after a character the next one still extends the prefix.</summary>
    public static TimeSpan Pause { get; } = TimeSpan.FromMilliseconds(500);

    // Whether a character typed now extends the prefix.
    private bool IsTyping
        => _prefix.Length > 0 && clock.GetElapsedTime(_typedAt) <= Pause;

    /// <summary>Finds the label <paramref name="character"/> leads to, searching on from <paramref name="current"/> and wrapping round, among the labels <paramref name="canTake"/> admits.</summary>
    /// <param name="character">The character typed.</param>
    /// <param name="labels">The labels the list shows, in order.</param>
    /// <param name="current">The index of the label the reader is on, or -1 where they are on none.</param>
    /// <param name="culture">The culture the labels are compared in.</param>
    /// <param name="canTake">Whether the label at an index can be moved to, where null admits every label.</param>
    /// <returns>The index of the label found, or null where no label starts with what was typed.</returns>
    /// <remarks>A prefix that grows may still name the label the reader is on, so its search starts there; a single character, or one repeated, starts after it.</remarks>
    public int? Find(string character, IReadOnlyList<string> labels, int current, CultureInfo culture, Func<int, bool>? canTake = null)
    {
        ArgumentException.ThrowIfNullOrEmpty(character);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(culture);

        _prefix = IsTyping ? _prefix + character : character;
        _typedAt = clock.GetTimestamp();

        if (labels.Count == 0)
        {
            return null;
        }

        var isRepeated = _prefix.Length > character.Length && _prefix.Replace(character, string.Empty, StringComparison.Ordinal).Length == 0;
        var search = isRepeated ? character : _prefix;
        var start = search.Length == character.Length ? current + 1 : Math.Max(current, 0);

        for (var step = 0; step < labels.Count; step++)
        {
            var index = (start + step) % labels.Count;

            if ((canTake?.Invoke(index) ?? true) && culture.CompareInfo.IsPrefix(labels[index], search, CompareOptions.IgnoreCase))
            {
                return index;
            }
        }

        return null;
    }
}
