namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>A file resource read as a document.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record DocumentResourceInfo : FileResourceInfo
{
    /// <summary>Creates a document resource for the source address.</summary>
    public DocumentResourceInfo(string source)
        : base(source)
    {
    }

    /// <summary>How many pages the document holds.</summary>
    public int? Count { get; init; }
}
