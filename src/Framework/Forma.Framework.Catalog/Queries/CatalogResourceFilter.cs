namespace Forma.Framework.Catalog.Queries;

using System.IO.Enumeration;

using Forma.Framework.Resources;

/// <summary>A filter of the resources a catalog accepts, parsed from a pattern.</summary>
/// <remarks>
/// Written either as <c>globs:*.jpg;*.pdf</c> or as <c>contentType:image;video</c>. A content type
/// matches a resource's own media type, and failing that the kind its type stands for.
/// </remarks>
public sealed class CatalogResourceFilter
{
    private const string ContentTypePrefix = "contentType:";

    private const string GlobsPrefix = "globs:";

    private readonly string[] _contentTypes;

    private readonly string[] _globs;

    private CatalogResourceFilter(string[] globs, string[] contentTypes)
    {
        _globs = globs;
        _contentTypes = contentTypes;
    }

    /// <summary>The pattern the filter was parsed from.</summary>
    public string Pattern { get; private init; } = string.Empty;

    /// <summary>Parses the pattern into a filter.</summary>
    /// <exception cref="ArgumentException">The pattern names neither globs nor content types.</exception>
    public static CatalogResourceFilter? Parse(string? pattern)
    {
        if (string.IsNullOrWhiteSpace(pattern))
        {
            return null;
        }

        if (pattern.StartsWith(GlobsPrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new CatalogResourceFilter(Split(pattern[GlobsPrefix.Length..]), []) { Pattern = pattern };
        }

        if (pattern.StartsWith(ContentTypePrefix, StringComparison.OrdinalIgnoreCase))
        {
            return new CatalogResourceFilter([], Split(pattern[ContentTypePrefix.Length..])) { Pattern = pattern };
        }

        throw new ArgumentException(
            $"'{pattern}' has to start with '{GlobsPrefix}' or '{ContentTypePrefix}'.",
            nameof(pattern));
    }

    /// <summary>Whether the resource is one the catalog accepts.</summary>
    public bool Accepts(ResourceInfo resource)
    {
        ArgumentNullException.ThrowIfNull(resource);

        return Accepts(resource.Name, (resource as FileResourceInfo)?.ContentType, resource);
    }

    /// <summary>Whether a file with the name and content type is one the catalog accepts.</summary>
    public bool Accepts(string name, string? contentType)
        => Accepts(name, contentType, resource: null);

    private static string[] Split(string values)
        => [.. values.Split(';', StringSplitOptions.RemoveEmptyEntries | StringSplitOptions.TrimEntries)];

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
