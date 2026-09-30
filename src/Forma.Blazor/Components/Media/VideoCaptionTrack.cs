namespace Forma.Blazor.Components.Media;

using System.Diagnostics.CodeAnalysis;

/// <summary>A caption file offered alongside a self-hosted video.</summary>
[SuppressMessage("Design", "CA1054:URI parameters should not be strings")]
[SuppressMessage("Design", "CA1056:URI properties should not be strings")]
public sealed record VideoCaptionTrack
{
    /// <summary>The source address of the WebVTT file.</summary>
    public string Source { get; }

    /// <summary>The BCP 47 tag of the language the captions are written in.</summary>
    public string Language { get; }

    /// <summary>The label shown in the player's caption menu.</summary>
    public string Label { get; }

    /// <summary>Whether these captions show by default.</summary>
    public bool IsDefault { get; init; }

    /// <summary>Creates a caption track from its address, language and menu name.</summary>
    public VideoCaptionTrack(string source, string language, string label)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(source);
        ArgumentException.ThrowIfNullOrWhiteSpace(language);
        ArgumentException.ThrowIfNullOrWhiteSpace(label);

        Source = source;
        Language = language;
        Label = label;
    }
}
