namespace Forma.Framework.Localization.Files;

using System.Buffers;
using System.Diagnostics.CodeAnalysis;
using System.Text.Json;

/// <summary>What a <c>.locali.json</c> file says about the locale files beneath it; a key the file leaves out is null.</summary>
/// <param name="RootNamespace">The root namespace of everything beneath the file.</param>
/// <param name="RootDir">Where the source's names begin, as a path relative to the file's own folder; one ending in a project or solution file means that file's folder.</param>
/// <param name="LocalesDir">The name a folder of locale files has, written with <c>/</c> between folders.</param>
/// <param name="Paths">The paths a source reads by, replacing the default ones.</param>
public sealed record LocaliSettings(string? RootNamespace, string? RootDir, string? LocalesDir, IReadOnlyList<LocalePath>? Paths)
{
    /// <summary>The name a settings file has.</summary>
    public const string FileName = ".locali.json";

    /// <summary>The key naming the root namespace.</summary>
    public const string RootNamespaceKey = "rootNamespace";

    /// <summary>The key naming where the source's names begin.</summary>
    public const string RootDirKey = "rootDir";

    /// <summary>The key naming the folder of locale files.</summary>
    public const string LocalesDirKey = "localesDir";

    /// <summary>The key listing the paths.</summary>
    public const string PathsKey = "paths";

    /// <summary>The name a folder of locale files has unless a setting names another.</summary>
    public const string DefaultLocalesDir = ".locales";

    private static readonly SearchValues<char> NotInARelativePath = SearchValues.Create("\\:");

    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
        AllowDuplicateProperties = false,
    };

    /// <summary>The keys the runtime reads, in the order a file is expected to write them.</summary>
    public static IReadOnlyList<string> Keys { get; } = [RootNamespaceKey, RootDirKey, LocalesDirKey, PathsKey];

    /// <summary>Settings that say nothing, leaving every value to a farther file or the default.</summary>
    public static LocaliSettings Empty { get; } = new(null, null, null, null);

    /// <summary>Reads the keys the runtime owns from a settings file, passing over every other key.</summary>
    /// <param name="stream">The file's content.</param>
    /// <param name="fileName">The file being read, named in anything thrown.</param>
    /// <exception cref="LocaliSettingsException">The file is not a JSON object, or a key the runtime owns is malformed.</exception>
    public static LocaliSettings Read(Stream stream, string fileName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrEmpty(fileName);

        try
        {
            using var document = JsonDocument.Parse(stream, DocumentOptions);
            if (document.RootElement.ValueKind != JsonValueKind.Object)
            {
                throw new LocaliSettingsException($"'{fileName}' is not a JSON object.");
            }

            string? rootNamespace = null;
            string? rootDir = null;
            string? localesDir = null;
            IReadOnlyList<LocalePath>? paths = null;

            foreach (var property in document.RootElement.EnumerateObject())
            {
                switch (property.Name)
                {
                    case RootNamespaceKey:
                        rootNamespace = ReadSetting(property.Value, RootNamespaceKey, fileName);
                        break;

                    case RootDirKey:
                        rootDir = ReadSetting(property.Value, RootDirKey, fileName);
                        break;

                    case LocalesDirKey:
                        localesDir = ReadSetting(property.Value, LocalesDirKey, fileName);
                        break;

                    case PathsKey:
                        paths = ReadPaths(property.Value, fileName);
                        break;
                }
            }

            return new LocaliSettings(rootNamespace, rootDir, localesDir, paths);
        }
        catch (JsonException exception)
        {
            throw new LocaliSettingsException($"'{fileName}' is not readable JSON: {exception.Message}", exception);
        }
    }

    /// <summary>Whether a key whose value is text accepts one, and what is wrong with it when it does not.</summary>
    /// <param name="key">The key, as a file writes it; one the runtime does not own, and <see cref="PathsKey"/>, whose value is a list of paths each read by <see cref="LocalePath.Parse"/>, accept any text.</param>
    /// <param name="value">The value, as a file writes it.</param>
    /// <param name="problem">What is wrong with the value, in a sentence, when the key does not accept it.</param>
    public static bool Accepts(string key, string value, [NotNullWhen(false)] out string? problem)
    {
        ArgumentException.ThrowIfNullOrEmpty(key);
        ArgumentNullException.ThrowIfNull(value);

        problem = (key, value) switch
        {
            (_, "") => $"'{key}' names nothing.",
            (RootNamespaceKey, _) when !IsNamespace(value) => $"'{key}' names '{value}', which is not a namespace.",
            (RootDirKey, _) when !IsRelativePath(value) => $"'{key}' names '{value}', which is not a relative path written with '/'.",
            (LocalesDirKey, _) when !IsRelativePath(value) || IsDotted(value) => $"'{key}' names '{value}', which is not a relative folder written with '/'.",
            _ => null,
        };

        return problem is null;
    }

    private static string ReadSetting(JsonElement value, string key, string fileName)
    {
        var text = ReadText(value, key, fileName);
        if (!Accepts(key, text, out var problem))
        {
            throw new LocaliSettingsException($"In '{fileName}', {problem}");
        }

        return text;
    }

    private static List<LocalePath> ReadPaths(JsonElement value, string fileName)
    {
        if (value.ValueKind != JsonValueKind.Array || value.GetArrayLength() == 0)
        {
            throw new LocaliSettingsException($"'{fileName}' gives '{PathsKey}' as something other than a list of paths.");
        }

        var paths = new List<LocalePath>();
        foreach (var item in value.EnumerateArray())
        {
            var text = ReadText(item, PathsKey, fileName);
            try
            {
                paths.Add(LocalePath.Parse(text));
            }
            catch (FormatException exception)
            {
                throw new LocaliSettingsException($"'{fileName}' lists a malformed path under '{PathsKey}': {exception.Message}", exception);
            }
        }

        return paths;
    }

    // Dots divide the segments, and a segment is one or more characters with no whitespace among them.
    private static bool IsNamespace(ReadOnlySpan<char> text)
    {
        foreach (var range in text.Split('.'))
        {
            var segment = text[range];
            if (segment.IsEmpty || HasWhitespace(segment))
            {
                return false;
            }
        }

        return true;
    }

    private static bool HasWhitespace(ReadOnlySpan<char> segment)
    {
        foreach (var character in segment)
        {
            if (char.IsWhiteSpace(character))
            {
                return true;
            }
        }

        return false;
    }

    // A path is relative, written with '/' alone, and never doubles a separator.
    private static bool IsRelativePath(ReadOnlySpan<char> text)
    {
        if (text[0] == Path.AltDirectorySeparatorChar || text.ContainsAny(NotInARelativePath))
        {
            return false;
        }

        foreach (var range in text.Split(Path.AltDirectorySeparatorChar))
        {
            if (text[range].IsEmpty)
            {
                return false;
            }
        }

        return true;
    }

    private static bool IsDotted(ReadOnlySpan<char> text)
    {
        foreach (var range in text.Split(Path.AltDirectorySeparatorChar))
        {
            if (text[range] is "." or "..")
            {
                return true;
            }
        }

        return false;
    }

    private static string ReadText(JsonElement value, string key, string fileName)
        => value.ValueKind == JsonValueKind.String && value.GetString() is { Length: > 0 } text
            ? text
            : throw new LocaliSettingsException($"'{fileName}' gives '{key}' as something other than text.");
}
