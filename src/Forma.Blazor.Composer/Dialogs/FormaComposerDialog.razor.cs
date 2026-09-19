namespace Forma.Blazor.Components.Composer.Dialogs;

using Forma.Blazor.Components.Feedback;

using Microsoft.AspNetCore.Components;
using Microsoft.AspNetCore.Components.Forms;

/// <summary>The modal every composer dialog is built on: its fields, an optional notice, and whatever confirming refused.</summary>
public sealed partial class FormaComposerDialog : FormaComponentBase
{
    // Confirming has to be able to fail, so the modal is not allowed to close itself before the fields are judged.
    private static readonly DialogOptions StayOpenOnConfirm = new() { CloseOnConfirm = false };

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter, EditorRequired] public bool Opened { get; set; }

    /// <summary>The title shown in the modal header.</summary>
    [Parameter, EditorRequired] public string? Title { get; set; }

    /// <summary>The width the modal takes.</summary>
    [Parameter] public DialogSize Size { get; set; } = DialogSize.Medium;

    /// <summary>The fields the dialog offers.</summary>
    [Parameter] public RenderFragment? ChildContent { get; set; }

    /// <summary>Text explaining why the dialog can do nothing, shown beneath the fields.</summary>
    [Parameter] public string? Notice { get; set; }

    /// <summary>What confirming refused, shown beneath the fields, and empty while nothing has been refused.</summary>
    [Parameter] public IReadOnlyList<string> Messages { get; set; } = [];

    /// <summary>Optional override for the confirming action's text.</summary>
    [Parameter] public string? ConfirmText { get; set; }

    /// <summary>Optional override for the dismissing action's text.</summary>
    [Parameter] public string? CancelText { get; set; }

    /// <summary>Callback invoked when the confirming action is pressed.</summary>
    [Parameter] public EventCallback OnConfirm { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    private readonly EditContext _editContext;

    /// <summary>Creates the dialog shell over an edit context the bound inputs resolve against.</summary>
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
}
