namespace Forma.Framework.Resources;

/// <summary>A resource that runs for a length of time, such as audio or video.</summary>
public interface ITimedResource
{
    /// <summary>How long the resource runs for.</summary>
    TimeSpan? Duration { get; }
}
