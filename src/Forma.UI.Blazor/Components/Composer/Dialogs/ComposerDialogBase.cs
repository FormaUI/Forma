namespace Forma.UI.Blazor.Components.Composer.Dialogs;

using Forma.Framework.Localization;
using Forma.UI.Features.Composer.Editing;
using Forma.UI.Features.Composer.Sanitization;

using Microsoft.AspNetCore.Components;

/// <summary>What every composer dialog carries: its open state, its dismissal, and the rules a document is held to.</summary>
public abstract class ComposerDialogBase : ComponentBase
{
    /// <summary>Whether the dialog is currently visible.</summary>
    [Parameter] public bool Opened { get; set; }

    /// <summary>Callback invoked when the dialog is dismissed.</summary>
    [Parameter] public EventCallback OnCancel { get; set; }

    /// <summary>The shared composer context cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    /// <summary>The messages shown beneath the fields, rebuilt each time confirming is judged.</summary>
    private protected List<string> Messages { get; } = [];

    [Inject] private protected IStringLocale<FormaComposer> L { get; set; } = default!;

    // A dialog standing outside a composer has no host rules to answer to, so it judges an address the way the engine would.
    /// <summary>The markup the document may carry, from the host's sanitizer where there is one.</summary>
    private protected IMarkupPolicy MarkupPolicy
        => Context?.Sanitizer.MarkupPolicy ?? ComposerMarkupPolicy.Default;
}
