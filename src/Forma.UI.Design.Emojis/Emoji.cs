namespace Forma.UI.Design.Emojis;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>One emoji, named by the Unicode sequence that is it.</summary>
public readonly record struct Emoji
{
    /// <summary>Names an emoji by its sequence.</summary>
    /// <param name="sequence">The scalar values, one or more, that together form the emoji.</param>
    /// <exception cref="ArgumentException">The sequence is empty or is more than one character.</exception>
    public Emoji(string sequence)
    {
        ArgumentException.ThrowIfNullOrEmpty(sequence);

        if (!IsOneCharacter(sequence))
        {
            throw new ArgumentException("An emoji is a single character, as one or more scalar values.", nameof(sequence));
        }

        Sequence = sequence;
    }

    /// <summary>The scalar values, one or more, that together form the emoji.</summary>
    public string Sequence { get; }

    /// <summary>Attempts to name an emoji by a sequence that may not be one.</summary>
    /// <param name="sequence">The scalar values that may form an emoji.</param>
    /// <param name="emoji">The emoji named, when the sequence is a single character.</param>
    /// <returns>Whether the sequence names an emoji.</returns>
    public static bool TryCreate([NotNullWhen(true)] string? sequence, out Emoji emoji)
    {
        if (!string.IsNullOrEmpty(sequence) && IsOneCharacter(sequence))
        {
            emoji = new(sequence);
            return true;
        }

        emoji = default;
        return false;
    }

    /// <summary>Takes a sequence as the emoji it names.</summary>
    /// <param name="sequence">The scalar values, one or more, that together form the emoji.</param>
    public static implicit operator Emoji(string sequence)
    {
        return new(sequence);
    }

    private static bool IsOneCharacter(string sequence)
        => new StringInfo(sequence).LengthInTextElements == 1;
}
