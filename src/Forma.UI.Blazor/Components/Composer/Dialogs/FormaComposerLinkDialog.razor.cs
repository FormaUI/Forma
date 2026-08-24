namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for inserting and editing links and anchors.</summary>
public sealed partial class FormaComposerLinkDialog : ComponentBase
{
    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private string? _href;
    private string? _name;
    private bool _openInNewTab;

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Whether the dialog is configuring a named anchor rather than a hyperlink.</summary>
    [Parameter] public bool IsAnchor { get; set; }

    /// <summary>The initial URL when editing an existing link.</summary>
    [Parameter] public string? InitialHref { get; set; }

    /// <summary>The initial text selected in the document.</summary>
    [Parameter] public string? InitialText { get; set; }

    /// <summary>Callback invoked when link details are confirmed.</summary>
    [Parameter] public EventCallback<LinkDialogResult> OnInsert { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed without saving.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        if (Opened)
        {
            _href = InitialHref;

            // The selected text names the link or anchor, so an author marking a phrase does not retype it.
            _name ??= InitialText;
        }
    }

    private async Task HandleConfirmAsync()
    {
        if (IsAnchor && string.IsNullOrWhiteSpace(_name))
        {
            return;
        }

        if (!IsAnchor && string.IsNullOrWhiteSpace(_href))
        {
            return;
        }

        var result = new LinkDialogResult(
            IsAnchor,
            _name ?? string.Empty,
            _href ?? string.Empty,
            _openInNewTab);

        await OnInsert.InvokeAsync(result);
    }
}
