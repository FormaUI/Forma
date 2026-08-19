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

    /// <summary>How wide the image is in pixels.</summary>
    public int? Width { get; init; }

    /// <summary>How tall the image is in pixels.</summary>
    public int? Height { get; init; }
}
