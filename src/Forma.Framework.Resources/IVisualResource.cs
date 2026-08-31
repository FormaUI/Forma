namespace Forma.Framework.Resources;

/// <summary>A resource that occupies a pixel area, such as an image or a video.</summary>
public interface IVisualResource
{
    /// <summary>How wide the resource is in pixels.</summary>
    int? Width { get; }

    /// <summary>How tall the resource is in pixels.</summary>
    int? Height { get; }
}
