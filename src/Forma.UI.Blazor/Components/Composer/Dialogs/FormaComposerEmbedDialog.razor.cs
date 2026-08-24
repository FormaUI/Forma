namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for inserting declarative embed placeholders.</summary>
public sealed partial class FormaComposerEmbedDialog : ComponentBase
{
    [Inject] private IStringLocale<FormaComposer> L { get; set; } = default!;

    private string? _embedType;
    private string? _identifier;
    private string? _settings;

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Callback invoked when embed details are confirmed.</summary>
    [Parameter] public EventCallback<EmbedDialogResult> OnInsert { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    private async Task HandleConfirmAsync()
    {
        if (string.IsNullOrWhiteSpace(_embedType) || string.IsNullOrWhiteSpace(_identifier))
        {
            return;
        }

        var result = new EmbedDialogResult(_embedType, _identifier, _settings);
        await OnInsert.InvokeAsync(result);
    }
}
