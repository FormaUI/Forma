namespace Forma.Framework.Explorer.Items.Queries;

using System.IO.Enumeration;

using Forma.Framework.Resources;

/// <summary>Which resources an explorer accepts, read from a pattern.</summary>
/// <remarks>
/// Written either as <c>globs:*.jpg;*.pdf</c> or as <c>contentType:image;video</c>. A content type
/// matches a resource's own media type, and failing that the kind its type stands for.
/// </remarks>
public sealed class ExplorerResourceFilter
{
    private const string ContentTypePrefix = "contentType:";

    private const string GlobsPrefix = "globs:";

    private readonly string[] _contentTypes;

    private readonly string[] _globs;

    private ExplorerResourceFilter(string[] globs, string[] contentTypes)
    {
        _globs = globs;
        _contentTypes = contentTypes;
    }

    /// <summary>The pattern as written, for a message naming what is accepted.</summary>
    public string Pattern { get; private init; } = string.Empty;

    /// <summary>Reads a pattern, or null when none was given.</summary>
    /// <exception cref="ArgumentException">The pattern names neither globs nor content types.</exception>
    public static ExplorerResourceFilter? Parse(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        if (pattern.StartsWith(GlobsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new ExplorerResourceFilter(Split(pattern[GlobsPrefix.Length..]), []) { Pattern = pattern };
        }

        if (pattern.StartsWith(ContentTypePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new ExplorerResourceFilter([], Split(pattern[ContentTypePrefix.Length..])) { Pattern = pattern };
        }

        throw new ArgumentException(
            $"'{pattern}' has to start with '{GlobsPrefix}' or '{ContentTypePrefix}'.",
            nameof(pattern));
    }

    /// <summary>Whether the resource is one the explorer accepts.</summary>
    public bool Accepts(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return Accepts(resource.Name, (resource as FileResourceInfo)?.ContentType, resource);
    }

    /// <summary>Whether a file the reader chose is one the explorer accepts.</summary>
    public bool Accepts(string name, string? contentType)
        => Accepts(name, contentType, resource: null);

    private static string[] Split(string values)
        => [.. values.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

    // A resource whose type says what it is answers even when nothing recorded its media type.
    private static string? GetKind(ResourceInfo? resource)
        => resource switch
        {
            ImageResourceInfo => "image",
            VideoResourceInfo => "video",
            AudioResourceInfo => "audio",
            DocumentResourceInfo => "text",
            ArchiveResourceInfo => "archive",
            _ => null,
        };

    private bool Accepts(string name, string? contentType, ResourceInfo? resource)
    {
        if (_globs.Length > 0)
        {
            return _globs.Any(glob => FileSystemName.MatchesSimpleExpression(glob, name));
        }

        if (_contentTypes.Length == 0)
        {
            return true;
        }

        foreach (var accepted in _contentTypes)
        {
            if (contentType is not null
                && (contentType.StartsWith($"{accepted}/", StringComparison.OrdinalIgnoreCase)
                    || contentType.Equals(accepted, StringComparison.OrdinalIgnoreCase)))
            {
                return true;
            }

            if (GetKind(resource) is { } kind && kind.Equals(accepted, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }
}
