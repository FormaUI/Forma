namespace Forma.Framework.Localization.Files;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Checks the entry every localization file carries to declare what it is.</summary>
public static class LocalizationMetadata
{
    /// <summary>Checks that a file's metadata entry lets the file be read.</summary>
    /// <param name="value">The entry's value.</param>
    /// <param name="fileName">The name of the file being read.</param>
    /// <param name="expectedCulture">The culture the file's own name gives it.</param>
    /// <exception cref="LocalizationMetadataException">The entry is missing or malformed, is written for a schema this build does not read, or declares another culture.</exception>
    public static void Validate(string? value, string fileName, string expectedCulture)
    {
        if (value is null)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' carries no '{LocalizationSchema.Marker}' entry, so it cannot be read as a localization file.");
        }

        var result = LocaleFieldParser.Parse(value);
        if (!result.IsValid)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' has a malformed '{LocalizationSchema.Marker}' entry; expected 'SchemaVersion: {LocalizationSchema.CurrentVersion}; Culture: {expectedCulture};'.");
        }

        ValidateSchemaVersion(result, value, fileName);
        ValidateCulture(result, value, fileName, expectedCulture);
    }

    private static void ValidateSchemaVersion(LocaleFieldResult result, string value, string fileName)
    {
        if (Find(result, value, LocalizationSchema.SchemaVersionField) is not { } writtenVersion)
        {
            throw new LocalizationMetadataException($"'{fileName}' does not declare '{LocalizationSchema.SchemaVersionField}'.");
        }

        if (!Version.TryParse(writtenVersion, out var schema) || schema.Build < 0 || schema.Revision >= 0)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' declares '{LocalizationSchema.SchemaVersionField}: {writtenVersion}', which is not three numbers separated by dots.");
        }

        // Only the major version decides whether a file is readable.
        if (schema.Major != LocalizationSchema.SupportedMajor)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' is written for localization schema {writtenVersion}; this build reads {LocalizationSchema.SupportedMajor}.x.");
        }
    }

    private static void ValidateCulture(LocaleFieldResult result, string value, string fileName, string expectedCulture)
    {
        if (Find(result, value, LocalizationSchema.CultureField) is not { } writtenCulture)
        {
            throw new LocalizationMetadataException($"'{fileName}' does not declare '{LocalizationSchema.CultureField}'.");
        }

        if (!string.Equals(writtenCulture, expectedCulture, StringComparison.OrdinalIgnoreCase))
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' declares '{LocalizationSchema.CultureField}: {writtenCulture}' but is read as '{expectedCulture}'.");
        }
    }

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
