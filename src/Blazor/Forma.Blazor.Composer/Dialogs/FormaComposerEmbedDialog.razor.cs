namespace Forma.Blazor.Components.Composer.Dialogs;

using Microsoft.AspNetCore.Components;

/// <summary>The dialog that collects an embed placeholder.</summary>
public sealed partial class FormaComposerEmbedDialog : ComposerDialogBase
{
    private string? _embedType;
    private string? _identifier;
    private string? _settings;

    /// <summary>Callback invoked with the confirmed embed.</summary>
    [Parameter] public EventCallback<EmbedDialogResult> OnInsert { get; set; }

    /// <inheritdoc />
    private protected override void OnOpen()
        => (_embedType, _identifier, _settings) = GetReopeningResult<EmbedDialogResult>() is { } embed
            ? (embed.EmbedType, embed.Identifier, embed.Settings)
            : (null, null, null);

    private async Task HandleConfirmAsync()
    {
        Messages.Clear();

        // Every missing field is reported at once.
        if (string.IsNullOrWhiteSpace(_embedType))
        {
            Messages.Add(L["Enter the embed type before inserting."].Value);
        }

        if (string.IsNullOrWhiteSpace(_identifier))
        {
            Messages.Add(L["Enter the identifier before inserting."].Value);
        }

        if (Messages.Count > 0)
        {
            return;
        }

        await OnInsert.InvokeAsync(new EmbedDialogResult { EmbedType = _embedType!.Trim(), Identifier = _identifier!.Trim(), Settings = _settings });
    }
}
