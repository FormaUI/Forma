namespace Forma.Framework.Localization.Json;

using System.Collections.Concurrent;
using System.Text.Json;

/// <summary>The folder a host keeps its own translation files in, laid out by assembly and type, each file read once.</summary>
/// <param name="path">The folder's full path, or empty when the host keeps no such folder.</param>
internal sealed class LocalesFolder(string path)
{
    private static readonly JsonDocumentOptions DocumentOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    private readonly string _path = path;
    private readonly ConcurrentDictionary<string, LocalesFile?> _files = new(StringComparer.Ordinal);

    /// <summary>The host's translations for a type in one culture, root by root and within a root from the file named for the assembly down to the file named for the type, so a later entry overrides an earlier one.</summary>
    /// <param name="assemblyName">The name of the assembly the type lives in.</param>
    /// <param name="typeName">The type's full name.</param>
    /// <param name="cultureName">The culture the files are named for.</param>
    public IReadOnlyList<KeyValuePair<string, string?>> Read(string assemblyName, string typeName, string cultureName)
    {
        var entries = new List<KeyValuePair<string, string?>>();
        if (_path.Length == 0)
        {
            return entries;
        }

        var segments = typeName.Split('.');
        var assemblySegments = assemblyName.Split('.');
        var leadsWithAssembly = segments.Length > assemblySegments.Length && segments.AsSpan(0, assemblySegments.Length).SequenceEqual(assemblySegments);

        // The assembly's name is the root a file need not declare; a type rooted elsewhere is laid out by its full name under it.
        ReadRoot(assemblyName, assemblyName, leadsWithAssembly ? segments[assemblySegments.Length..] : segments, cultureName, entries);

        for (var length = 1; length < segments.Length; length++)
        {
            var rootNamespace = string.Join('.', segments[..length]);
            if (rootNamespace != assemblyName)
            {
                ReadRoot(assemblyName, rootNamespace, segments[length..], cultureName, entries);
            }
        }

        return entries;
    }

    private void ReadRoot(string assemblyName, string rootNamespace, string[] segments, string cultureName, List<KeyValuePair<string, string?>> entries)
    {
        for (var depth = 0; depth <= segments.Length; depth++)
        {
            var filePath = GetFilePath(assemblyName, segments, depth, cultureName);
            if (GetFile(filePath, cultureName) is { } file && (file.RootNamespace ?? assemblyName) == rootNamespace)
            {
                Collect(file.Root, segments.AsSpan(depth), entries, filePath);
            }
        }
    }

    // The file named for the assembly sits in the folder itself; every other one sits under the assembly's own folder.
    private string GetFilePath(string assemblyName, string[] segments, int depth, string cultureName)
        => depth == 0
            ? Path.Combine(_path, $"{assemblyName}.{cultureName}.json")
            : Path.Combine(_path, assemblyName, Path.Combine(segments[..depth]) + $".{cultureName}.json");

    private LocalesFile? GetFile(string filePath, string cultureName)
        => _files.GetOrAdd(filePath, static (filePath, cultureName) => Parse(filePath, cultureName), cultureName);

    private static LocalesFile? Parse(string filePath, string cultureName)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var stream = File.OpenRead(filePath);
        using var document = JsonDocument.Parse(stream, DocumentOptions);
        if (document.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new JsonException($"'{filePath}' is not a JSON object.");
        }

        var marker = document.RootElement.TryGetProperty(LocalizationSchema.Marker, out var value) && value.ValueKind == JsonValueKind.String ? value.GetString() : null;
        var metadata = LocalizationMetadata.Read(marker, filePath, cultureName);

        return new LocalesFile(document.RootElement.Clone(), metadata.RootNamespace);
    }

    // An object completes the type's name, whether its key spells several segments or one; text is a translation once the name is complete.
    private static void Collect(JsonElement node, ReadOnlySpan<string> remainingSegments, List<KeyValuePair<string, string?>> entries, string filePath)
    {
        foreach (var property in node.EnumerateObject())
        {
            if (property.Value.ValueKind == JsonValueKind.Object)
            {
                var sectionSegments = property.Name.Split('.');
                if (sectionSegments.Length <= remainingSegments.Length && remainingSegments[..sectionSegments.Length].SequenceEqual(sectionSegments))
                {
                    Collect(property.Value, remainingSegments[sectionSegments.Length..], entries, filePath);
                }
            }
            else if (remainingSegments.Length == 0 && property.Name != LocalizationSchema.Marker)
            {
                entries.Add(new(property.Name, GetText(property.Value, property.Name, filePath)));
            }
        }
    }

    private static string? GetText(JsonElement value, string name, string filePath)
        => value.ValueKind switch
        {
            JsonValueKind.Null => null,
            JsonValueKind.String => value.GetString(),
            _ => throw new JsonException($"'{filePath}' gives '{name}' a value that is not text."),
        };

    /// <summary>A host file as read: its translations, and the root it completes a type's name from when it declares one.</summary>
    private sealed record LocalesFile(JsonElement Root, string? RootNamespace);
}
