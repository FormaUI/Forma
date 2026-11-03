namespace Forma.Framework.Culture;

/// <summary>The place of an accepted culture pair in the sequence of the document that accepted it.</summary>
public sealed record CultureVersion
{
    /// <summary>Creates the version numbered <paramref name="number"/> in the sequence of the document <paramref name="documentKey"/> names.</summary>
    /// <param name="documentKey">The identity of the document whose sequence the version belongs to.</param>
    /// <param name="number">The version's place in its document's sequence.</param>
    public CultureVersion(string documentKey, int number)
    {
        ArgumentNullException.ThrowIfNull(documentKey);
        ArgumentOutOfRangeException.ThrowIfNegative(number);

        DocumentKey = documentKey;
        Number = number;
    }

    /// <summary>The version of the cultures a state starts from, which belongs to no document.</summary>
    public static CultureVersion Initial { get; } = new(string.Empty, 0);

    /// <summary>The identity of the document whose sequence the version belongs to.</summary>
    public string DocumentKey { get; }

    /// <summary>The version's place in its document's sequence.</summary>
    public int Number { get; }

    /// <summary>Whether the version comes after <paramref name="other"/>: a later number in the same document's sequence, or any number in another document's.</summary>
    /// <param name="other">The version compared with.</param>
    public bool Follows(CultureVersion other)
    {
        ArgumentNullException.ThrowIfNull(other);

        return !string.Equals(DocumentKey, other.DocumentKey, StringComparison.Ordinal) || Number > other.Number;
    }
}
