namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Diagnostics.CodeAnalysis;
using System.Globalization;
using System.Runtime.InteropServices;

/// <summary>The default reading of a locale file's name: the culture the file is for, and the parts of the name it stands for.</summary>
public static class LocaleFileNames
{
    // A name of more parts than this is read through the slower path that splits it whole.
    private const int MostParts = 64;

    private static readonly ConcurrentDictionary<string, bool> Cultures = new(StringComparer.Ordinal);

    /// <summary>Whether a part of a name is a culture, written as .NET names it.</summary>
    /// <param name="part">The part.</param>
    public static bool IsCulture(ReadOnlySpan<char> part)
    {
        if (!HasCultureShape(part))
        {
            return false;
        }

        if (Cultures.GetAlternateLookup<ReadOnlySpan<char>>().TryGetValue(part, out var isCulture))
        {
            return isCulture;
        }

        return Cultures.GetOrAdd(part.ToString(), static candidate =>
        {
            try
            {
                return string.Equals(CultureInfo.GetCultureInfo(candidate, predefinedOnly: true).Name, candidate, StringComparison.Ordinal);
            }
            catch (CultureNotFoundException)
            {
                return false;
            }
        });
    }

    /// <summary>Whether a name holds a part written exactly so, told without reading the name whole.</summary>
    /// <param name="name">The name.</param>
    /// <param name="part">The part.</param>
    internal static bool HasPart(string name, string part)
    {
        var text = name.AsSpan();
        Span<Range> ranges = stackalloc Range[MostParts];
        var count = text.SplitAny(ranges, "./");

        for (var index = 0; index < count; index++)
        {
            if (text[ranges[index]].SequenceEqual(part))
            {
                return true;
            }
        }

        // A name of very many parts is left for the reading of it whole to judge.
        return count == MostParts;
    }

    /// <summary>The parts of a name written with folders, dots or both.</summary>
    /// <param name="name">The name.</param>
    internal static string[] Split(string name)
        => name.Split(['.', '/']);

    /// <summary>Reads a file's name, from where its source's names begin, into the culture it is for and the parts of the name it stands for.</summary>
    /// <remarks>
    /// A folder and a dot separate the name alike. A folder of locale files is left out wherever it sits, the first part
    /// that is a culture is the file's culture, and every other part is part of the name, so <c>.locales/es/Accounts.Admin.json</c>,
    /// <c>Accounts/Admin/.locales/es.json</c> and <c>Accounts.Admin.es.json</c> all stand for <c>Accounts.Admin</c>.
    /// </remarks>
    /// <param name="name">The file's name from where its source's names begin.</param>
    /// <param name="localesDir">The name of a folder of locale files, written with <c>/</c>.</param>
    /// <param name="extension">The extension the backend's files carry.</param>
    /// <param name="comparison">How names compare.</param>
    /// <param name="fileName">What the reading took from the name.</param>
    public static bool TryRead(string name, string localesDir, string extension, StringComparison comparison, [NotNullWhen(true)] out LocaleFileName? fileName)
    {
        ArgumentNullException.ThrowIfNull(name);
        ArgumentException.ThrowIfNullOrEmpty(localesDir);
        ArgumentException.ThrowIfNullOrEmpty(extension);

        fileName = TryRead(name, localesDir, extension, comparison, out var culture, out var parts, out var isInLocalesDir)
            ? new LocaleFileName(culture, ImmutableCollectionsMarshal.AsImmutableArray(parts), isInLocalesDir)
            : null;

        return fileName is not null;
    }

    /// <summary>Whether a name's parts begin with another's.</summary>
    /// <param name="parts">The name's parts.</param>
    /// <param name="start">The parts it begins with.</param>
    /// <param name="comparison">How names compare.</param>
    internal static bool StartsWith(ReadOnlySpan<string> parts, ReadOnlySpan<string> start, StringComparison comparison)
    {
        if (parts.Length < start.Length)
        {
            return false;
        }

        for (var offset = 0; offset < start.Length; offset++)
        {
            if (!string.Equals(parts[offset], start[offset], comparison))
            {
                return false;
            }
        }

        return true;
    }

