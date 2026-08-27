namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;
using Forma.Framework.Sanitization;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>What every composer dialog carries: its open state, its dismissal, and the rules a document is held to.</summary>
public abstract class ComposerDialogBase : ComponentBase
{
    private bool _seeded;

    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>What the dialog reopened on, or null when it is collecting something new.</summary>
    /// <remarks>
    /// Public for Blazor parameter binding; dialog implementations should use
    /// <see cref="Reopened{TResult}"/> rather than reading this property directly.
    /// </remarks>
    [Parameter]
    public ComposerDialogResult? Reopening { get; set; }

    /// <summary>The shared composer context cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    /// <summary>The messages shown beneath the fields, rebuilt each time confirming is judged.</summary>
    private protected List<string> Messages { get; } = [];

    [Inject] private protected IStringLocale<FormaComposer> L { get; set; } = default!;

    // A dialog standing outside a composer has no host rules to answer to, so it judges an address the way the engine would.
    /// <summary>The markup the document may carry, from the host's sanitizer where there is one.</summary>
    private protected IMarkupPolicy MarkupPolicy
        => Context?.Sanitizer.MarkupPolicy ?? RichContentMarkupPolicy.Default;

    /// <summary>Whether the dialog reopened on something already in the document, rather than collecting something new.</summary>
    private protected bool IsReopened => Reopening is not null;

    /// <inheritdoc />
    protected override void OnParametersSet()
    {
        // Only the first opening seeds the fields; a later render would otherwise overwrite what was typed.
        if (!Opened || _seeded)
        {
            return;
        }

        _seeded = true;

        Seed();
    }

    /// <summary>Fills the fields from whatever the dialog reopened on, which a dialog collecting something new leaves alone.</summary>
    private protected virtual void Seed()
    {
    }

    /// <summary>What the dialog reopened on when it is of this dialog's own kind, and null otherwise.</summary>
    /// <typeparam name="TResult">The result this dialog collects.</typeparam>
    private protected TResult? Reopened<TResult>()
        where TResult : ComposerDialogResult
        => Reopening as TResult;
}
