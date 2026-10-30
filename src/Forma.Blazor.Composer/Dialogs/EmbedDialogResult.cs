namespace Forma.Blazor.Components.Composer.Dialogs;

using System.Diagnostics.CodeAnalysis;

/// <summary>The data collected from the embed dialog.</summary>
[DynamicallyAccessedMembers(DynamicallyAccessedMemberTypes.PublicConstructors | DynamicallyAccessedMemberTypes.PublicProperties)]
public sealed record EmbedDialogResult : ComposerDialogResult
{
    /// <summary>The type of the embed, such as a form, a poll or a widget.</summary>
    public string EmbedType { get; init; } = "";

    /// <summary>The identifier of the embedded resource.</summary>
    public string Identifier { get; init; } = "";

    /// <summary>The settings of the embed.</summary>
    public string? Settings { get; init; }
}
