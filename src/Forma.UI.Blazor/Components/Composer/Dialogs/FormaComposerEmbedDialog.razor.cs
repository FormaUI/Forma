namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for inserting declarative embed placeholders.</summary>
public sealed partial class FormaComposerEmbedDialog : ComposerDialogBase
{
    private string? _embedType;
    private string? _identifier;
    private string? _settings;

    /// <summary>Callback invoked when embed details are confirmed.</summary>
    [Parameter] public EventCallback<EmbedDialogResult> OnInsert { get; set; }

    /// <inheritdoc />
    private protected override void OnOpen()
        => (_embedType, _identifier, _settings) = Reopened<EmbedDialogResult>() is { } embed
            ? (embed.EmbedType, embed.Identifier, embed.Settings)
            : (null, null, null);

    private async Task HandleConfirmAsync()
    {
        Messages.Clear();

        // Both are named at once, so an author filling one is not sent back for the other.
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

        await OnInsert.InvokeAsync(new EmbedDialogResult(_embedType!.Trim(), _identifier!.Trim(), _settings));
    }
}
