namespace Forma.Framework.Localization.Json;

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

    /// <summary>The field naming the revision the translations are at.</summary>
    public const string RevisionField = "Revision";

    /// <summary>The field naming the schema the file is written for.</summary>
    public const string SchemaVersionField = "SchemaVersion";

    /// <summary>The fields an entry may declare after the required ones.</summary>
    public static IReadOnlyList<string> OptionalFields { get; } = [AuthorField, RevisionField];

    /// <summary>The fields every entry declares, in the order it declares them.</summary>
    public static IReadOnlyList<string> RequiredFields { get; } = [SchemaVersionField, CultureField];

    /// <summary>The schema major version this build reads, which is the only part that decides readability.</summary>
    public static int SupportedMajor { get; } = Version.Parse(CurrentVersion).Major;
}
