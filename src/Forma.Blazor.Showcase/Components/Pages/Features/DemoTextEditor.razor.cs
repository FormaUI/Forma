namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Features.Composer.Editing;

using Microsoft.AspNetCore.Components;

/// <summary>The plain text canvas for the composer demo, keeping its plugin's readout in step with the document.</summary>
public sealed partial class DemoTextEditor : ComponentBase
{
    /// <summary>The composer state cascaded from the parent shell.</summary>
    [CascadingParameter] public ComposerState? State { get; set; }

    /// <summary>The plugin this canvas belongs to, cascaded by the host so each composer drives its own.</summary>
    [CascadingParameter] public TextEditorPlugin? Plugin { get; set; }

    // The engine rebuilds the context on every document change, whoever made it, which is the only
    // signal a plugin gets that its readout has gone stale.
    /// <inheritdoc />
    protected override void OnParametersSet()
        => Plugin?.RefreshReadout(State?.Document.Content ?? string.Empty);
}
