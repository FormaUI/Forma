namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>A resource an <see cref="IResourceProvider"/> resolved from a source address.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public record ResourceInfo
{
    /// <summary>Creates a resource nothing is known about beyond where it lives.</summary>
    public ResourceInfo(string source)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        Source = source;
        Name = ResolveName(source);
    }

    /// <summary>Where the resource lives, as the provider was given it.</summary>
    public string Source { get; }

    /// <summary>The resource's own name, defaulting to the source's last segment.</summary>
    public string Name { get; init; }

    /// <summary>How large the resource is in bytes; a folder counts what it holds.</summary>
    public long? Size { get; init; }

    /// <summary>When the resource came into being.</summary>
    public DateTimeOffset? CreatedAt { get; init; }

    /// <summary>When the resource last changed.</summary>
    public DateTimeOffset? ModifiedAt { get; init; }

    // A source is as likely to be a URL as a path, so the query and fragment are dropped before the last segment.
    private static string ResolveName(string source)
    {
        var path = source.Split('?', '#')[0].TrimEnd('/', '\\');
        var separator = path.LastIndexOfAny(['/', '\\']);
        var name = separator < 0 ? path : path[(separator + 1)..];

        return name.Length == 0 ? source : Uri.UnescapeDataString(name);
    }
}
