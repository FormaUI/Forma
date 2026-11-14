namespace Forma.Framework.Localization.Files;

using System.Collections.Concurrent;
using System.Globalization;

/// <summary>What the default reading takes from a locale file's name: the culture it is for, and the parts of the name it stands for.</summary>
internal static class LocaleFileNames
{
    private static readonly ConcurrentDictionary<string, bool> Cultures = new(StringComparer.Ordinal);

    /// <summary>Whether a part of a name is a culture, written as .NET names it.</summary>
    /// <param name="part">The part.</param>
    public static bool IsCulture(string part)
        => HasCultureShape(part) && Cultures.GetOrAdd(part, static candidate =>
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

    // A culture's name starts with a language of two or three lowercase letters, so a part such as a type's name is told
    // apart without asking .NET, which answers a name it doesn't know by throwing.
    private static bool HasCultureShape(string part)
    {
        var language = part.IndexOf('-', StringComparison.Ordinal) is var hyphen and >= 0 ? part.AsSpan(0, hyphen) : part;

        return language.Length is 2 or 3 && !language.ContainsAnyExceptInRange('a', 'z');
    }

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
    /// <param name="culture">The culture the file is for.</param>
    /// <param name="parts">The parts of the name the file stands for, empty for the root.</param>
    /// <param name="isInLocalesDir">Whether the file sits in a folder of locale files.</param>
    public static bool TryRead(string name, string localesDir, string extension, StringComparison comparison, out string culture, out string[] parts, out bool isInLocalesDir)
    {
        culture = string.Empty;
        parts = [];
        isInLocalesDir = false;

        if (name.Length <= extension.Length || !name.EndsWith(extension, comparison))
        {
            return false;
        }

        var segments = name[..^extension.Length].Replace('/', '.').Split('.');
        var localesSegments = localesDir.Replace('/', '.').Split('.');
        var kept = new List<string>(segments.Length);

        for (var index = 0; index < segments.Length; index++)
        {
            if (StartsWith(segments, index, localesSegments, comparison))
            {
                index += localesSegments.Length - 1;
                isInLocalesDir = true;
            }
            else if (segments[index].Length == 0)
            {
                // An empty part spells no name, so the file is no locale file of the default reading.
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

    /// <summary>Whether a name's parts begin with another's.</summary>
    /// <param name="parts">The name's parts.</param>
    /// <param name="start">The parts it begins with.</param>
    /// <param name="comparison">How names compare.</param>
    public static bool StartsWith(string[] parts, string[] start, StringComparison comparison)
        => StartsWith(parts, 0, start, comparison);

    private static bool StartsWith(string[] parts, int index, string[] start, StringComparison comparison)
    {
        if (parts.Length - index < start.Length)
        {
            return false;
        }

        for (var offset = 0; offset < start.Length; offset++)
        {
            if (!string.Equals(parts[index + offset], start[offset], comparison))
            {
                return false;
            }
        }

        return true;
    }
}
