namespace Forma.Blazor.Components.Composer.Plugins;

/// <summary>Options configuring which files may be attached to a document.</summary>
public sealed record FileUploadPluginOptions
{
    /// <summary>Which files the chooser offers, in the syntax a file input's <c>accept</c> takes; anything when null.</summary>
    public string? Accept { get; init; }

    /// <summary>The largest file that may be attached, unlimited when null.</summary>
    public long? MaxFileSize { get; init; }

    /// <summary>How many files may be chosen at once, unlimited when null.</summary>
    public int? MaxFiles { get; init; }

    /// <summary>Default options for the file upload plugin.</summary>
    public static FileUploadPluginOptions Default { get; } = new();
}
