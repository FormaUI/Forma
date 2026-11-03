namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>A file resource a browser renders as an image.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record ImageResourceInfo : FileResourceInfo, IVisualResource
{
    /// <summary>Creates an image resource for the source address.</summary>
    public ImageResourceInfo(string source)
        : base(source)
    {
    }

    /// <inheritdoc />
    public int? Width { get; init; }

    /// <inheritdoc />
    public int? Height { get; init; }
}
