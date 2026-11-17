namespace Forma.Framework.Input.Keyboard;

using System.Globalization;

/// <summary>The search a list makes as a reader types its labels: characters typed within a pause of each other build a prefix, and a character typed again on its own moves on through the labels it starts.</summary>
public sealed class TypeAhead
{
    private string _prefix = string.Empty;
    private double _typedAt;

    /// <summary>How long after a character the next one still extends the prefix.</summary>
    public static TimeSpan Pause { get; } = TimeSpan.FromMilliseconds(500);

    /// <summary>Finds the label <paramref name="key"/> leads to, searching on from <paramref name="current"/> and wrapping round, among the labels <paramref name="canTake"/> admits.</summary>
    /// <param name="key">The key typed, whose <see cref="InputKeyInfo.Key"/> is the character.</param>
    /// <param name="labels">The labels the list shows, in order.</param>
    /// <param name="current">The index of the label the reader is on, or -1 where they are on none.</param>
    /// <param name="culture">The culture the labels are compared in.</param>
    /// <param name="canTake">Whether the label at an index can be moved to, where null admits every label.</param>
    /// <returns>The index of the label found, or null where no label starts with what was typed.</returns>
    /// <remarks>
    /// The pause is measured between the times the keys were pressed, so a key that reaches the list late extends the prefix
    /// it was typed into. A prefix that grows may still name the label the reader is on, so its search starts there; a single
    /// character, or one repeated, starts after it.
    /// </remarks>
    public int? Find(InputKeyInfo key, IReadOnlyList<string> labels, int current, CultureInfo culture, Func<int, bool>? canTake = null)
    {
        ArgumentNullException.ThrowIfNull(key);
        ArgumentException.ThrowIfNullOrEmpty(key.Key);
        ArgumentNullException.ThrowIfNull(labels);
        ArgumentNullException.ThrowIfNull(culture);

        var character = key.Key;
        _prefix = _prefix.Length > 0 && key.Timestamp - _typedAt <= Pause.TotalMilliseconds ? _prefix + character : character;
        _typedAt = key.Timestamp;

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
