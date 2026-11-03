namespace Forma.Blazor.Components.Composer.Dialogs;

using System.Diagnostics.CodeAnalysis;

/// <summary>The data collected from the video embed dialog.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record VideoDialogResult : ComposerDialogResult
{
    /// <summary>The address of the video to embed.</summary>
    public string Source { get; init; } = "";

    /// <summary>The accessible title of the video.</summary>
    public string? Title { get; init; }

    /// <summary>Whether the video plays automatically.</summary>
    public bool Autoplay { get; init; }
}
