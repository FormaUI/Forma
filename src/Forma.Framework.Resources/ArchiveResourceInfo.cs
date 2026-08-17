namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>A file resource holding other files in compressed form.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record ArchiveResourceInfo : FileResourceInfo
{
    /// <summary>Creates an archive resource for the source address.</summary>
    public ArchiveResourceInfo(string source)
        : base(source)
    {
    }

    /// <summary>How many resources the archive holds.</summary>
    public int? Count { get; init; }
}
