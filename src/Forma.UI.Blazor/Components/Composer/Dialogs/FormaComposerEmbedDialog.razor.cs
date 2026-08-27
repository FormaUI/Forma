namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Microsoft.AspNetCore.Components;

/// <summary>Dialog for inserting declarative embed placeholders.</summary>
public sealed partial class FormaComposerEmbedDialog : ComposerDialogBase
{
    private string? _embedType;
    private string? _identifier;
    private string? _settings;
    private bool _seeded;

    /// <summary>The embed being reopened, or null when a new one is being inserted.</summary>
    [Parameter] public EmbedDialogResult? Reopening { get; set; }

    /// <summary>Callback invoked when embed details are confirmed.</summary>
    [Parameter] public EventCallback<EmbedDialogResult> OnInsert { get; set; }

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
        _embedType = Reopening?.EmbedType;
        _identifier = Reopening?.Identifier;
        _settings = Reopening?.Settings;
    }

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
