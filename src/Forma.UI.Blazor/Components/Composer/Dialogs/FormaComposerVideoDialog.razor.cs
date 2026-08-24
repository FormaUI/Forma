namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Components.Feedback;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for configuring and inserting video embeds.</summary>
public sealed partial class FormaComposerVideoDialog : ComponentBase
{
    // Confirming has to be able to fail, so the modal is not allowed to close itself before the address is judged.
    private static readonly FormaModalOptions StayOpenOnConfirm = new() { CloseOnConfirm = false };

    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private string? _source;
    private string? _title;
    private bool _autoplay;
    private bool _showSourceRequired;
    private bool _showSourceUnusable;

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Callback invoked when video embed details are confirmed.</summary>
    [Parameter] public EventCallback<VideoDialogResult> OnInsert { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    // The document keeps only an http or https address, because the sanitizer drops every other scheme
    // and the author would be left with a placeholder pointing nowhere.
    private static bool IsPlayable(string source)
        => Uri.TryCreate(source, UriKind.Absolute, out var address)
            && address.Scheme is "http" or "https";

    private async Task HandleConfirmAsync()
    {
        _showSourceRequired = false;
        _showSourceUnusable = false;

        if (string.IsNullOrWhiteSpace(_source))
        {
            _showSourceRequired = true;

            return;
        }

        var source = _source.Trim();

        if (!IsPlayable(source))
        {
            _showSourceUnusable = true;

            return;
        }

        await OnInsert.InvokeAsync(new VideoDialogResult(source, _title, _autoplay));
    }
}
