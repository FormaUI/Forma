namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for inserting an image by address.</summary>
public sealed partial class FormaComposerImageDialog : ComposerDialogBase
{
    private string? _source;
    private string? _altText;
    private string? _caption;

    /// <summary>Callback invoked when image details are confirmed.</summary>
    [Parameter] public EventCallback<ImageDialogResult> OnInsert { get; set; }

    private async Task HandleConfirmAsync()
    {
        Messages.Clear();

        if (string.IsNullOrWhiteSpace(_source))
        {
            Messages.Add(L["Enter the address of the image before inserting."].Value);

            return;
        }

        var source = _source.Trim();

        // Accepting what the sanitizer would strip would leave the author with a picture that silently vanished.
        if (!MarkupPolicy.Allows(HtmlSubject.Url, source, default, out _))
        {
            Messages.Add(L["An address has to be a web address, a mail or telephone link, a path within this site, or an inline picture."].Value);

            return;
        }

        await OnInsert.InvokeAsync(new ImageDialogResult(source, _altText, _caption));
    }
}
