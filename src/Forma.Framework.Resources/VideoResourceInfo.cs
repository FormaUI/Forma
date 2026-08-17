namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>A file resource a browser plays as video.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record VideoResourceInfo : FileResourceInfo, IVisualResource, ITimedResource
{
    /// <summary>Creates a video resource for the source address.</summary>
    public VideoResourceInfo(string source)
        : base(source)
    {
    }

    /// <summary>How wide the video is in pixels.</summary>
    public int? Width { get; init; }

    /// <summary>How tall the video is in pixels.</summary>
    public int? Height { get; init; }

    /// <summary>How long the video runs for.</summary>
    public TimeSpan? Duration { get; init; }
}
