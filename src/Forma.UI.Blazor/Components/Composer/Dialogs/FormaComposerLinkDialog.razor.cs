namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for inserting and editing links and anchors.</summary>
public sealed partial class FormaComposerLinkDialog : ComposerDialogBase
{
    private string? _href;
    private string? _name;
    private bool _openInNewTab;

    /// <summary>Whether the dialog is configuring a named anchor rather than a hyperlink.</summary>
    [Parameter] public bool IsAnchor { get; set; }

    /// <summary>Callback invoked when link details are confirmed.</summary>
    [Parameter] public EventCallback<LinkDialogResult> OnInsert { get; set; }

    /// <inheritdoc />
    // An address is carried only when one link is being retargeted; several share none, and the author names it.
    private protected override void Seed()
        => (_href, _name, _openInNewTab) = Reopened<LinkDialogResult>() is { } link
            ? (link.Href, link.Name, link.OpenInNewTab)
            : (null, null, false);

    private async Task HandleConfirmAsync()
    {
        Messages.Clear();

        if (IsAnchor)
        {
            if (string.IsNullOrWhiteSpace(_name))
            {
                Messages.Add(L["Enter a name for the anchor before inserting."].Value);

                return;
            }

            await OnInsert.InvokeAsync(new LinkDialogResult(IsAnchor: true, _name, string.Empty, _openInNewTab));

            return;
        }

        if (string.IsNullOrWhiteSpace(_href))
        {
            Messages.Add(L["Enter the address of the link before inserting."].Value);

            return;
        }

        var href = _href.Trim();

        // Accepting what the sanitizer would strip would leave the author with a link that silently vanished.
        if (!MarkupPolicy.Allows(HtmlSubject.Url, href, default, out _))
        {
            Messages.Add(L["An address has to be a web address, a mail or telephone link, a path within this site, or an inline picture."].Value);

            return;
        }

        await OnInsert.InvokeAsync(new LinkDialogResult(IsAnchor: false, _name ?? string.Empty, href, _openInNewTab));
    }
}
