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

    /// <inheritdoc />
    public int? Width { get; init; }

    /// <inheritdoc />
    public int? Height { get; init; }

    /// <inheritdoc />
    public TimeSpan? Duration { get; init; }
}