    private static bool TryRead(string name, string localesDir, string extension, StringComparison comparison, out string culture, out string[] parts, out bool isInLocalesDir)
    {
        culture = string.Empty;
        parts = [];
        isInLocalesDir = false;

        if (name.Length <= extension.Length || !name.EndsWith(extension, comparison))
        {
            return false;
        }

        var text = name.AsSpan(0, name.Length - extension.Length);
        Span<Range> ranges = stackalloc Range[MostParts];
        var count = text.SplitAny(ranges, "./");

        if (count == MostParts)
        {
            return TryReadSplit(text.ToString(), localesDir, comparison, out culture, out parts, out isInLocalesDir);
        }

        Span<Range> locales = stackalloc Range[MostParts];
        var localesCount = localesDir.AsSpan().SplitAny(locales, "./");
        Span<bool> kept = stackalloc bool[count];
        var keptCount = 0;

        for (var index = 0; index < count; index++)
        {
            if (StartsWith(text, ranges[index..count], localesDir, locales[..localesCount], comparison))
            {
                index += localesCount - 1;
                isInLocalesDir = true;
            }
            else if (text[ranges[index]].IsEmpty)
            {
                // An empty part spells no name, so the file is no locale file of the default reading.
                return false;
            }
            else if (culture.Length == 0 && IsCulture(text[ranges[index]]))
            {
                culture = text[ranges[index]].ToString();
            }
            else
            {
                kept[index] = true;
                keptCount++;
            }
        }

        if (culture.Length == 0)
        {
            return false;
        }

        parts = new string[keptCount];
        for (int index = 0, written = 0; index < count; index++)
        {
            if (kept[index])
            {
                parts[written++] = text[ranges[index]].ToString();
            }
        }

        return true;
    }

    // A culture's name starts with a language of two or three lowercase letters, so a part such as a type's name is told
    // apart without asking .NET, which answers a name it doesn't know by throwing.
    private static bool HasCultureShape(ReadOnlySpan<char> part)
    {
        var hyphen = part.IndexOf('-');
        var language = hyphen >= 0 ? part[..hyphen] : part;

        return language.Length is 2 or 3 && !language.ContainsAnyExceptInRange('a', 'z');
    }

    private static bool StartsWith(ReadOnlySpan<char> text, ReadOnlySpan<Range> ranges, ReadOnlySpan<char> localesDir, ReadOnlySpan<Range> locales, StringComparison comparison)
    {
        if (ranges.Length < locales.Length)
        {
            return false;
        }

        for (var offset = 0; offset < locales.Length; offset++)
        {
            if (!text[ranges[offset]].Equals(localesDir[locales[offset]], comparison))
            {
                return false;
            }
        }

        return true;
    }

    // A name of very many parts is read by splitting it whole.
    private static bool TryReadSplit(string text, string localesDir, StringComparison comparison, out string culture, out string[] parts, out bool isInLocalesDir)
    {
        culture = string.Empty;
        parts = [];
        isInLocalesDir = false;

        var segments = Split(text);
        var localesSegments = Split(localesDir);
        var kept = new List<string>(segments.Length);

        for (var index = 0; index < segments.Length; index++)
        {
            if (StartsWith(segments.AsSpan(index), localesSegments, comparison))
            {
                index += localesSegments.Length - 1;
                isInLocalesDir = true;
            }
            else if (segments[index].Length == 0)
            {
                return false;
            }
            else if (culture.Length == 0 && IsCulture(segments[index]))
            {
                culture = segments[index];
            }
            else
            {
                kept.Add(segments[index]);
            }
        }

        parts = [.. kept];

        return culture.Length > 0;
    }
}
