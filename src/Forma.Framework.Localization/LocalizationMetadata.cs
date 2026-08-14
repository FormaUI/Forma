namespace Forma.Framework.Localization;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Reads and checks the entry every localization file carries to declare what it is.</summary>
internal static class LocalizationMetadata
{
    /// <summary>The key the entry sits under, which also marks the file as a localization file.</summary>
    public const string Marker = "$localization";

    /// <summary>The schema major version this build reads.</summary>
    public const int SupportedSchemaMajor = 1;

    private const string SchemaVersionField = "SchemaVersion";
    private const string CultureField = "Culture";

    /// <summary>Removes the entry from a file's translations, having checked the file may be read at all.</summary>
    /// <param name="entries">The translations as read, including the metadata entry.</param>
    /// <param name="fileName">The file being read, named in anything thrown.</param>
    /// <param name="expectedCulture">The culture the file's own name gives it.</param>
    public static void Consume(Dictionary<string, string?> entries, string fileName, string expectedCulture)
    {
        if (!entries.Remove(Marker, out var value) || value is null)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' carries no '{Marker}' entry, so it cannot be read as a localization file.");
        }

        var result = LocaleFieldParser.Parse(value);
        if (!result.IsValid)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' has a malformed '{Marker}' entry; expected 'SchemaVersion: 1.0.0; Culture: {expectedCulture};'.");
        }

        ReadSchemaVersion(result, value, fileName);
        ReadCulture(result, value, fileName, expectedCulture);
    }

    private static void ReadSchemaVersion(LocaleFieldResult result, string value, string fileName)
    {
        if (Find(result, value, SchemaVersionField) is not { } written)
        {
            throw new LocalizationMetadataException($"'{fileName}' does not declare '{SchemaVersionField}'.");
        }

        // A version we cannot even read says nothing about whether this build could honour it.
        if (!Version.TryParse(written, out var schema) || schema.Build < 0 || schema.Revision >= 0)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' declares '{SchemaVersionField}: {written}', which is not three numbers separated by dots.");
        }

        // Only the major part decides: a differing minor is additive by definition, so it stays readable.
        if (schema.Major != SupportedSchemaMajor)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' is written for localization schema {written}; this build reads {SupportedSchemaMajor}.x.");
        }
    }

    private static void ReadCulture(LocaleFieldResult result, string value, string fileName, string expectedCulture)
    {
        if (Find(result, value, CultureField) is not { } written)
        {
            throw new LocalizationMetadataException($"'{fileName}' does not declare '{CultureField}'.");
        }

        if (!string.Equals(written, expectedCulture, StringComparison.OrdinalIgnoreCase))
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' declares '{CultureField}: {written}' but is read as '{expectedCulture}'.");
        }
    }

    // A name this build does not know is ignored, so a file from a newer minor schema still loads.
    private static string? Find(LocaleFieldResult result, string value, string name)
    {
        foreach (var field in result.Fields)
        {
            if (field.NameSpan.Slice(value).Equals(name, StringComparison.Ordinal))
            {
                return field.ValueSpan.Slice(value).ToString();
            }
        }

        return null;
    }
}
