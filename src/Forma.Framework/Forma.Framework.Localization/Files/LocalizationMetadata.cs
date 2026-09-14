namespace Forma.Framework.Localization.Files;

using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Reads and checks the entry every localization file carries to declare what it is.</summary>
public static class LocalizationMetadata
{
    /// <summary>Reads what the entry declares, having checked that its value, or its absence, lets the file be read at all.</summary>
    /// <param name="value">The entry's value, or null when the file carries none.</param>
    /// <param name="fileName">The file being read, named in anything thrown.</param>
    /// <param name="expectedCulture">The culture the file's own name gives it.</param>
    public static LocalizationFileMetadata Read(string? value, string fileName, string expectedCulture)
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

        ReadSchemaVersion(result, value, fileName);
        ReadCulture(result, value, fileName, expectedCulture);

        return new LocalizationFileMetadata(Find(result, value, LocalizationSchema.RootNamespaceField));
    }

    private static void ReadSchemaVersion(LocaleFieldResult result, string value, string fileName)
    {
        if (Find(result, value, LocalizationSchema.SchemaVersionField) is not { } written)
        {
            throw new LocalizationMetadataException($"'{fileName}' does not declare '{LocalizationSchema.SchemaVersionField}'.");
        }

        // A version we cannot even read says nothing about whether this build could honor it.
        if (!Version.TryParse(written, out var schema) || schema.Build < 0 || schema.Revision >= 0)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' declares '{LocalizationSchema.SchemaVersionField}: {written}', which is not three numbers separated by dots.");
        }

        // Only the major part decides: a differing minor is additive by definition, so it stays readable.
        if (schema.Major != LocalizationSchema.SupportedMajor)
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' is written for localization schema {written}; this build reads {LocalizationSchema.SupportedMajor}.x.");
        }
    }

    private static void ReadCulture(LocaleFieldResult result, string value, string fileName, string expectedCulture)
    {
        if (Find(result, value, LocalizationSchema.CultureField) is not { } written)
        {
            throw new LocalizationMetadataException($"'{fileName}' does not declare '{LocalizationSchema.CultureField}'.");
        }

        if (!string.Equals(written, expectedCulture, StringComparison.OrdinalIgnoreCase))
        {
            throw new LocalizationMetadataException(
                $"'{fileName}' declares '{LocalizationSchema.CultureField}: {written}' but is read as '{expectedCulture}'.");
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
