namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>A resource that contains other resources.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record FolderResourceInfo : ResourceInfo
{
    /// <summary>Creates a folder resource for the source address.</summary>
    public FolderResourceInfo(string source)
        : base(source)
    {
    }

    /// <summary>The source of the folder this one sits inside, or null when it is top-level.</summary>
    [SuppressMessage("Design", "CA1056:URI properties should not be strings")]
    public string? Parent { get; init; }

    /// <summary>How many resources the folder holds.</summary>
    public int? Count { get; init; }
}
