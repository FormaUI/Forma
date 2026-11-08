namespace Forma.Blazor.Components.Composer.Dialogs;

using Forma.Blazor.Components.Feedback;
using Forma.Blazor.Components.Inputs;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>The modal every composer dialog is built on: its fields, a notice, and the messages confirming refused with.</summary>
public sealed partial class FormaComposerDialog : FormaComponentBase
{
    // The modal stays open on confirm, so confirming can fail.
    private static readonly DialogOptions StayOpenOnConfirm = new() { CloseOnConfirm = false };

    /// <summary>Whether the dialog is open.</summary>
    [Parameter, EditorRequired] public bool Opened { get; set; }

    /// <summary>The title shown in the modal header.</summary>
    [Parameter, EditorRequired] public string? Title { get; set; }

    /// <summary>The width the modal takes.</summary>
    [Parameter] public DialogSize Size { get; set; } = DialogSize.Medium;

    /// <summary>The fields the dialog offers.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>The notice shown beneath the fields.</summary>
    [Parameter] public string? Notice { get; set; }

    /// <summary>The messages shown beneath the fields.</summary>
    [Parameter] public IReadOnlyList<string> Messages { get; set; } = [];

    /// <summary>The text of the confirming action.</summary>
    [Parameter] public string? ConfirmText { get; set; }

    /// <summary>The text of the dismissing action.</summary>
    [Parameter] public string? CancelText { get; set; }

    /// <summary>Callback invoked when the confirming action is pressed.</summary>
    [Parameter] public EventCallback OnConfirm { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private readonly EditContext _editContext;

    /// <summary>Creates the dialog with the edit context its bound inputs use.</summary>
    public FormaComposerDialog()
    {
        _editContext = new EditContext(this);
    }

    private string ConfirmLabel
        => ConfirmText ?? L["Insert"];

    private string CancelLabel
        => CancelText ?? L["Cancel"];

    /// <inheritdoc />
    protected override void OnParametersSet()
        => ArgumentNullException.ThrowIfNull(Messages);

    // The confirming action sits outside the fields' edit context, so the dialog waits for their text itself.
    private async Task ConfirmAsync()
    {
        if (!await _editContext.SettleEditsAsync())
        {
            return;
        }

        await OnConfirm.InvokeAsync();
    }
}
