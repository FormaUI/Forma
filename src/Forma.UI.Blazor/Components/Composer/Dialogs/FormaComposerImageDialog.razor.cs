namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for inserting an image by address.</summary>
public sealed partial class FormaComposerImageDialog : ComposerDialogBase
{
    private string? _source;
    private string? _altText;
    private string? _caption;
    private bool _seeded;

    /// <summary>The image being reopened, or null when a new one is being inserted.</summary>
    [Parameter] public ImageDialogResult? Reopening { get; set; }

    /// <summary>Callback invoked when image details are confirmed.</summary>
    [Parameter] public EventCallback<ImageDialogResult> OnInsert { get; set; }

    /// <summary>Whether the dialog reopened on something already in the document, rather than inserting anew.</summary>
    private bool IsReopened => Reopening is not null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Only the first opening seeds the fields; a later render would otherwise overwrite what was typed.
        if (!Opened || _seeded)
        {
            return;
        }

        _seeded = true;
        _source = Reopening?.Source;
        _altText = Reopening?.AltText;
        _caption = Reopening?.Caption;
    }

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
