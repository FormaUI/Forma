namespace Forma.Framework.Localization.Json;

using System.Text.Json;

/// <summary>A translation file as read: its entries, and the root it completes a type's name from when it declares one.</summary>
/// <param name="Root">The file's top-level object.</param>
/// <param name="RootNamespace">The namespace the file completes a type's name from, or null when it declares none.</param>
internal sealed record LocaleFile(JsonElement Root, string? RootNamespace)
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <summary>Reads a file, having checked its metadata lets it be read at all.</summary>
    /// <param name="stream">The file's content.</param>
    /// <param name="name">The file's name, named in anything thrown.</param>
    /// <param name="cultureName">The culture the file's name gives it.</param>
    public static LocaleFile Read(Stream stream, string name, string cultureName)
    {
        using var document = JsonDocument.Parse(stream, DocumentOptions);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"'{name}' is not a JSON object.");
        }

        var marker = document.RootElement.TryGetProperty(LocalizationSchema.Marker, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        var metadata = LocalizationMetadata.Read(marker, name, cultureName);

        return new LocaleFile(document.RootElement.Clone(), metadata.RootNamespace);
    }

    /// <summary>Adds the entries for the type whose name the given segments complete, in the order the file writes them.</summary>
    /// <param name="remainingSegments">The part of the type's name the file's own name leaves out.</param>
    /// <param name="entries">The list the entries are added to.</param>
    /// <param name="name">The file's name, named in anything thrown.</param>
    public void Collect(ReadOnlySpan<string> remainingSegments, List<KeyValuePair<string, string?>> entries, string name)
        => Collect(Root, remainingSegments, entries, name);

    // An object completes the type's name, whether its key spells several segments or one; text is a translation once the name is complete.
    private static void Collect(JsonElement node, ReadOnlySpan<string> remainingSegments, List<KeyValuePair<string, string?>> entries, string name)
    {
        foreach (var property in node.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                var sectionSegments = property.Name.Split('.');
                if (sectionSegments.Length <= remainingSegments.Length && remainingSegments[..sectionSegments.Length].SequenceEqual(sectionSegments))
                {
                    Collect(property.Value, remainingSegments[sectionSegments.Length..], entries, name);
                }
            }
            else if (remainingSegments.Length == 0 && property.Name != LocalizationSchema.Marker)
            {
                entries.Add(new(property.Name, GetText(property.Value, property.Name, name)));
            }
        }
    }

    private static string? GetText(JsonElement value, string key, string name)
        => value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            _ => throw new JsonException($"'{name}' gives '{key}' a value that is not text."),
        };
}
