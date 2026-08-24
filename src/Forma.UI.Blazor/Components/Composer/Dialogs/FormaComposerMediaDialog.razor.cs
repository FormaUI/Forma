namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;
using Forma.UI.Blazor.Components.Inputs;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for inserting and configuring media images.</summary>
public sealed partial class FormaComposerMediaDialog : ComponentBase
{
    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private string? _source;
    private string? _altText;
    private string? _caption;
    private string? _alignment;

    private readonly IReadOnlyList<SelectOption<string?>> _alignmentOptions =
    [
        new(null, "Default"),
        new("left", "Left"),
        new("center", "Center"),
        new("right", "Right"),
    ];

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Callback invoked when media details are confirmed.</summary>
    [Parameter] public EventCallback<MediaDialogResult> OnInsert { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task HandleConfirmAsync()
    {
        if (string.IsNullOrWhiteSpace(_source))
        {
            return;
        }

        var result = new MediaDialogResult(_source, _altText, _caption, _alignment);
        await OnInsert.InvokeAsync(result);
    }
}
