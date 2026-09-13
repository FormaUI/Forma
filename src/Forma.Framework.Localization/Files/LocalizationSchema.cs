namespace Forma.Framework.Localization.Files;

/// <summary>The shape of the entry every localization file carries to declare what it is.</summary>
public static class LocalizationSchema
{
    /// <summary>The field naming who wrote the translations.</summary>
    public const string AuthorField = "Author";

    /// <summary>The field naming the culture the file carries translations for.</summary>
    public const string CultureField = "Culture";

    /// <summary>The schema this build defines, which every file it owns declares.</summary>
    public const string CurrentVersion = "1.0.0";

    /// <summary>The key the entry sits under, which also marks the file as a localization file.</summary>
    public const string Marker = "$localization";

    /// <summary>The key of the section whose entries every type reached through its node shares, unless a section of its own says otherwise.</summary>
    public const string SharedKey = "$shared";

    /// <summary>The key of the entry reserved for tools, each keeping its own entries under keys naming it, which the runtime reads nothing from.</summary>
    public const string ToolsKey = "$tools";

    /// <summary>The prefix that keeps a key shaped like a reserved entry as text; one dollar sign is dropped and the rest is read as written.</summary>
    public const string EscapePrefix = "$$";

    /// <summary>The field naming the namespace a host file completes a type's name from, in place of the assembly's name.</summary>
    public const string RootNamespaceField = "RootNamespace";

    /// <summary>The field naming the revision the translations are at.</summary>
    public const string RevisionField = "Revision";

    /// <summary>The field naming the schema the file is written for.</summary>
    public const string SchemaVersionField = "SchemaVersion";

    /// <summary>The fields an entry may declare after the required ones.</summary>
    public static IReadOnlyList<string> OptionalFields { get; } = [AuthorField, RevisionField, RootNamespaceField];

    /// <summary>The fields every entry declares, in the order it declares them.</summary>
    public static IReadOnlyList<string> RequiredFields { get; } = [SchemaVersionField, CultureField];

    /// <summary>The schema major version this build reads, which is the only part that decides readability.</summary>
    public static int SupportedMajor { get; } = Version.Parse(CurrentVersion).Major;

    /// <summary>Whether a key is shaped like a reserved entry: a dollar sign, then one or more lowercase ASCII letters, and nothing else.</summary>
    /// <param name="key">The key as written.</param>
    public static bool IsReservedShape(ReadOnlySpan<char> key)
    {
        if (key.Length < 2 || key[0] != '$')
        {
            return false;
        }

        foreach (var character in key[1..])
        {
            if (!char.IsAsciiLetterLower(character))
            {
                return false;
            }
        }

        return true;
    }

    /// <summary>The text a key stands for: the key with one dollar sign dropped when it starts with the escape prefix, and the key itself otherwise.</summary>
    /// <param name="key">The key as written.</param>
    public static string Unescape(string key)
    {
        ArgumentNullException.ThrowIfNull(key);

        return key.StartsWith(EscapePrefix, StringComparison.Ordinal) ? key[1..] : key;
    }
}
