namespace Forma.Framework.Localization.Json;

using System.Text.Json;

using Forma.Framework.Localization.Files;

/// <summary>The JSON format a translation file is written in: an object whose text values are translations and whose nested objects are sections.</summary>
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
        var metadata = LocalizationMetadata.Read(marker, fileName, cultureName);

        return new LocaleFile(ReadSection(document.RootElement, fileName), metadata.RootNamespace);
    }

    // An object is a section; text or null is a translation; the marker is the file's own, and the entry reserved for tools is left to them whatever it holds.
    private static LocaleSection ReadSection(JsonElement node, string fileName)
    {
        var sections = new List<KeyValuePair<string, LocaleSection>>();
        var entries = new List<KeyValuePair<string, string?>>();

        foreach (var property in node.EnumerateObject())
        {
            if (property.Name == LocalizationSchema.ToolsKey)
            {
                continue;
            }

            switch (property.Value.ValueKind)
            {
                case JsonValueKind.Object:
                    sections.Add(new(property.Name, ReadSection(property.Value, fileName)));
                    break;
                case JsonValueKind.String when property.Name != LocalizationSchema.Marker:
                    entries.Add(new(property.Name, property.Value.GetString()));
                    break;
                case JsonValueKind.String:
                    break;
                case JsonValueKind.Null:
                    entries.Add(new(property.Name, null));
                    break;
                default:
                    throw new JsonException($"'{fileName}' gives '{property.Name}' a value that is not text.");
            }
        }

        return new LocaleSection(sections, entries);
    }
}
