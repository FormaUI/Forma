namespace Forma.Framework.Localization.Files;

using System.Collections.Immutable;

/// <summary>What the default reading takes from a locale file's name: the culture the file is for, and the parts of the name it stands for.</summary>
/// <param name="Culture">The culture the file is for, as .NET names it.</param>
/// <param name="Parts">The parts of the name the file stands for, in order, empty for the root.</param>
/// <param name="IsInLocalesDir">Whether the file sits in a folder of locale files.</param>
public sealed record LocaleFileName(string Culture, ImmutableArray<string> Parts, bool IsInLocalesDir)
{
    /// <inheritdoc />
    /// <remarks>Two readings are equal where their parts are, part by part.</remarks>
    public bool Equals(LocaleFileName? other)
        => other is not null
            && string.Equals(Culture, other.Culture, StringComparison.Ordinal)
            && IsInLocalesDir == other.IsInLocalesDir
            && Parts.AsSpan().SequenceEqual(other.Parts.AsSpan());

    /// <inheritdoc />
    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(Culture, StringComparer.Ordinal);
        hash.Add(IsInLocalesDir);

        foreach (var part in Parts)
        {
            hash.Add(part, StringComparer.Ordinal);
        }

        return hash.ToHashCode();
    }
}
