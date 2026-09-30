namespace Forma.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Composer.Editing;
using Forma.Framework.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>The base of every composer dialog: its open state, its dismissal, and the markup policy of the document.</summary>
public abstract class ComposerDialogBase : FormaComponentBase
{
    private bool _hasOpened;

    /// <summary>Whether the dialog is open.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>What the dialog reopened on, where null means it collects something new.</summary>
    [Parameter]
    public ComposerDialogResult? Reopening { get; set; }

    /// <summary>The context of the composer the dialog belongs to.</summary>
    [CascadingParameter] public ComposerContext? Composer { get; set; }

    /// <inheritdoc />
    protected override Type LocalizationSourceType
        => typeof(FormaComposer);

    /// <summary>The messages shown beneath the fields.</summary>
    private protected List<string> Messages { get; } = [];

    /// <summary>The markup policy the document is held to.</summary>
    /// <remarks>Outside a composer, the policy is the rich content default.</remarks>
    private protected IMarkupPolicy MarkupPolicy
        => Composer?.Sanitizer.MarkupPolicy ?? RichContentMarkupPolicy.Default;

    /// <summary>Whether the dialog reopened on something already in the document.</summary>
    private protected bool IsReopened => Reopening is not null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // The fields are filled once, on the first opening.
        if (!Opened || _hasOpened)
        {
            return;
        }

        _hasOpened = true;

        OnOpen();
    }

    /// <summary>Fills the fields as the dialog first opens.</summary>
    private protected virtual void OnOpen()
    {
    }

    /// <summary>What the dialog reopened on, as a result of this dialog's own kind.</summary>
    /// <typeparam name="TResult">The result this dialog collects.</typeparam>
    private protected TResult? Reopened<TResult>()
        where TResult : ComposerDialogResult
        => Reopening as TResult;
}
