namespace Forma.Blazor.Components.Composer.Dialogs;

using System.Diagnostics.CodeAnalysis;

/// <summary>The data collected from the link dialog.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record LinkDialogResult : ComposerDialogResult
{
    /// <summary>Whether the target is a named anchor.</summary>
    public required bool IsAnchor { get; init; }

    /// <summary>The link's visible text, or the anchor's name.</summary>
    public required string Name { get; init; }

    /// <summary>The address the link leads to.</summary>
    public required string Href { get; init; }

    /// <summary>Whether the link opens in a new browser tab.</summary>
    public required bool OpenInNewTab { get; init; }
}
