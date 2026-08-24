namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for configuring and inserting video embeds.</summary>
public sealed partial class FormaComposerVideoDialog : ComponentBase
{
    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private string? _source;
    private string? _title;
    private bool _autoplay;

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Callback invoked when video embed details are confirmed.</summary>
    [Parameter] public EventCallback<VideoDialogResult> OnInsert { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task HandleConfirmAsync()
    {
        if (string.IsNullOrWhiteSpace(_source))
        {
            return;
        }

        var result = new VideoDialogResult(_source, _title, _autoplay);
        await OnInsert.InvokeAsync(result);
    }
}
