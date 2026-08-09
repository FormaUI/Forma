namespace Forma.Framework.Files;

using System.Diagnostics.CodeAnalysis;
using System.Globalization;

/// <summary>A resource an <see cref="IResourceProvider"/> resolved from a source address.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public record ResourceInfo
{
    public ResourceInfo(string source, ResourceKind kind = ResourceKind.Unknown)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);

        Source = source;
        Name = ResolveName(source);
        Kind = kind;
    }

    /// <summary>Where the resource lives, as the provider was given it.</summary>
    public string Source { get; }

    /// <summary>The resource's own name, taken from the source's last segment.</summary>
    public string Name { get; }

    /// <summary>What the resource is, deciding which preview a viewer renders.</summary>
    public ResourceKind Kind { get; init; }

    /// <summary>How large the resource is, when something along the way knew; <see cref="DisplaySize"/> renders it.</summary>
    public long? Size { get; init; }

    /// <summary>Size written for a reader, taking it as a count of bytes; a resource measured in anything else overrides this.</summary>
    public virtual string? DisplaySize
        => Size is { } size ? FormatBytes(size) : null;

    /// <summary>The name's extension, including the dot, or empty when it has none; compare it case-insensitively.</summary>
    public string Extension
        => Path.GetExtension(Name);

    private static string FormatBytes(long bytes)
    {
        const long Kb = 1024;
        const long Mb = Kb * 1024;
        const long Gb = Mb * 1024;

        return bytes switch
        {
            < Kb => string.Format(CultureInfo.CurrentCulture, "{0} B", bytes),
            < Mb => string.Format(CultureInfo.CurrentCulture, "{0:0.#} KB", bytes / (double)Kb),
            < Gb => string.Format(CultureInfo.CurrentCulture, "{0:0.#} MB", bytes / (double)Mb),
            _ => string.Format(CultureInfo.CurrentCulture, "{0:0.#} GB", bytes / (double)Gb),
        };
    }

    // A source is as likely to be a URL as a path, so the query and fragment are dropped before the last segment.
    private static string ResolveName(string source)
    {
        var path = source.Split('?', '#')[0].TrimEnd('/', '\\');
        var separator = path.LastIndexOfAny(['/', '\\']);
        var name = separator < 0 ? path : path[(separator + 1)..];

        return name.Length == 0 ? source : Uri.UnescapeDataString(name);
    }
}
