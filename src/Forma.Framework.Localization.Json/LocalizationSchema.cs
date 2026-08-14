namespace Forma.Framework.Localization.Json;

/// <summary>The shape of the entry every localization file carries to declare what it is.</summary>
public static class LocalizationSchema
{
    /// <summary>The field naming who wrote the translations.</summary>
    public const string AuthorField = "Author";

    /// <summary>The field naming the culture the file carries translations for.</summary>
    public const string CultureField = "Culture";

    /// <summary>The key the entry sits under, which also marks the file as a localization file.</summary>
    public const string Marker = "$localization";

    /// <summary>The field naming the revision the translations are at.</summary>
    public const string RevisionField = "Revision";

    /// <summary>The field naming the schema the file is written for.</summary>
    public const string SchemaVersionField = "SchemaVersion";

    /// <summary>The schema major version this build reads.</summary>
    public const int SupportedMajor = 1;

    /// <summary>The fields an entry may declare after the required ones.</summary>
    public static IReadOnlyList<string> OptionalFields { get; } = [AuthorField, RevisionField];

    /// <summary>The fields every entry declares, in the order it declares them.</summary>
    public static IReadOnlyList<string> RequiredFields { get; } = [SchemaVersionField, CultureField];
}
