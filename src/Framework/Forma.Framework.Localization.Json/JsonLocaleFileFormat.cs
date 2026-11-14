namespace Forma.Framework.Localization.Json;

using System.Buffers;
using System.Text.Json;

using Forma.Framework.Localization.Files;

/// <summary>The JSON format a locale file is written in: an object whose text properties are entries and whose nested objects are sections.</summary>
public sealed class JsonLocaleFileFormat : ILocaleFileFormat
{
    private static readonly JsonReaderOptions ReaderOptions = new()
    {
        AllowTrailingCommas = true,
        CommentHandling = JsonCommentHandling.Skip,
    };

    /// <inheritdoc />
    public string Extension => ".json";

    /// <inheritdoc />
    /// <remarks>
    /// The file is read in one pass, and what it writes wrongly is reported once the pass ends, in this order: JSON that
    /// can't be read, a file that isn't an object, the entry declaring what the file is, then the first entry the file
    /// writes wrongly.
    /// </remarks>
    public LocaleFile Read(Stream stream, string fileName, string cultureName)
    {
        ArgumentNullException.ThrowIfNull(stream);
        ArgumentException.ThrowIfNullOrEmpty(fileName);
        ArgumentException.ThrowIfNullOrEmpty(cultureName);

        var buffer = ReadToEnd(stream, out var length);

        try
        {
            var findings = new Findings(fileName);
            var root = Parse(buffer.AsSpan(0, length), findings) ?? throw new LocaleFileException($"'{fileName}' is not a JSON object.");

            LocalizationMetadata.Validate(findings.Marker, fileName, cultureName);

            return findings.Problem is { } problem ? throw new LocaleFileException(problem) : new LocaleFile(root);
        }
        finally
        {
            ArrayPool<byte>.Shared.Return(buffer);
        }
    }

    private static LocaleSection? Parse(ReadOnlySpan<byte> text, Findings findings)
    {
        try
        {
            var reader = new Utf8JsonReader(text.StartsWith(Utf8Bom) ? text[Utf8Bom.Length..] : text, ReaderOptions);
            reader.Read();

            LocaleSection? root = null;
            if (reader.TokenType == JsonTokenType.StartObject)
            {
                root = ReadSection(ref reader, findings, isRoot: true);
            }
            else
            {
                reader.Skip();
            }

            // A value after the file's one value is read only to be rejected.
            while (reader.Read())
            {
            }

            return root;
        }
        catch (JsonException exception)
        {
            throw new LocaleFileException($"'{findings.FileName}' is not readable JSON: {exception.Message}", exception);
        }
    }

    // A reserved name is matched as written, before any escape is removed from a key.
    private static LocaleSection ReadSection(ref Utf8JsonReader reader, Findings findings, bool isRoot)
    {
        var sections = new List<KeyValuePair<string, LocaleSection>>();
        var entries = new List<KeyValuePair<string, string?>>();
        var sharedEntries = new List<KeyValuePair<string, string?>>();

        while (reader.Read() && reader.TokenType == JsonTokenType.PropertyName)
        {
            var name = reader.GetString()!;
            reader.Read();

            // Of two markers at the top level the last is the file's, and one that isn't text declares nothing.
            if (name == LocalizationSchema.Marker)
            {
                var isText = reader.TokenType == JsonTokenType.String;
                if (isRoot)
                {
                    findings.Marker = isText ? reader.GetString() : null;
                }

                if (isText)
                {
                    continue;
                }
            }

            if (name == LocalizationSchema.SharedKey)
            {
                if (reader.TokenType != JsonTokenType.StartObject)
                {
                    findings.Report($"'{findings.FileName}' gives '{name}' a value that is not an object.");
                    reader.Skip();

                    continue;
                }

                var section = ReadSection(ref reader, findings, isRoot: false);
                if (section.Sections.Count > 0 || section.SharedEntries.Count > 0)
                {
                    findings.Report($"'{findings.FileName}' nests '{(section.Sections.Count > 0 ? section.Sections[0].Key : LocalizationSchema.SharedKey)}' inside '{name}', which holds entries alone.");
                }

                sharedEntries.AddRange(section.Entries);

                continue;
            }

            var key = GetKey(name, findings);

            switch (reader.TokenType)
            {
                case JsonTokenType.StartObject:
                    sections.Add(new(key, ReadSection(ref reader, findings, isRoot: false)));
                    break;
                case JsonTokenType.String:
                    entries.Add(new(key, reader.GetString()));
                    break;
                case JsonTokenType.Null:
                    entries.Add(new(key, null));
                    break;
                default:
                    findings.Report($"'{findings.FileName}' gives '{name}' a value that is not text.");
                    reader.Skip();
                    break;
            }
        }

        return new LocaleSection(sections, entries, sharedEntries);
    }

    private static string GetKey(string name, Findings findings)
    {
        if (name.StartsWith(LocalizationSchema.EscapePrefix, StringComparison.Ordinal))
        {
            return LocalizationSchema.Unescape(name);
        }

        if (LocalizationSchema.IsReservedShape(name) && name is not (LocalizationSchema.Marker or LocalizationSchema.SharedKey))
        {
            findings.Report($"'{findings.FileName}' names '{name}', which is shaped like a reserved entry the runtime does not define.");
        }

        return name;
    }

    // The stream is read whole into a pooled buffer, one byte longer than a seekable stream holds, so the read that finds
    // its end needs no larger one.
    private static byte[] ReadToEnd(Stream stream, out int length)
    {
        var buffer = ArrayPool<byte>.Shared.Rent(stream.CanSeek ? (int)Math.Max(stream.Length - stream.Position, 0) + 1 : 4096);
        length = 0;

        try
        {
            int read;
            while ((read = stream.Read(buffer, length, buffer.Length - length)) > 0)
            {
                length += read;
                if (length == buffer.Length)
                {
                    var larger = ArrayPool<byte>.Shared.Rent(buffer.Length * 2);
                    buffer.AsSpan(0, length).CopyTo(larger);
                    ArrayPool<byte>.Shared.Return(buffer);
                    buffer = larger;
                }
            }

            return buffer;
        }
        catch
        {
            ArrayPool<byte>.Shared.Return(buffer);

            throw;
        }
    }

    private static ReadOnlySpan<byte> Utf8Bom
        => [0xEF, 0xBB, 0xBF];

    /// <summary>What reading a file found that is judged once the whole file is read: the entry declaring what the file is, and the first thing it writes wrongly.</summary>
    /// <param name="fileName">The name the file is reported by.</param>
    private sealed class Findings(string fileName)
    {
        /// <summary>The name the file is reported by.</summary>
        public string FileName
            => fileName;

        /// <summary>The text of the entry declaring what the file is.</summary>
        public string? Marker { get; set; }

        /// <summary>The first thing the file writes wrongly.</summary>
        public string? Problem { get; private set; }

        /// <summary>Records what the file writes wrongly, keeping the first.</summary>
        /// <param name="problem">What the file writes wrongly.</param>
        public void Report(string problem)
            => Problem ??= problem;
    }
}
