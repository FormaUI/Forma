namespace Forma.Framework.Sanitization;

/// <summary>What an address is to the element written with it, which decides the schemes it allows.</summary>
public enum AddressKind
{
    /// <summary>A link a reader follows, such as an anchor's <c>href</c>.</summary>
    Link,

    /// <summary>A link that downloads what it addresses rather than opening it, an anchor carrying <c>download</c>.</summary>
    DownloadLink,

    /// <summary>The source of an image, or of a video's poster.</summary>
    ImageSource,

    /// <summary>The source of a video.</summary>
    VideoSource,

    /// <summary>The source of an audio.</summary>
    AudioSource,

    /// <summary>The source of a text track, such as a video's captions.</summary>
    TrackSource,

    /// <summary>The source of a frame or an object, which the browser opens as a document of its own.</summary>
    FrameSource,
}
