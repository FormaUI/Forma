namespace Forma.Framework.Localization.Linter.Linting;

using Forma.Framework.Localization.Json;
using Forma.Framework.Localization.Linter.Diagnostics;
using Forma.Framework.Localization.Linter.Documents;
using Forma.Framework.Localization.Parser;
using Forma.Framework.Localization.Parser.Syntax;

/// <summary>Checks a file's metadata entry against the schema, strictly where the runtime is permissive.</summary>
public static class SchemaLinter
{
    /// <summary>Reports everything the entry gets wrong, in the order the file would be read.</summary>
    /// <param name="document">The file to check.</param>
    /// <param name="expectedCulture">The culture the file's own name gives it.</param>
    public static IEnumerable<LintDiagnostic> Lint(LocalizationDocument document, string? expectedCulture)
    {
        ArgumentNullException.ThrowIfNull(document);

        return Walk(document, expectedCulture);
    }

    private static IEnumerable<LintDiagnostic> Walk(LocalizationDocument document, string? expectedCulture)
    {
        var index = IndexOfMarker(document);
        if (index < 0)
        {
            yield return document.Diagnose(
                0,
                LintCodes.MissingMarker,
                $"The file carries no '{LocalizationSchema.Marker}' entry, so it cannot be read as a localization file.");

            yield break;
        }

        var marker = document.Entries[index];
        if (index != 0)
        {
            // The runtime is order-agnostic about presence alone, so a marker sitting elsewhere still loads.
            yield return document.Diagnose(
                marker.NameOffset,
                LintCodes.MarkerNotFirst,
                $"'{LocalizationSchema.Marker}' is entry {index + 1}; it has to be the first.",
                LintSeverity.Warning);
        }

        if (marker.Value is not { } value)
        {
            yield return document.Diagnose(
                marker.ValueOffset,
                LintCodes.MalformedMetadata,
                $"'{LocalizationSchema.Marker}' carries no text.");

            yield break;
        }

        var result = LocaleFieldParser.Parse(value);
        if (!result.IsValid)
        {
            yield return document.Diagnose(
                marker.ValueOffset,
                LintCodes.MalformedMetadata,
                $"'{LocalizationSchema.Marker}' is malformed; expected '{Expected(expectedCulture)}'.");

            yield break;
        }

        var names = Names(result, value);

        foreach (var diagnostic in LintRequired(document, marker, names))
        {
            yield return diagnostic;
        }

        foreach (var diagnostic in LintUnknown(document, marker, names))
        {
            yield return diagnostic;
        }

        foreach (var diagnostic in LintVersion(document, marker, result, value))
        {
            yield return diagnostic;
        }

        foreach (var diagnostic in LintCulture(document, marker, result, value, expectedCulture))
        {
            yield return diagnostic;
        }
    }

    private static IEnumerable<LintDiagnostic> LintRequired(LocalizationDocument document, LocalizationEntry marker, IReadOnlyList<string> names)
    {
        var required = LocalizationSchema.RequiredFields;

        foreach (var name in required)
        {
            if (!names.Contains(name, StringComparer.Ordinal))
            {
                yield return document.Diagnose(
                    marker.ValueOffset,
                    LintCodes.MissingRequiredField,
                    $"'{LocalizationSchema.Marker}' does not declare '{name}'.");
            }
        }

        // Only the required fields are ordered, so the optional ones are stripped out before the order is judged.
        var written = names.Where(name => required.Contains(name, StringComparer.Ordinal)).ToArray();
        var wanted = required.Where(name => written.Contains(name, StringComparer.Ordinal)).ToArray();

        if (!written.SequenceEqual(wanted, StringComparer.Ordinal))
        {
            // The runtime reads the fields by name regardless of order, so this is only ever a linter concern.
            yield return document.Diagnose(
                marker.ValueOffset,
                LintCodes.RequiredFieldOutOfOrder,
                $"'{LocalizationSchema.Marker}' declares {string.Join(", then ", written)}; the schema orders them {string.Join(", then ", wanted)}.",
                LintSeverity.Warning);
        }
    }

    private static IEnumerable<LintDiagnostic> LintUnknown(LocalizationDocument document, LocalizationEntry marker, IReadOnlyList<string> names)
    {
        foreach (var name in names)
        {
            if (!LocalizationSchema.RequiredFields.Contains(name, StringComparer.Ordinal)
                && !LocalizationSchema.OptionalFields.Contains(name, StringComparer.Ordinal))
            {
                // The runtime ignores a name it does not know, for forward compatibility with a newer minor schema.
                yield return document.Diagnose(
                    marker.ValueOffset,
                    LintCodes.UnknownMetadataField,
                    $"'{LocalizationSchema.Marker}' declares '{name}', which the schema does not define.",
                    LintSeverity.Warning);
            }
        }
    }

    private static IEnumerable<LintDiagnostic> LintVersion(LocalizationDocument document, LocalizationEntry marker, LocaleFieldResult result, string value)
    {
        if (Find(result, value, LocalizationSchema.SchemaVersionField) is not { } written)
        {
            yield break;
        }

        if (string.Equals(written, LocalizationSchema.CurrentVersion, StringComparison.Ordinal))
        {
            yield break;
        }

        // The runtime rejects only a differing major; a differing minor or patch still loads, so it is a warning here.
        var severity = Version.TryParse(written, out var schema) && schema.Major == LocalizationSchema.SupportedMajor
            ? LintSeverity.Warning
            : LintSeverity.Error;

        yield return document.Diagnose(
            marker.ValueOffset,
            LintCodes.SchemaVersionMismatch,
            $"The file declares schema {written}; this build defines {LocalizationSchema.CurrentVersion}.",
            severity);
    }

    private static IEnumerable<LintDiagnostic> LintCulture(LocalizationDocument document, LocalizationEntry marker, LocaleFieldResult result, string value, string? expectedCulture)
    {
        if (expectedCulture is null || Find(result, value, LocalizationSchema.CultureField) is not { } written)
        {
            yield break;
        }

        if (!string.Equals(written, expectedCulture, StringComparison.OrdinalIgnoreCase))
        {
            yield return document.Diagnose(
                marker.ValueOffset,
                LintCodes.CultureMismatch,
                $"The file declares '{LocalizationSchema.CultureField}: {written}' but is named for '{expectedCulture}'.");
        }
    }

    private static int IndexOfMarker(LocalizationDocument document)
    {
        for (var index = 0; index < document.Entries.Count; index++)
        {
            if (string.Equals(document.Entries[index].Name, LocalizationSchema.Marker, StringComparison.Ordinal))
            {
                return index;
            }
        }

        return -1;
    }

    private static IReadOnlyList<string> Names(LocaleFieldResult result, string value)
        => [.. result.Fields.Select(field => field.NameSpan.Slice(value).ToString())];

    private static string? Find(LocaleFieldResult result, string value, string name)
    {
        foreach (var field in result.Fields)
        {
            if (field.NameSpan.Slice(value).SequenceEqual(name))
            {
                return field.ValueSpan.Slice(value).ToString();
            }
        }

        return null;
    }

    private static string Expected(string? culture)
        => $"{LocalizationSchema.SchemaVersionField}: {LocalizationSchema.CurrentVersion}; {LocalizationSchema.CultureField}: {culture ?? "<culture>"};";
}
