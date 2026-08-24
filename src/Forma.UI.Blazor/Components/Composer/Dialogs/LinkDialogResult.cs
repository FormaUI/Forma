namespace Forma.UI.Blazor.Components.Composer.Dialogs;

/// <summary>The data collected from the link dialog.</summary>
/// <param name="Href">The destination URL of the link.</param>
/// <param name="Text">The visible text of the link.</param>
/// <param name="Title">The optional advisory title or tooltip.</param>
/// <param name="OpenInNewTab">Whether the link opens in a new browser tab.</param>
/// <param name="IsAnchor">Whether the target is a named anchor rather than a URL.</param>
/// <param name="AnchorName">The name or identifier of the anchor.</param>
public sealed record LinkDialogResult(
    string Href,
    string? Text,
    string? Title,
    bool OpenInNewTab,
    bool IsAnchor,
    string? AnchorName);
