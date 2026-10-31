namespace Forma.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>The dialog that collects a link or an anchor.</summary>
public sealed partial class FormaComposerLinkDialog : ComposerDialogBase
{
    private string? _href;
    private string? _name;
    private bool _openInNewTab;

    /// <summary>Whether the dialog collects a named anchor.</summary>
    [Parameter] public bool IsAnchor { get; set; }

    /// <summary>Callback invoked with the confirmed link.</summary>
    [Parameter] public EventCallback<LinkDialogResult> OnInsert { get; set; }

    /// <inheritdoc />
    // A reopened link carries its address only when one link is retargeted; for several, the author enters it.
    private protected override void OnOpen()
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

            await OnInsert.InvokeAsync(new LinkDialogResult { IsAnchor = true, Name = _name, Href = string.Empty, OpenInNewTab = _openInNewTab });

            return;
        }

        if (string.IsNullOrWhiteSpace(_href))
        {
            Messages.Add(L["Enter the address of the link before inserting."].Value);

            return;
        }

        var href = _href.Trim();

        // An address the sanitizer would strip is refused.
        if (!MarkupPolicy.Allows(HtmlSubject.Url, href, default, out _))
        {
            Messages.Add(L["An address has to be a web address, a mail or telephone link, a path within this site, or an inline picture."].Value);

            return;
        }

        await OnInsert.InvokeAsync(new LinkDialogResult { IsAnchor = false, Name = _name ?? string.Empty, Href = href, OpenInNewTab = _openInNewTab });
    }
}
