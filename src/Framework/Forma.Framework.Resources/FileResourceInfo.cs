namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>A resource that holds content.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record FileResourceInfo : ResourceInfo
{
    /// <summary>Creates a file resource for the source address.</summary>
    public FileResourceInfo(string source)
        : base(source)
    {
    }

    /// <summary>The resource's media type, as a browser would be told it.</summary>
    public string? ContentType { get; init; }

    /// <summary>The name's extension, including the dot.</summary>
    public string Extension
        => Path.GetExtension(Name);
}
