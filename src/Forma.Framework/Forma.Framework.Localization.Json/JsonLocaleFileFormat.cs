namespace Forma.Framework.Localization.Json;

using System.Text.Json;

using Forma.Framework.Localization.Files;

/// <summary>The JSON format a locale file is written in: an object whose text values are translations and whose nested objects are sections.</summary>
public sealed class JsonLocaleFileFormat : ILocaleFileFormat
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <inheritdoc />
    public string Extension => ".json";

    /// <inheritdoc />
    public LocaleFile Read(Stream stream, string fileName, string cultureName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentException.ThrowIfNullOrEmpty(cultureName);

        using var document = JsonDocument.Parse(stream, DocumentOptions);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"'{fileName}' is not a JSON object.");
        }

        var marker = document.RootElement.TryGetProperty(LocalizationSchema.Marker, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        LocalizationMetadata.Validate(marker, fileName, cultureName);

        return new LocaleFile(ReadSection(document.RootElement, fileName));
    }

    // The reserved names are read as written, before any escape: the marker is the file's own, and the shared section's entries go to everything beneath the node. Every other key is read as the text behind its escape; an object is a section, text or null a translation.
    private static LocaleSection ReadSection(JsonElement node, string fileName)
    {
        var sections = new List<KeyValuePair<string, LocaleSection>>();
        var entries = new List<KeyValuePair<string, string?>>();
        var shared = new List<KeyValuePair<string, string?>>();

        foreach (var property in node.EnumerateObject())
        {
            if (property.Name == LocalizationSchema.Marker && property.Value.ValueKind == JsonValueKind.String)
            {
                continue;
            }

            if (property.Name == LocalizationSchema.SharedKey)
            {
                if (property.Value.ValueKind != JsonValueKind.Object)
                {
                    throw new JsonException($"'{fileName}' gives '{property.Name}' a value that is not an object.");
                }

                var section = ReadSection(property.Value, fileName);
                if (section.Sections.Count > 0 || section.SharedEntries.Count > 0)
                {
                    throw new JsonException($"'{fileName}' nests '{(section.Sections.Count > 0 ? section.Sections[0].Key : LocalizationSchema.SharedKey)}' inside '{property.Name}', which holds translations alone.");
                }

                shared.AddRange(section.Entries);
                continue;
            }

            var key = GetKey(property.Name, fileName);

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    sections.Add(new(key, ReadSection(property.Value, fileName)));
                    break;
                case JsonValueKind.String:
                    entries.Add(new(key, property.Value.GetString()));
                    break;
                case JsonValueKind.Null:
                    entries.Add(new(key, null));
                    break;
                default:
                    throw new JsonException($"'{fileName}' gives '{property.Name}' a value that is not text.");
            }
        }

        return new LocaleSection(sections, entries, shared);
    }

    // A key shaped like a reserved entry is the runtime's to define, so one it does not define is refused rather than read as text.
    private static string GetKey(string name, string fileName)
    {
        if (name.StartsWith(LocalizationSchema.EscapePrefix, StringComparison.Ordinal))
        {
            return LocalizationSchema.Unescape(name);
        }

        if (LocalizationSchema.IsReservedShape(name) && name is not (LocalizationSchema.Marker or LocalizationSchema.SharedKey))
        {
            throw new JsonException($"'{fileName}' names '{name}', which is shaped like a reserved entry the runtime does not define.");
        }

        return name;
    }
}
