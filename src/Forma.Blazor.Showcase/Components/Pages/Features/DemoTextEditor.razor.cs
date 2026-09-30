namespace Forma.Blazor.Showcase.Components.Pages.Features;

using Forma.Framework.Composer.Editing;

using Microsoft.AspNetCore.Components;

/// <summary>The plain text canvas for the composer demo, keeping its plugin's readout in step with the document.</summary>
public sealed partial class DemoTextEditor : ComponentBase
{
    /// <summary>The composer's context.</summary>
    [CascadingParameter] public ComposerContext? Context { get; set; }

    /// <summary>The plugin this canvas belongs to.</summary>
    [CascadingParameter] public TextEditorPlugin? Plugin { get; set; }

    // The engine rebuilds the context on every document change, so each change arrives here.
    /// <inheritdoc />
    protected override void OnParametersSet()
        => Plugin?.RefreshReadout(Context?.Document.Content ?? string.Empty);
}
