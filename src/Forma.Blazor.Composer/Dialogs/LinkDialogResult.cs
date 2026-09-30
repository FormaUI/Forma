namespace Forma.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the link dialog.</summary>
/// <param name="IsAnchor">Whether the target is a named anchor.</param>
/// <param name="Name">The link's visible text, or the anchor's name.</param>
/// <param name="Href">The address the link leads to.</param>
/// <param name="OpenInNewTab">Whether the link opens in a new browser tab.</param>
public sealed record LinkDialogResult(
    bool IsAnchor,
    string Name,
    string Href,
    bool OpenInNewTab) : ComposerDialogResult;
