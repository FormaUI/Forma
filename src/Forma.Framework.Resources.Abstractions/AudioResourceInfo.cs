namespace Forma.Framework.Resources;

using System.Diagnostics.CodeAnalysis;

/// <summary>A file resource a browser plays as audio.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
public record AudioResourceInfo : FileResourceInfo, ITimedResource
{
    /// <summary>Creates an audio resource for the source address.</summary>
    public AudioResourceInfo(string source)
        : base(source)
    {
    }

    /// <summary>How long the audio runs for.</summary>
    public TimeSpan? Duration { get; init; }
}
