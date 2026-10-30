namespace Forma.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>The dialog that collects an image by its address.</summary>
public sealed partial class FormaComposerImageDialog : ComposerDialogBase
{
    private string? _source;
    private string? _altText;
    private string? _caption;

    /// <summary>Callback invoked with the confirmed image.</summary>
    [Parameter] public EventCallback<ImageDialogResult> OnInsert { get; set; }

    /// <inheritdoc />
    private protected override void OnOpen()
        => (_source, _altText, _caption) = Reopened<ImageDialogResult>() is { } image
            ? (image.Source, image.AltText, image.Caption)
            : (null, null, null);

    private async Task HandleConfirmAsync()
    {
        Messages.Clear();

        if (string.IsNullOrWhiteSpace(_source))
        {
            Messages.Add(L["Enter the address of the image before inserting."].Value);

            return;
        }

        var source = _source.Trim();

        // An address the sanitizer would strip is refused.
        if (!MarkupPolicy.Allows(HtmlSubject.Url, source, default, out _))
        {
            Messages.Add(L["An address has to be a web address, a mail or telephone link, a path within this site, or an inline picture."].Value);

            return;
        }

        await OnInsert.InvokeAsync(new ImageDialogResult { Source = source, AltText = _altText, Caption = _caption });
    }
}
