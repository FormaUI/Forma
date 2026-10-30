namespace Forma.Blazor.Components.Composer.Dialogs;

using System.Diagnostics.CodeAnalysis;

/// <summary>The data collected from the image dialog.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record ImageDialogResult : ComposerDialogResult
{
    /// <summary>The address of the image.</summary>
    public string Source { get; init; } = "";

    /// <summary>The alternative text describing the image.</summary>
    public string? AltText { get; init; }

    /// <summary>The caption of the image.</summary>
    public string? Caption { get; init; }
}
