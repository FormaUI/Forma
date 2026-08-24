namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Components.Feedback;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for inserting an image by address.</summary>
public sealed partial class FormaComposerImageDialog : ComponentBase
{
    // Confirming has to be able to fail, so the modal is not allowed to close itself before the address is judged.
    private static readonly FormaModalOptions StayOpenOnConfirm = new() { CloseOnConfirm = false };

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private string? _source;
    private string? _altText;
    private string? _caption;
    private bool _showSourceRequired;

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Callback invoked when image details are confirmed.</summary>
    [Parameter] public EventCallback<ImageDialogResult> OnInsert { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task HandleConfirmAsync()
    {
        if (string.IsNullOrWhiteSpace(_source))
        {
            _showSourceRequired = true;

            return;
        }

        await OnInsert.InvokeAsync(new ImageDialogResult(_source.Trim(), _altText, _caption));
    }
}
